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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Research.ReviewBot
{
  using System.Diagnostics.Contracts;
  using Microsoft.Research.ReviewBot.Annotations;
  using Microsoft.Research.ReviewBot.Utils;
  static class Searcher
  {
    public static SyntaxDictionary GetSyntaxNodeToAnnotationDict(Compilation compilation, IEnumerable<BaseAnnotation> annotations)
    {
      #region CodeContracts
      Contract.Requires(compilation != null);
      Contract.Requires(annotations != null);
      Contract.Ensures(Contract.Result<SyntaxDictionary>() != null);
      #endregion CodeContracts

      Output.WriteLine("Creating the SyntaxNode to annotations dictionary");

      var annotations_by_node = new SyntaxDictionary();
      foreach (var st in compilation.SyntaxTrees) 
      {
        var fp = st.FilePath;
        var curr_anns = annotations.Where(x => x.FileName.Equals(fp, StringComparison.OrdinalIgnoreCase));
        var fields = curr_anns.OfType<ReadonlyField>();
        curr_anns = curr_anns.Except(fields);
        //Dictionary<MethodNameId, List<BaseAnnotation>> curr_anns;
        //if (annotations.TryGetValue(fp, out curr_anns)) // does the syntax tree have any annotations?
        if (curr_anns.Any())
        {
          annotations_by_node.Add(fp, new Dictionary<SyntaxNode, List<BaseAnnotation>>());
          var sm = compilation.GetSemanticModel(st);
          var mf = new MethodsFinder(curr_anns, sm);
          var before = curr_anns.Count();
          var after = mf.methods_to_annotate.Count;
          if (before != after) {
            Output.WriteWarning("Didn't match all methods for " + fp + " There were " + before + " but now " + after, "ERROR");
            Output.WriteLine("Next the list of those we failed to match");
            foreach (var orig in curr_anns.Select(x => x.MethodName))
            {
              var matched = false;
              foreach (var newguy in mf.methods_to_annotate.Keys)
              {
                if (newguy is FieldDeclarationSyntax) { continue; } // this way of finding missing annotations doesn't really work for readonly fields
                var sym = sm.GetDeclaredSymbol(newguy) as ISymbol;
                var dci = sym.GetDocumentationCommentId();
                if (dci.Equals(orig))
                {
                  matched = true;
                }
              }
              if (!matched)
              {
                Output.WriteLine("Failed to find {0}", orig);
              }
            }
          }
          else
          {
            Output.WriteLine("Matched all methods in {0}", fp);
          }
          //before = DictionaryHelpers.CountTotalItems(curr_anns);
          before = curr_anns.Count();
          after = DictionaryHelpers.CountTotalItems(mf.methods_to_annotate);
          if (before != after)
          {
            Output.WriteWarning("Lost some annotations for " + fp + " There were " + before + " but now " + after, "ERROR");
          }
          else
          {
            Output.WriteLine("Matched all annotations in {0}", fp);
          }
          foreach (var key in mf.methods_to_annotate.Keys) 
          {
            annotations_by_node[fp].Add(key, mf.methods_to_annotate[key]); 
          }
        }
      }
      int total = 0;
      foreach (var dict in annotations_by_node.Values)
      {
        total += DictionaryHelpers.CountTotalItems(dict);
      }

      Output.WriteLine("I started with {0} annotations and I found {1}", annotations.Count().ToString(), total.ToString());
      if (annotations.Count() != total)
      {
        var interfaces = annotations.OfType<ResolvedInterfaceAnnotation>();
        var fields = annotations.OfType<ReadonlyField>();
        var invariants = annotations.OfType<ResolvedObjectInvariant>();
        var other = annotations.OfType<Precondition>();
        var found = new List<BaseAnnotation>();
        foreach (var file in annotations_by_node)
        {
          foreach (var sugs in file.Value.Values)
          {
            found = found.Concat(sugs).ToList();
          }
        }
        var interfacesFound = found.OfType<ResolvedInterfaceAnnotation>();
        var fieldsFound = found.OfType<ReadonlyField>();
        var invariantsFound = found.OfType<ResolvedObjectInvariant>();
        var otherFound = found.OfType<Precondition>();
        RBLogger.Info("Interfaces found {0} of {1}", interfacesFound.Count(), interfaces.Count());
        RBLogger.Info("fields found {0} of {1}", fieldsFound.Count(), fields.Count());
        RBLogger.Info("invariants found {0} of {1}", invariantsFound.Count(), invariants.Count());
        RBLogger.Info("other found {0} of {1}", otherFound.Count(), other.Count());
        var lost = other.Except(otherFound);
        RBLogger.Info("Missing annotations:");
        RBLogger.Indent();
        foreach (var group in lost.GroupBy(x => x.MethodName))
        {
          RBLogger.Info(group.First().MethodName);
          RBLogger.Indent();
          foreach (var ann in group)
          {
            RBLogger.Info(ann);
          }
          RBLogger.Unindent();
        }
        RBLogger.Unindent();
        //RBLogger.Info("Found annotations {0}", found.Count());
        //RBLogger.Info("Unmatched annotations:");
        //RBLogger.Indent();
        //foreach (var annotation in other)
        //{
        //  RBLogger.Info(annotation.MethodName);
        //  RBLogger.Indent();
        //  RBLogger.Info(annotation);
        //  RBLogger.Unindent();
        //}
        //RBLogger.Unindent();
      }
      return annotations_by_node;
    }
    private class MethodsFinder : CSharpSyntaxWalker
    {
      public readonly Dictionary<SyntaxNode, List<BaseAnnotation>> methods_to_annotate;
      //private readonly Dictionary<MethodNameId, List<BaseAnnotation>> preconditions;
      private readonly IEnumerable<BaseAnnotation> preconditions;
      readonly SemanticModel sm;
      //public MethodsFinder(Dictionary<MethodNameId,List<BaseAnnotation>> preconditions, SemanticModel sm) 
      public MethodsFinder(IEnumerable<BaseAnnotation> preconditions, SemanticModel sm) 

      {
        this.methods_to_annotate = new Dictionary<SyntaxNode, List<BaseAnnotation>>();
        //this.fieldsToMakeReadonly = new Dictionary<SyntaxNode, List<ReadonlyField>>();
        this.preconditions = preconditions;
        this.sm = sm;
        this.Visit(sm.SyntaxTree.GetRoot());
      }
      public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
      {
        TryToMatch(node);
      }
      public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
      {
        TryToMatch(node);
      }
      public override void VisitAccessorDeclaration(AccessorDeclarationSyntax node)
      {
        if (node.Body != null) // body will be null for automatically generated property accessors
        {
          TryToMatch(node);
        }
      }
      #region Private -- where the real work is done

      private void TryToMatch(SyntaxNode node) 
      {
        var si = sm.GetDeclaredSymbol(node);
        var dci = si.GetDocumentationCommentId();
        var matches = preconditions.Where(x => x.MethodName.Equals(dci));
        if (matches.Any())
        {
          methods_to_annotate.Add(node, matches.ToList());
        }
        //foreach (var methodname in preconditions.Keys)
        //{
        //  var si = sm.GetDeclaredSymbol(node) as ISymbol;
        //  var dci = si.GetDocumentationCommentId();
        //  if (dci.Equals(methodname))
        //  {
        //    methods_to_annotate.Add(node, preconditions[methodname]);
        //    return;
        //  }
        //}
      }
      #endregion
    }
  }
}
