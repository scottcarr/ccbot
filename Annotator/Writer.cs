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
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
//using Microsoft.CodeAnalysis.Syntax;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;
using System.Diagnostics.Contracts;

namespace Microsoft.Research.ReviewBot
{
  class Writer
  {
    public static Compilation WriteChanges(Compilation orig, Compilation newcomp, bool inplace)
    {
      #region CodeContracts
      Contract.Requires(orig != null);
      Contract.Requires(newcomp != null);
      Contract.Ensures(Contract.Result<Compilation>() != null);
      #endregion CodeContracts

      // this assumes that currentCompilation and originalCompilatin retain the same order
      // which will definitely be true if I just replace syntaxtrees (I hope)
      var formattedCompilation = newcomp;
      foreach(var pair in orig.SyntaxTrees.Zip(newcomp.SyntaxTrees, (SyntaxTree a, SyntaxTree b) => Tuple.Create(a, b)))
        // there must be some easier way to iterate over two lists!?
      {
        var oldst = pair.Item1 as SyntaxTree;
        var newst = pair.Item2 as SyntaxTree;
        var orig_path = oldst.FilePath;
        var copy_path = GetCopyPath(orig_path);
        MSBuildWorkspace msbw = MSBuildWorkspace.Create();
        //var options = msbw.GetOptions();
        // turns out the code I was annotationing had inconsistent formatting
        //options = options.WithChangedOption(CSharpFormattingOptions.OpenBracesInNewLineForMethods, false);
        //options = options.WithChangedOption(CSharpFormattingOptions.OpenBracesInNewLineForTypes, false);
        //options = options.WithChangedOption(CSharpFormattingOptions.OpenBracesInNewLineForControl, false);
        //options = options.WithChangedOption(FormattingOptions.UseTabs, LanguageNames.CSharp, true);
        //var newstFormatted = Formatter.Format(newst.GetRoot(), msbw, options).SyntaxTree;
        var newstFormatted = Formatter.Format(newst.GetRoot(), msbw).SyntaxTree;
        newstFormatted = SyntaxFactory.SyntaxTree(newstFormatted.GetRoot(), oldst.FilePath);
        formattedCompilation = formattedCompilation.ReplaceSyntaxTree(newst, newstFormatted);
        newst = newstFormatted; 
        Contract.Assume(newst != null);
        Contract.Assert(oldst != null);
        if (inplace && newst != oldst && newst.GetChanges(oldst).Any())
        {
          System.IO.File.WriteAllText(copy_path, oldst.GetText().ToString());
        }
        if (newst != oldst && newst.GetChanges(oldst).Any())
        {
          IEnumerable<string> changes;
          if (TryGetChangesIgnoringWhiteSpace(oldst, newst, out changes))
          {
            //RBLogger.Info("Changes in {0}", oldst.FilePath);
            //foreach (var change in changes)
            //{
            //  RBLogger.Info(change);
            //}
            System.IO.File.WriteAllText(orig_path, newst.GetText().ToString());
            //RBLogger.Info("Final version of {0}", orig_path);
            //RBLogger.Indent(); 
            //RBLogger.Info(newst.GetText());
            //RBLogger.Unindent(); 
          }
        }
      }
      return formattedCompilation;
    }
    private static bool TryGetChangesIgnoringWhiteSpace(SyntaxTree oldTree, SyntaxTree newTree, out IEnumerable<string> textchanges) 
    {
      #region CodeContracts
      Contract.Requires(oldTree != null);
      Contract.Requires(newTree != null);
      Contract.Ensures(!Contract.Result<bool>() || Contract.ValueAtReturn<IEnumerable<string>>(out textchanges) != null);
      #endregion CodeContracts

      var workspace = MSBuildWorkspace.Create();
      oldTree = Formatter.Format(oldTree.GetRoot(), workspace).SyntaxTree;
      newTree = Formatter.Format(newTree.GetRoot(), workspace).SyntaxTree;
      textchanges = null;
      if (oldTree == newTree) 
      {
        return false;
      }
      var changes = newTree.GetChanges(oldTree);
      if (!changes.Any()) {
        return false;
      }
      textchanges = changes.Select(x => x.NewText.Trim());
      //textchanges = changes.Select(x => x.NewText.Trim());
      if (textchanges.All(x => x == ""))
      {
        return false;
      }
      return true;

    }
    public static String GetCopyPath(String orig_path)
    {
      var ext = orig_path.LastIndexOf(".cs");
      if (ext != -1)
      {
        var trimmed = orig_path.Remove(ext);
        return trimmed + "_old.cs";
      }
      else
      {
        Contract.Assume(false, "We should never reach it");
        throw new InvalidOperationException();
      }
    }
  }
}
