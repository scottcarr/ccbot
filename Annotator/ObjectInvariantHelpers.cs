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
using System.Collections.Immutable;

namespace Microsoft.Research.ReviewBot
{
  using Microsoft.Research.ReviewBot.Annotations;
  using Microsoft.Research.ReviewBot.Utils;
  public static class ObjectInvariantHelpers
  {
    /// <summary>
    /// Find each object invariant if it exists, otherwise create it, update the annotations
    /// </summary>
    /// <param name="compilation"></param>
    /// <param name="annotations"></param>
    /// <param name="newannotations">modified annotations with method/file names in the new compilation</param>
    /// <returns>modified compilation with missing object invariants added</returns>
    public static Compilation ObjectInvariantPass(Compilation compilation, 
                                                  IEnumerable<BaseAnnotation> annotations, 
                                                  out IEnumerable<BaseAnnotation> newannotations)
    {
      #region CodeContracts
      Contract.Requires(compilation != null);
      Contract.Requires(annotations != null);
      Contract.Ensures(Contract.Result<Compilation>() != null);
      Contract.Ensures(Contract.ValueAtReturn(out newannotations) != null);
      Contract.Ensures(Contract.ValueAtReturn(out newannotations).Count() == annotations.Count());
      #endregion CodeContracts

      Output.WriteLine("Adding object invariants");

      var objectInvariants = annotations.OfType<UnresolvedObjectInvariant>();
      var newanns = annotations.Except(objectInvariants);
      // with partial classes we can have annotations that refer to that same class in different files
      //foreach (var fileGroup in objectInvariants.GroupBy(x => x.FileName))
      //{
      //  var fileName = fileGroup.First().FileName;
      //  var syntaxTree = compilation.SyntaxTrees.First(x => x.FilePath.Equals(fileName, StringComparison.OrdinalIgnoreCase));
      //  foreach (var typeGroup in fileGroup.GroupBy(x => x.TypeName))
      //  {
      //    var typeName = typeGroup.First().TypeName;
      //    var typeNode = FindClassNode(compilation, syntaxTree, typeName);
      //    var method = FindObjectInvariantMethod(compilation.GetSemanticModel(syntaxTree), typeNode);
      //    if (method == null)
      //    {
      //      method = AddObjectInvariantMethod(typeNode);
      //      var newTypeNode = method.Parent;
      //      var newSyntaxTreeRoot = syntaxTree.GetRoot().ReplaceNode(typeNode, newTypeNode);
      //      var newSyntaxTree = SyntaxFactory.SyntaxTree(newSyntaxTreeRoot, syntaxTree.FilePath);
      //      compilation = compilation.ReplaceSyntaxTree(syntaxTree, newSyntaxTree);
      //      //syntaxTree = newSyntaxTree;
      //      // the next call is necessary when we change the compilation inside the loop
      //      syntaxTree = compilation.SyntaxTrees.First(x => x.FilePath.Equals(fileName, StringComparison.OrdinalIgnoreCase));
      //    } // else do nothing, the object invariant method exists
      //    newanns = newanns.Concat(UpdateMethodNames(compilation, typeGroup));
      //  }
      //}
      foreach (var typeGroup in objectInvariants.GroupBy(x => x.TypeName))
      {
        var typeName = typeGroup.First().TypeName;
        var files = typeGroup.Select(annotation => annotation.FileName).Distinct();
        //ClassDeclarationSyntax typeNode;
        TypeDeclarationSyntax typeNode;
        SyntaxTree syntaxTree;
        // we need multiple files because of partial classes
        if (!DoesObjectInvariantExist(typeName, files, compilation, out typeNode, out syntaxTree))
        {
          var method = AddObjectInvariantMethod(typeNode);
          var newTypeNode = method.Parent;
          var newSyntaxTreeRoot = syntaxTree.GetRoot().ReplaceNode(typeNode, newTypeNode);
          var newSyntaxTree = SyntaxFactory.SyntaxTree(newSyntaxTreeRoot, syntaxTree.FilePath);
          compilation = compilation.ReplaceSyntaxTree(syntaxTree, newSyntaxTree);
          // the next call is necessary when we change the compilation inside the loop
          //syntaxTree = compilation.SyntaxTrees.First(x => x.FilePath.Equals(syntaxTree.FilePath, StringComparison.OrdinalIgnoreCase));
        }
        newanns = newanns.Concat(ResolveObjectInvariants(compilation, typeGroup));
      }
      newannotations = newanns.ToImmutableArray();
      return compilation;
    }
    //private static bool DoesObjectInvariantExist(string typename, IEnumerable<string> files, Compilation compilation, out ClassDeclarationSyntax typeNode, out SyntaxTree syntaxTree)
    private static bool DoesObjectInvariantExist(string typename, IEnumerable<string> files, Compilation compilation, out TypeDeclarationSyntax typeNode, out SyntaxTree syntaxTree)
    {
      // there has to be at least one file in files, so these are never null at return
      syntaxTree = null;
      typeNode = null;
      foreach (var file in files)
      {
        syntaxTree = compilation.SyntaxTrees.First(tree => tree.FilePath.Equals(file, StringComparison.OrdinalIgnoreCase));
        typeNode = FindClassNode(compilation, syntaxTree, typename);
        var method = FindObjectInvariantMethod(compilation.GetSemanticModel(syntaxTree), typeNode);
        if (method != null)
        {
          return true;
        }
      }
      return false;
    }

