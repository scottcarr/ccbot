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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics.Contracts;

namespace Microsoft.Research.ReviewBot
{
  using Microsoft.Research.ReviewBot.Annotations;
  using Microsoft.Research.ReviewBot.Utils;
  public static class ReadonlyHelpers
  {
    public static Compilation ReadonlyPass(IEnumerable<BaseAnnotation> annotations, Compilation compilation)
    {
      #region CodeContracts
      Contract.Requires(annotations != null);
      Contract.Requires(compilation != null);
      Contract.Ensures(Contract.Result<Compilation>() != null);
      #endregion CodeContracts

      Output.WriteLine("Preprocessing candidate read-only fields");

      var readonlyAnnotations = annotations.OfType<ReadonlyField>();
      
      return ReadonlyHelpers.SpiltReadOnlyFieldDeclarations(compilation, readonlyAnnotations);
    }

    /// <summary>
    /// walk the syntax trees and replace each field declaration like int x,y,z
    /// with int x; int y; int z;
    /// if one of those has a readonly suggestion
    /// </summary>
    /// <param name="compilation"></param>
    /// <param name="annotations"></param>
    /// <returns></returns>
    private static Compilation SpiltReadOnlyFieldDeclarations(Compilation compilation, IEnumerable<ReadonlyField> annotations)
    {
      #region CodeContracts
      Contract.Requires(compilation != null);
      Contract.Requires(annotations != null);
      Contract.Ensures(Contract.Result<Compilation>() != null);
      #endregion CodeContracts

      // this is probably terribly ineffiecient, but once you modify the syntaxTree in anyway you have to get a new semantic model
      foreach(var annotation in annotations)
      {
        Contract.Assume(compilation.SyntaxTrees.Any());
        var st = compilation.SyntaxTrees.First(x => x.FilePath.Equals(annotation.FileName, StringComparison.OrdinalIgnoreCase));
        var fsr = new FieldSplitterRewriter(annotation, st, compilation);
        var newroot = fsr.Visit(st.GetRoot()).SyntaxTree.GetRoot();
        compilation = compilation.ReplaceSyntaxTree(st, SyntaxFactory.SyntaxTree(newroot, st.FilePath));
        Contract.Assume(compilation != null);
      }
      //foreach (var annotationGroup in annotations.GroupBy(x => x.FileName)) 
      //{
      //  if (annotationGroup.Any()) 
      //  {
      //    var first = annotationGroup.First();
      //    RBLogger.Info("Splitting fields in {0}", first.FileName);
      //    var st = compilation.SyntaxTrees.First(x => x.FilePath.Equals(first.FileName, StringComparison.OrdinalIgnoreCase));
      //    var fsr = new FieldSplitterRewriter(annotationGroup, st, compilation);
      //    var newroot = fsr.Visit(st.GetRoot()).SyntaxTree.GetRoot();
      //    compilation = compilation.ReplaceSyntaxTree(st, SyntaxFactory.SyntaxTree(newroot, st.FilePath));
      //  }
      //}
      return compilation;
    }
    private class FieldSplitterRewriter : CSharpSyntaxRewriter
    {
//      private readonly IEnumerable<ReadonlyField> annotations;
      private readonly SemanticModel SemanticModel;
      private readonly Compilation Compilation;
      //private readonly IEnumerable<String> annotationDCIs;
      private readonly string AnnotationDCI;
      private readonly ReadonlyField annotation;
      //public FieldSplitterRewriter (IEnumerable<ReadonlyField> annotations, SyntaxTree st, Compilation compilation) 
      public FieldSplitterRewriter (ReadonlyField annotation, SyntaxTree st, Compilation compilation) 
      {
        //this.annotations = annotations;
        this.annotation = annotation;
        this.SemanticModel = compilation.GetSemanticModel(st);
        this.AnnotationDCI = annotation.FieldName.Replace("F:", "");
        //this.annotationDCIs = annotations.Select(x => x.FieldName.Replace("F:",""));
        this.Compilation = compilation;
      }
      public ClassDeclarationSyntax RewriteFieldDeclaration(FieldDeclarationSyntax node)
      {

        var parent = node.Parent as ClassDeclarationSyntax;
        var variables = node.Declaration.Variables;
        var newmems = new SyntaxList<MemberDeclarationSyntax>();
        var oldmems = parent.Members;
        //var hits = variables.Where(x => this.annotationDCIs.Contains(SemanticModel.GetDeclaredSymbol(x).ToString()));
        //if (hits.Any())
        var hits = variables.Where(variable => SemanticModel.GetDeclaredSymbol(variable).ToString().Equals(AnnotationDCI));
        if(hits.Any())
        {
          //RBLogger.Info("Got a hit");
          oldmems = oldmems.Remove(node);
          //var parent = node.Parent as ClassDeclarationSyntax;
          //parent = parent.RemoveNode(node, SyntaxRemoveOptions.KeepDirectives | SyntaxRemoveOptions.KeepExteriorTrivia);
          //parent = parent.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
          var oldmodifiers = node.Modifiers;
          foreach (var variable in node.Declaration.Variables)
          {
            var varList = SyntaxFactory.SeparatedList<VariableDeclaratorSyntax>();
            variables = variables.Add(variable);
            var vardecl = SyntaxFactory.VariableDeclaration(node.Declaration.Type, varList);
            var fielddecl = SyntaxFactory.FieldDeclaration(vardecl);
            Contract.Assert(fielddecl != null);
            fielddecl = fielddecl.AddModifiers(oldmodifiers.ToArray());
            //if (hits.Contains(variable) && !oldmodifiers.Any(x => x.Text.Equals("readonly")))
            if (hits.Contains(variable) && !oldmodifiers.Any(x => x.Equals(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))))
            {
              //fielddecl = fielddecl.AddModifiers(SyntaxFactory.ParseToken(@"readonly"));
              fielddecl = fielddecl.AddModifiers(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));
              fielddecl = fielddecl.WithTrailingTrivia(SyntaxFactory.Comment(Constants.String.Signature));
            }
            newmems = newmems.Add(fielddecl);
          }
          //newmems = newmems.AddRange(parent.Members);
          newmems = newmems.AddRange(oldmems);
          return parent.WithMembers(newmems);
        }
        return node.Parent as ClassDeclarationSyntax;
      }
      public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
      {
        foreach (var mem in node.Members)
        {
          if (mem is FieldDeclarationSyntax)
          {
            var newnode = RewriteFieldDeclaration(mem as FieldDeclarationSyntax);
          }
        }
        return base.VisitClassDeclaration(node);
      }
    }
  }
}
