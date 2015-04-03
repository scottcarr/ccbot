/*
  ReviewBot 0.1
  Copyright (c) Microsoft Corporation
  All rights reserved. 
  
  MIT License
  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the ""Software""), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
  The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
  THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Diagnostics.Contracts;
using Microsoft.CodeAnalysis.MSBuild;


namespace Microsoft.Research.ReviewBot
{
  static class RoslynInterface
  {
    static public bool TryCreateCompilation(Options options, out Task<Compilation> cu, out MSBuildWorkspace workspace, out Project project) 
    {
      Contract.Ensures(!Contract.Result<bool>() || Contract.ValueAtReturn(out cu) != null); 
      Contract.Ensures(!Contract.Result<bool>() || Contract.ValueAtReturn(out workspace) != null);

      Output.WriteLine("Opening the solution {0}", options.Solution);

      workspace = MSBuildWorkspace.Create();
      cu = null;
      project = null;
      try
      {
        if (options.Project != null && options.Solution != null)
        {
          return CreateCompilationFromSolution(options, workspace, out cu, out project);
        }
        else
        {
          Output.WriteError("Failed to parse either the Project or Solution");
          // not implemented;
          return false;
        }
      }
      catch(Exception e)
      {
        Output.WriteError("Error while parsing the .csproj file. Exception from Roslyn: {0}", e.ToString());
        cu = null;
        return false;
      }
    }

    private static bool CreateCompilationFromSolution(Options options, MSBuildWorkspace workspace, out Task<Compilation> compilationAsync, out Project project) 
    {
      var solution = workspace.OpenSolutionAsync(options.Solution).Result;
      var projects = solution.Projects.Where(proj => proj.FilePath.Equals(options.Project));
      if (projects.Any())
      {
        project = projects.First();
      }
      else
      {
        Output.WriteError("Unable to find the specified project in solution. Project {0}", options.Project);

        project = null;
        compilationAsync = null;
        return false;
      }

      List<string> why;
      Project fixedProject;
      if (HasErrors(project, out why))
      {
        if (TryFixingMissingReferences(project, out why, out fixedProject) != ReferenceStatus.Broken)
        {
          project = fixedProject;
        }
      }

      compilationAsync = project.GetCompilationAsync();
      return true;
    }
    enum ReferenceStatus { OK, MissingMSCorLib, MissingFacades45, MissingFacades46, Broken };
    enum DotNetFrameworkVersions { v4_5, v4_6};
    static IEnumerable<MetadataReference> GetFacadeReferences(DotNetFrameworkVersions version)
    {
      var refs = new List<MetadataReference>();
      string facadesDir = "";
      switch (version)
      {
        case DotNetFrameworkVersions.v4_5:
          facadesDir = String.Format(@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\");
          break;
        case DotNetFrameworkVersions.v4_6:
          facadesDir = String.Format(@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\");
          break;
      }
      string[] dlls =
      {
       "System.Collections.dll",
       "System.Collections.Concurrent.dll",
       "System.Globalization.dll",
       "System.IO.dll",
       "System.Reflection.dll",
       "System.Reflection.Extensions.dll",
       "System.Reflection.Primitives.dll",
       "System.Resources.ResourceManager.dll",
       "System.Runtime.dll",
       "System.Runtime.Extensions.dll",
       "System.Runtime.InteropServices.dll",
       "System.Text.Encoding.dll",
       "System.Threading.dll",
       "System.Threading.Tasks.dll",
       "System.Xml.ReaderWriter.dll",
      };
      var dllPaths = dlls.Select(x => facadesDir + x);
      refs.AddRange(dllPaths.Select(f => MetadataReference.CreateFromFile(f)));
      return refs;
    }
    static MetadataReference GetMSCorLibRef()
    {
      return MetadataReference.CreateFromAssembly(typeof(object).Assembly);
    }
    static ReferenceStatus TryFixingMissingReferences(Project proj, out List<string> why, out Project fixedProj)
    {
      List<string> orig_msg, mscl_msg, facade45_msg, facade46_msg;
      why = new List<string>();
      if (!HasErrors(proj, out orig_msg))
      {
        fixedProj = proj;
        return ReferenceStatus.OK;
      }
      var projWithMSCL = proj.AddMetadataReference(GetMSCorLibRef());
      if (!HasErrors(projWithMSCL, out mscl_msg))
      {
        fixedProj = projWithMSCL;
        return ReferenceStatus.MissingMSCorLib;
      }
      var projWithFacades45 = proj.AddMetadataReferences(GetFacadeReferences(DotNetFrameworkVersions.v4_5));
      if (!HasErrors(projWithFacades45, out facade45_msg))
      {
        fixedProj = projWithFacades45;
        return ReferenceStatus.MissingFacades45;
      }
      var projWithFacades46 = proj.AddMetadataReferences(GetFacadeReferences(DotNetFrameworkVersions.v4_6));
      if (!HasErrors(projWithFacades46, out facade46_msg))
      {
        fixedProj = projWithFacades46;
        return ReferenceStatus.MissingFacades46;
      }

      var msgs = new List<Tuple<Project, List<string>>>();
      msgs.Add(new Tuple<Project, List<string>>(proj, orig_msg));
      msgs.Add(new Tuple<Project, List<string>>(projWithMSCL, mscl_msg));
      msgs.Add(new Tuple<Project, List<string>>(projWithFacades45, facade45_msg));
      msgs.Add(new Tuple<Project, List<string>>(projWithFacades46, facade46_msg));
      var lens = msgs.Select(x => x.Item2.Count());
      var shortest_len = lens.Min();
      var shortest = msgs.First(x => x.Item2.Count() == shortest_len);
      why = shortest.Item2;
      fixedProj = shortest.Item1;
      return ReferenceStatus.Broken;
    }
    static bool HasErrors(Project proj, out List<string> why)
    {
      why = new List<string>();
      try
      {
        var cu = proj.GetCompilationAsync().Result;
        foreach (var e in cu.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error))
        {
          why.Add(String.Format("{0}: {1}", e.Location, e.GetMessage()));
        }
        return why.Count() != 0;
      }
      catch (AggregateException e)
      {
        foreach (var ie in e.InnerExceptions)
        {
          why.Add(ie.Message);
        }
        return false;

      }
      catch (Exception e)
      {
        why.Add(e.Message);
        return false;
      }
    }
  }
}