    private static IEnumerable<ResolvedObjectInvariant> ResolveObjectInvariants(Compilation compilation, IEnumerable<UnresolvedObjectInvariant> annotations)
    {
      #region CodeContracts
      Contract.Requires(compilation != null);
      Contract.Requires(compilation.SyntaxTrees != null);
      Contract.Requires(annotations != null);
      Contract.Requires(annotations.Any());
      Contract.Requires(Contract.ForAll(annotations, annotation => annotation != null));
      Contract.Ensures(Contract.Result<IEnumerable<ResolvedObjectInvariant>>() != null);
      Contract.Ensures(Contract.Result<IEnumerable<ResolvedObjectInvariant>>().Count() == annotations.Count());
      Contract.Ensures(Contract.ForAll(Contract.Result<IEnumerable<ResolvedObjectInvariant>>(), annotation => annotation != null));
      #endregion CodeContracts

      var typeName = annotations.First().TypeName;
      var files = annotations.Select(annotation => annotation.FileName);
      //var fileName = annotations.First().FileName;
      //var syntaxTree = compilation.SyntaxTrees.First(x => x.FilePath.Equals(fileName, StringComparison.OrdinalIgnoreCase));
      var syntaxTrees = compilation.SyntaxTrees.Where(tree => files.Any(file => file.Equals(tree.FilePath, StringComparison.OrdinalIgnoreCase)));
      foreach (var tree in syntaxTrees)
      {
        var sm = compilation.GetSemanticModel(tree);
        var typeNode = FindClassNode(compilation, tree, typeName);
        if (typeNode == null) { continue; }
        var methodNode = FindObjectInvariantMethod(sm, typeNode);
        if (methodNode == null) { continue; }
        var symbol = sm.GetDeclaredSymbol(methodNode);
        if (symbol == null) { continue; }
        var methodName = symbol.GetDocumentationCommentId();
        if (methodName == null) { continue; }
        //return annotations.Select(x => x.WithMethodName(methodName).WithFileName(tree.FilePath));
        return annotations.Select(x => new ResolvedObjectInvariant(x, tree.FilePath, methodName));
      }
      Contract.Assert(false); // if we didnt find the object invariant method by now, we're doomed
      return null;
    }
    //private static ClassDeclarationSyntax FindClassNode(Compilation compilation, SyntaxTree syntaxTree, String documentationCommentId)
    private static TypeDeclarationSyntax FindClassNode(Compilation compilation, SyntaxTree syntaxTree, String documentationCommentId)
    {
      #region CodeContracts
      Contract.Requires(compilation != null);
      Contract.Requires(syntaxTree != null);
      Contract.Requires(!string.IsNullOrEmpty(documentationCommentId));
      // this method doesn't really ensure anything as it is,
      // it could be refactored into TryFindClassNode then it would ensure returning true => return is non null
      #endregion CodeContracts

      //var classes = syntaxTree.GetRoot().DescendantNodesAndSelf().Where(node => node.CSharpKind() == SyntaxKind.ClassDeclaration);
      var classes = syntaxTree.GetRoot().DescendantNodesAndSelf().OfType<TypeDeclarationSyntax>();
      //var desendants = syntaxTree.GetRoot().DescendantNodesAndSelf().ToArray();
      //var desClasses = desendants.Where(x => x.CSharpKind() == SyntaxKind.ClassDeclaration);
      //foreach(var node in syntaxTree.GetRoot().ChildNodes())
      //{
      //  RBLogger.Info(node.CSharpKind());
      //}
      //var last = syntaxTree.GetRoot().ChildNodes().Last();
      //var lastKind = last.CSharpKind();
      //var l = (int)lastKind;
      //var i = (int)Microsoft.CodeAnalysis.CSharp.SyntaxKind.ClassDeclaration;
      //Console.WriteLine(last.CSharpKind());
      //Console.WriteLine(lastKind.Equals(Microsoft.CodeAnalysis.CSharp.SyntaxKind.ClassDeclaration));
      //Console.WriteLine(i == l);
      //RBLogger.Info(classes.Count());
      var sm = compilation.GetSemanticModel(syntaxTree);
      Contract.Assert(sm != null);
      //foreach(var c in classes)
      //{
      //  RBLogger.Info(sm.GetDeclaredSymbol(c).GetDocumentationCommentId());
      //}
      return classes.First(node => sm.GetDeclaredSymbol(node).GetDocumentationCommentId() == documentationCommentId) as TypeDeclarationSyntax;
      //return syntaxTree.GetRoot().DescendantNodesAndSelf().First(x => x.CSharpKind().Equals(SyntaxKind.ClassDeclaration)
      //                                                                         && sm.GetDeclaredSymbol(x).GetDocumentationCommentId().Equals(documentationCommentId))
      //                                                                         as ClassDeclarationSyntax;
    }
    //private static MethodDeclarationSyntax FindObjectInvariantMethod(SemanticModel sm, ClassDeclarationSyntax classnode)
    private static MethodDeclarationSyntax FindObjectInvariantMethod(SemanticModel sm, TypeDeclarationSyntax classnode)
    {
      #region CodeContracts
      Contract.Requires(sm != null);
      Contract.Requires(classnode != null);
      #endregion CodeContracts

      var finder = new ObjectInvariantMethodFinder(sm);
      finder.Visit((SyntaxNode) classnode);
      return finder.theinvariantmethod;

    }
    private class ObjectInvariantMethodFinder : CSharpSyntaxWalker
    {
      public MethodDeclarationSyntax theinvariantmethod = null;
      private bool already_found_a_class = false;
      readonly private SemanticModel sm; 
      public ObjectInvariantMethodFinder(SemanticModel sm)
      {
        #region CodeContracts
        Contract.Requires(sm != null);
        Contract.Ensures(this.sm == sm);
        #endregion CodeContracts

        this.sm = sm;
      }
      public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
      {
        if (isObjectInvariantMethod(node, sm))
        {
          theinvariantmethod = node;
        }
      }
      public override void VisitClassDeclaration(ClassDeclarationSyntax node)
      {
        if (!already_found_a_class)
        {
          already_found_a_class = true;
          base.VisitClassDeclaration(node);
        }
        return; // do not decsend into other classes
      }
    }
    //private static MethodDeclarationSyntax AddObjectInvariantMethod(ClassDeclarationSyntax class_to_add_to)
    private static MethodDeclarationSyntax AddObjectInvariantMethod(TypeDeclarationSyntax class_to_add_to)
    {
      Contract.Ensures(Contract.Result<MethodDeclarationSyntax>() != null);
      
      var oima = new ObjectInvariantMethodAdder();
      //var newclass = oima.Visit(class_to_add_to) as ClassDeclarationSyntax;
      var newclass = oima.Visit(class_to_add_to) as TypeDeclarationSyntax;
      return oima.invariantmethod;
    }
    private class ObjectInvariantMethodAdder : CSharpSyntaxRewriter
    {
      public MethodDeclarationSyntax invariantmethod;
      public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax classnode)
      {
        return VisitTypeDeclaration(classnode);
        //#region CodeContracts
        //Contract.Ensures(invariantmethod != null);
        //Contract.Assume(classnode != null);
        //#endregion CodeContracts

        //var newmems = classnode.Members;
        //var text = String.Format("\n[ContractInvariantMethod]\nprivate void {0}ObjectInvariantMethod() {{}}\n", classnode.Identifier.ToString());
        //var comp = SyntaxFactory.ParseCompilationUnit(text);
        //var objinv = comp.Members[0];
        //newmems = newmems.Add(objinv);
        //var newclass = classnode.WithMembers(newmems);
        //invariantmethod = (MethodDeclarationSyntax) newclass.Members.Last();
        //return newclass; // do not descend into class
      }
      public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax structnode)
      {
        return VisitTypeDeclaration(structnode);
        //#region CodeContracts
        //Contract.Ensures(invariantmethod != null);
        //Contract.Assume(structnode != null);
        //#endregion CodeContracts

        //var newmems = structnode.Members;
        //var text = String.Format("\n[ContractInvariantMethod]\nprivate void {0}ObjectInvariantMethod() {{}}\n", classnode.Identifier.ToString());
        //var comp = SyntaxFactory.ParseCompilationUnit(text);
        //var objinv = comp.Members[0];
        //newmems = newmems.Add(objinv);
        //var newclass = structnode.WithMembers(newmems);
        //invariantmethod = (MethodDeclarationSyntax) newclass.Members.Last();
        //return newclass; // do not descend into class
      }
      private SyntaxNode VisitTypeDeclaration(TypeDeclarationSyntax typenode)
      {
        #region CodeContracts
        Contract.Ensures(invariantmethod != null);
        Contract.Assume(typenode != null);
        #endregion CodeContracts

        var newmems = typenode.Members;
        var text = String.Format("\n[ContractInvariantMethod]\nprivate void {0}ObjectInvariantMethod() {{}}\n", typenode.Identifier.ToString());
        var comp = SyntaxFactory.ParseCompilationUnit(text);
        var objinv = comp.Members[0];
        newmems = newmems.Add(objinv);
        TypeDeclarationSyntax newtype = null;
        var classnode = typenode as ClassDeclarationSyntax;
        if (classnode != null)
        {
          newtype = classnode.WithMembers(newmems);
          invariantmethod = (MethodDeclarationSyntax) newtype.Members.Last();
        }
        var structnode = typenode as StructDeclarationSyntax;
        if (structnode != null)
        {
          newtype = structnode.WithMembers(newmems);
          invariantmethod = (MethodDeclarationSyntax) newtype.Members.Last();
        }
        if (newtype == null) { throw new ArgumentException("This node should have been a struct or class"); }
        return newtype; // do not descend into class/struct
      }
    }
    private static ClassDeclarationSyntax AddObjectInvariantsToExistingMethod(MethodDeclarationSyntax old_invariant_method, BlockSyntax new_invariants) 
    {
      Contract.Requires(old_invariant_method != null);
      Contract.Requires(new_invariants != null);

      var parentclass = old_invariant_method.Parent as ClassDeclarationSyntax;
      Contract.Assert(parentclass != null);
      // var new_body = old_invariant_method.Body.AddStatements(new_invariants.Statements); // this doesnt pass the type checker !?
      var new_body = old_invariant_method.Body;
      Contract.Assert(new_body != null);
      foreach (var inv in new_invariants.Statements) 
      {
        new_body = new_body.AddStatements(inv);
      }
      var new_invariant_method = old_invariant_method.WithBody(new_body);
      parentclass = parentclass.ReplaceNode(old_invariant_method, new_invariant_method);
      return parentclass;
    }
    internal static bool isObjectInvariantMethod(MethodDeclarationSyntax methdecl, SemanticModel sm)
    {
      #region CodeContracts
      Contract.Requires(methdecl != null);
      Contract.Requires(sm != null);
      #endregion CodeContracts

      foreach(var attlist in methdecl.AttributeLists) 
      {
        Contract.Assert(attlist != null);
        foreach(var att in attlist.Attributes) 
        {
          //var si = sm.GetSymbolInfo(att);
          var typeInfo = sm.GetTypeInfo(att);
          
          
          //Console.WriteLine(typeInfo.Type);
          //var dci = si.Symbol.GetDocumentationCommentId();
          var dci = typeInfo.Type.MetadataName;
          //if (dci.Equals("M:System.Diagnostics.Contracts.ContractInvariantMethodAttribute.#ctor"))
          if (dci.Equals("ContractInvariantMethod") || dci.Equals("ContractInvariantMethodAttribute"))
            // I don't know why I need both, something seems fishy
          {
            return true;
          }
        }
      }
      return false;
    }
  }
}
