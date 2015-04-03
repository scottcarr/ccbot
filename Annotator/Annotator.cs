/*
  ReviewBot 0.1
  Copyright (c) Microsoft Corporation
  All rights reserved. 
  
  MIT License
  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the ""Software""), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
  The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
  THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Research.ReviewBot.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Research.ReviewBot
{

  // Optional entry point
  public static class Program
  {
    public static void Main(string[] args)
    {
      Annotator.DoAnnotate(args);
    }
  }

  public static class Annotator
  {
    public static int DoAnnotate(Options options)
    {
      // 2. Create a compilation using Roslyn
      Task<Compilation> compilationAsync;
      Compilation compilation;
      MSBuildWorkspace workspace;
      Project project;
      if (!RoslynInterface.TryCreateCompilation(options, out compilationAsync, out workspace, out project))
      {
        Output.WriteError("Unable to create compilation");
        return -1;
      }

      // 3. Read Clousot XML
      CCCheckOutput xml;
      if (!XmlDoc.TryReadXml(options.CccheckXml, out xml))
      {
        Output.WriteError("Unable to read XML");
        return -1;
      }

      compilation = compilationAsync.Result;

      // 4. Check for diagnostics in the solution
      Output.WritePhase("Checking whether the original project has errors");
      var existingDiagnostics = compilation.GetDiagnostics();
      if (existingDiagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Any())
      {
        // SCOTT: this should be fatal IMHO
        HelperForDiagnostics.PrintDiagnostics(existingDiagnostics);
        Output.WriteErrorAndQuit("The original project has some errors");
      }

      // 5. Annotate
      Output.WritePhase("Reading the Contracts from Clousot");
      var annotations = Parser.GetAnnotationDictionary(xml);

      Output.WritePhase("Applying the annotations to the source code");
      var newcomp = ApplyAnnotations(annotations, compilation, project, true);

      // 6. Pretty print result
      Output.WritePhase("Cleaning up the code");
      newcomp = UsingHelpers.CleanImports(project, newcomp);

      // 7. Write result to disk
      Output.WritePhase("Writing result back to disk");
      newcomp = Writer.WriteChanges(compilation, newcomp, options.OutputType == OutputOption.inplace);

      //CommentHelpers.WriteCommentsFile(xml, compilation, newcomp, options.GitRoot);
      
      return 0;
    }
    public static int DoAnnotate(string[] args)
    {
      #region CodeContracts
      Contract.Requires(args != null);
      Contract.Requires(Contract.ForAll(0, args.Length, i => args[i] != null));
      Contract.Ensures(-1 <= Contract.Result<int>()); // we return 0 on success, -1 on failure
      #endregion CodeContracts

      // 1. parse the command line options
      Options options; String error;
      if (!Options.TryParseOptions(args, out options, out error))
      {
        Options.PrintUsage(error);
        Output.WriteError("Can't parse the options");
        return -1;
      }

      return DoAnnotate(options);
    }

    /// <summary>
    /// Apply the annotations, optionally remove errorenous annotations
    /// </summary>
    /// <param name="annotations">annotations to be applied</param>
    /// <param name="originalCompilation">compilation to annotate</param>
    /// <param name="project">originalCompilation's project</param>
    /// <param name="check_for_errors">if true, will remove annotations cause errors (according to Roslyn)</param>
    /// <returns>A modified version of originalCompilation with the annotations added</returns>
    private static Compilation ApplyAnnotations(IEnumerable<BaseAnnotation> annotations, Compilation originalCompilation, Project project, bool check_for_errors = true)
    {
      #region CodeContracts
      Contract.Requires(originalCompilation != null);
      Contract.Requires(annotations != null);
      Contract.Ensures(Contract.Result<Compilation>() != null);
      #endregion CodeContracts

      IEnumerable<BaseAnnotation> annotations2, annotations3;
      var newCompilation = originalCompilation;
      newCompilation = InterfaceAnnotationHelpers.CreateOrFindContractsClasses(annotations, project, newCompilation, out annotations2);
      newCompilation = ReadonlyHelpers.ReadonlyPass(annotations2, newCompilation);
      newCompilation = ObjectInvariantHelpers.ObjectInvariantPass(newCompilation, annotations2, out annotations3);
      var syntosugdict = Searcher.GetSyntaxNodeToAnnotationDict(newCompilation, annotations3);
      var syntosyndict = Replacer.PrecomputeReplacementNodes(syntosugdict, newCompilation);
      var comp2 = Replacer.RewriteCompilation(newCompilation, syntosyndict);
      var diagnostics = comp2.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
      if (diagnostics.Any() && check_for_errors)
      {
        Output.WriteLine("Fix the errors introduced by bad contracts");

        Contract.Assert(annotations3.Count() == annotations.Count());
        var filteredAnnotations = HelperForDiagnostics.FilterBadSuggestions(comp2, annotations3);
        // unresolve interface annotations, we're reverting to the original compilation and forgetting everything
        filteredAnnotations = filteredAnnotations.Select(x => x is ResolvedInterfaceAnnotation ? (x as ResolvedInterfaceAnnotation).OriginalAnnotation : x);
        filteredAnnotations = filteredAnnotations.Select(x => x is ResolvedObjectInvariant ? (x as ResolvedObjectInvariant).OriginalAnnotation : x);
        comp2 = ApplyAnnotations(filteredAnnotations, originalCompilation, project, false);
      }
      return comp2;
    }
  }
}
