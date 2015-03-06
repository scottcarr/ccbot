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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Diagnostics.Contracts;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Research.ReviewBot.Utils;


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

      compilationAsync = project.GetCompilationAsync();
      return true;
    }

  }
}