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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics.Contracts;


namespace Microsoft.Research.ReviewBot
{
  public static class CommentHelpers
  {
    // F: Moved this one in the comments
    //public const string CommentsDirectory = @"\\msr\public\logozzo\ReviewBot\\Comments";
    
    // None of this is used anymore, but I put a lot of work into tracking that location of the 
    // suggestion after inserting the contracts, so I feel bad about deleting it
    /*
    public static void WriteCommentsFile(CCCheckOutput cccheckXML, Compilation originalcompilation, Compilation finalcompilation, string depotroot) 
    {
      #region CodeContracts
      Contract.Requires(cccheckXML != null);
      Contract.Requires(originalcompilation != null);
      Contract.Requires(finalcompilation != null);
      Contract.Requires(!string.IsNullOrEmpty(depotroot));
      #endregion CodeContracts

      Contract.Assert(Directory.Exists(depotroot));
      Contract.Assert(cccheckXML.Items != null);
      var outFile = File.Create(CommentsFile);
      var xf = new XmlSerializer(typeof(List<Comment>));
      var comments = GetComments(cccheckXML, originalcompilation, finalcompilation, depotroot);
      xf.Serialize(outFile, comments);
      outFile.Close();
      outFile.Dispose();
    }

    private static IEnumerable<Comment> GetComments(CCCheckOutput cccheckXML, Compilation originalcompilation, Compilation finalcompilation, string depotroot)
    {
      #region CodeContracts
      Contract.Ensures(Contract.Result<IEnumerable<Comment>>() != null);
      #endregion CodeContracts

      var comments = new List<Comment>();
      var assemblies = cccheckXML.Items.OfType<CCCheckOutputAssembly>();
      foreach (var assembly in assemblies.AssumeNotNull())
      {
        foreach (var methodgroup in assembly.Method.GroupBy(x => x.Name))
        {
          var method = methodgroup.Last();
          if (method.Check != null)
          {
            foreach (var check in method.Check)
            {
              Comment comment;
              if (TryMakeComment(check, originalcompilation, finalcompilation, depotroot, out comment))
              {
                comments.Add(comment);
              }
            }
          }
        }
      }
      return comments;
    }
    private static bool TryMakeComment(CCCheckOutputAssemblyMethodCheck check, Compilation originalcompilation, Compilation finalcompilation, string depotroot, out Comment comment)
    {
      #region CodeContracts
      Contract.Requires(check != null);
      Contract.Requires(check.SourceLocation != null);
      Contract.Requires(check.Message != null);
      Contract.Requires(originalcompilation != null);
      Contract.Requires(finalcompilation != null);
      Contract.Requires(!string.IsNullOrEmpty(depotroot));
      Contract.Ensures(!Contract.Result<bool>() || Contract.ValueAtReturn(out comment) != null);
      #endregion CodeContracts

      comment = null;
      var location = check.SourceLocation;
      var index = location.IndexOf("(");
      if (index < 0) { return false; } // the location string should be like: "c:\foo.cs(10,2-12,0)"
      var fileName = location.Remove(index);
      var positionText = location.Replace(fileName, "");
      positionText = positionText.Trim('(');
      positionText = positionText.Trim(')');
      var parts = positionText.Split('-'); // ex: "23,2-182,33" => ["23,2","182,33"]
      var split = parts.Aggregate(new List<String>(), (acc, x) => acc.Concat(x.Split(',')).ToList()); // ex: ["23,2","182,33"] => ["23", "2", "182", "33"]
      List<int> numbers;
      try
      {
        numbers = split.Select(int.Parse).ToList(); // ex: ["23", "2", "182", "33"] => [23, 2, 182, 33]
        // this might throw an exception because some of the comments in the xml are invalid
      }
      catch (FormatException) { return false; }
      if (File.Exists(fileName))
      {
        var startLinePositon = new LinePosition(numbers[0], numbers[1]);
        var endLinePositon = new LinePosition(numbers[2], numbers[3]);
        var fileLinePosition = new FileLinePositionSpan(fileName, startLinePositon, endLinePositon);
        SyntaxNode originalNode;
        if (TryFindOverlappingNode(fileLinePosition, originalcompilation, out originalNode))
        {
          var sm = originalcompilation.GetSemanticModel(originalNode.SyntaxTree);
          Contract.Assert(sm != null);
          //var originalParent = originalNode.Parent.Parent;
          SyntaxNode originalParent;
          if (!TryGetParent(originalNode, out originalParent))
          {
            RBLogger.Error("Failed to find parent of {0}", originalNode);
            return false;
          }
          var originalParentDCI = sm.GetDeclaredSymbol(originalParent).GetDocumentationCommentId();
          var finalTree = finalcompilation.SyntaxTrees.First(x => x.FilePath.Equals(fileLinePosition.Path, StringComparison.OrdinalIgnoreCase));
          Contract.Assert(finalTree != null);
          SyntaxNode finalNode;
          try
          {
            var finalSM = finalcompilation.GetSemanticModel(finalTree);
            Contract.Assert(finalSM != null);
            //finalNode = finalTree.GetRoot().DescendantNodesAndSelf().First(x => x.IsEquivalentTo(originalNode));
            //finalNode = finalTree.GetRoot().DescendantNodesAndSelf().First(x => x.CSharpKind().Equals(originalNode.CSharpKind())
            //  && x.GetText().ToString().Equals(originalNode.GetText().ToString()));
            var finalParent = finalTree.GetRoot().DescendantNodesAndSelf().First(x => x.CSharpKind().Equals(originalParent.CSharpKind())
              && finalSM.GetDeclaredSymbol(x).GetDocumentationCommentId().Equals(originalParentDCI));
            var sameKind = finalParent.DescendantNodesAndSelf().Where(x => x.CSharpKind().Equals(originalNode.CSharpKind()));
            //var originalText = originalNode.WithLeadingTrivia(new SyntaxTriviaList()).WithTrailingTrivia(new SyntaxTriviaList()).GetText().ToString().Trim();
            var originalText = GetTextWithStrippedTrivia(originalNode);
            finalNode = sameKind.First(x => GetTextWithStrippedTrivia(x).Equals(originalText) && ParentKindsMatch(originalNode, x));
            //finalNode = sameKind.First(x => x.IsEquivalentTo(originalNode));
          }
          catch 
          {
            //var stmt = originalNode.DescendantNodesAndSelf().First(x => x.CSharpKind().Equals(SyntaxKind.ReturnStatement));
            //Console.WriteLine("return span: {0}", stmt.GetLocation().GetLineSpan(false));
            //Console.WriteLine("return span: {0}", stmt.GetLocation().GetLineSpan(true));
            RBLogger.Error("Failed to find equivalent node for: {0}", originalNode);
            return false;
          }
          Contract.Assert(finalNode != null);
          comment = new Comment();
          comment.Message = check.Message;
          //comment.StartChar = finalNode.Span.Start;
          //comment.EndChar = finalNode.Span.End;
          //var linespan = finalNode.GetLocation().GetLineSpan();
          var linespan = finalNode.GetLocation().AssumeNotNull().GetLineSpan(true);
          comment.StartLine = linespan.Span.Start.Line + 1; // roslyn appears to start counting lines from 0
          comment.EndLine = linespan.Span.End.Line + 1; // roslyn appears to start counting lines from 0
          // roslyn's node.GetLocation().GetLineSpan() appears to be unreliable
          // I'm going to assume the horizontal position of the comment shouldn't change
          comment.StartChar = fileLinePosition.Span.Start.Character - 1;
          comment.EndChar = fileLinePosition.Span.End.Character - 1;
          comment.Path = RelativizePath(fileLinePosition.Path, depotroot);
          //RBLogger.Info("Comment location: {0}", finalNode.GetLocation().GetLineSpan());
          RBLogger.Info("Comment location: {0}", finalNode.GetLocation().AssumeNotNull().GetLineSpan(true));
          return true;
        }
        else
        {
          RBLogger.Error("Unable to find a node at {0}", check.SourceLocation);
          return false;
        }
      } // if the file doesn't exist, this is not a valid check/comment
      RBLogger.Error("Got a suggestion for {0} but the file doesn't exist", fileName);
      return false;
    }
    private static bool ParentKindsMatch(SyntaxNode a, SyntaxNode b)
    {
      var parentA = a.Parent;
      var parentB = b.Parent;
      while (parentA != null && parentB != null)
      {
        if (parentA.CSharpKind() != parentB.CSharpKind()) { return false; }
        parentA = parentA.Parent;
        parentB = parentB.Parent;
      }
      return true;
    }
    /// <summary>
    /// Walk the tree of parents upwards until you get to a Method, Constructor, Accessor, or class
    /// </summary>
    /// <param name="child">The node where to start</param>
    /// <param name="parent">The parent node which will be a Method, Constructor, Accessor or class</param>
    /// <returns>true if a parent of one of those types was found, false if we go to a null parent</returns>
    private static bool TryGetParent(SyntaxNode child, out SyntaxNode parent)
    {
      #region CodeContracts
      Contract.Requires(child != null);
      Contract.Ensures(!Contract.Result<bool>() || Contract.ValueAtReturn(out parent) != null);
      #endregion CodeContracts

      parent = child;
      var parentKind = parent.CSharpKind();
      while (!(parentKind == SyntaxKind.MethodDeclaration 
        || parentKind == SyntaxKind.ConstructorDeclaration 
        || parentKind == SyntaxKind.GetAccessorDeclaration
        || parentKind == SyntaxKind.SetAccessorDeclaration
        || parentKind == SyntaxKind.ClassDeclaration))
      {
        parent = parent.Parent;
        if (parent == null) { return false; }
        parentKind = parent.CSharpKind();
      }
      return true;
    }
    private static string GetTextWithStrippedTrivia(SyntaxNode node)
    {
      #region CodeContracts
      Contract.Requires(node != null);
      Contract.Ensures(Contract.Result<string>() != null);
			#endregion CodeContracts

      var text = node.WithTrailingTrivia(new SyntaxTriviaList()).WithLeadingTrivia(new SyntaxTriviaList()).GetText().ToString().Trim().Replace(" ", "");
      //Console.WriteLine(text);
      return text;
      //return node.WithTrailingTrivia(new SyntaxTriviaList()).WithLeadingTrivia(new SyntaxTriviaList()).GetText().ToString().Trim();
    }
    private static string RelativizePath(string path, string depotroot)
    {
      #region CodeContracts
      Contract.Requires(!string.IsNullOrEmpty(path));
      Contract.Requires(!string.IsNullOrEmpty(depotroot));
      Contract.Ensures(!string.IsNullOrEmpty(Contract.Result<string>()));
      #endregion CodeContracts

      var pathForward = path.Replace("\\", "/");
      var depotrootForward = depotroot.Replace("\\", "/");
      var rel = Regex.Replace(pathForward, depotrootForward, "", RegexOptions.IgnoreCase);
      return "/" + rel;
    }
    private static bool TryFindOverlappingNode(FileLinePositionSpan filelineposition, Compilation compilation, out SyntaxNode node)
    {
      #region CodeContracts
      Contract.Requires(filelineposition.Path != null);
      Contract.Requires(compilation != null);
      Contract.Ensures(!Contract.Result<bool>() || Contract.ValueAtReturn(out node) != null);
      Contract.Assume(compilation.SyntaxTrees != null);
      Contract.Assume(Contract.ForAll(compilation.SyntaxTrees, tree => tree.FilePath != null));
      #endregion CodeContracts

      var syntaxTree = compilation.SyntaxTrees.First(x => x.FilePath.Equals(filelineposition.Path, StringComparison.OrdinalIgnoreCase));
      Contract.Assert(syntaxTree != null);
      var root = syntaxTree.GetRoot();
      node = FindNode(root, filelineposition);
      if (node == null) 
      {
        return false;
      }
      return true;
    }
    private static SyntaxNode FindNode(SyntaxNode root, FileLinePositionSpan filelineposition)
    {
      var nf = new NodeFinder(filelineposition);
      nf.Visit(root);
      return nf.Winner;
    }
    private class NodeFinder : SyntaxWalker
    {
      public SyntaxNode Winner {get; private set;}
      private FileLinePositionSpan WinnerSpan;
      private readonly FileLinePositionSpan TargetSpan;
      public NodeFinder(FileLinePositionSpan target) : base()
      {
        this.Winner = null;
        this.TargetSpan = target;
      }
      public override void Visit(SyntaxNode node)
      {
        #region CodeContracts
        Contract.Assume(node != null);
        #endregion CodeContracts

        //var currentSpan = node.GetLocation().GetLineSpan();
        var currentSpan = node.GetLocation().AssumeNotNull().GetLineSpan(true);
        if (IsContainer(TargetSpan, currentSpan, true))
        {
          if (IsNewWinner(currentSpan))
          {
            WinnerSpan = currentSpan;
            Winner = node;
          }
          base.Visit(node);
        }
      }
      /// <summary>
      /// Returns true if target is completely contained within current
      /// </summary>
      /// <param name="target"></param>
      /// <param name="current"></param>
      /// <param name="subtractone">subtracts one, for zero based indexing of the target span.  Roslyn is zero based.</param>
      /// <returns></returns>
      private bool IsContainer(FileLinePositionSpan target, FileLinePositionSpan current, bool subtractone)
      {
        int targetStartLine, targetStartChar, targetEndLine, targetEndChar;
        if (subtractone)
        {
          targetStartLine = target.StartLinePosition.Line - 1;
          targetStartChar = target.StartLinePosition.Character - 1;
          targetEndLine = target.EndLinePosition.Line - 1;
          targetEndChar = target.EndLinePosition.Character - 1;
        }
        else
        {
          targetStartLine = target.StartLinePosition.Line;
          targetStartChar = target.StartLinePosition.Character;
          targetEndLine = target.EndLinePosition.Line;
          targetEndChar = target.EndLinePosition.Character;
        }

        var currentStartLine = current.StartLinePosition.Line;
        var currentStartChar = current.StartLinePosition.Character;
        var currentEndLine = current.EndLinePosition.Line;
        var currentEndChar = current.EndLinePosition.Character;

        var earlierStart = currentStartLine < targetStartLine || (currentStartLine == targetStartLine && currentStartChar <= targetStartChar);
        var laterEnd = currentEndLine > targetEndLine || (currentEndLine == targetEndLine && currentEndChar >= targetEndChar);

        return earlierStart && laterEnd;

      }
      private bool IsNewWinner(FileLinePositionSpan current)
      {
        if (WinnerSpan.Equals(default(FileLinePositionSpan))) { return true; }
        return IsContainer(current, WinnerSpan, false);
      }
    }
    */
  }
}