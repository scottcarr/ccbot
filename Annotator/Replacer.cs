using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.Research.CodeAnalysis;
using Microsoft.Research.ReviewBot;

namespace Microsoft.Research.ReviewBot
{
  using System.Diagnostics.Contracts;
  using Microsoft.Research.ReviewBot.Annotations;
  using Microsoft.Research.ReviewBot.Utils;
  static class Replacer
  {
    private static SemanticModel SemanticModel;
    private static Compilation Compilation;
    public static ReplacementDictionary PrecomputeReplacementNodes(SyntaxDictionary annotationsByNode, Compilation compilation)
    {
      #region CodeContracts
      Contract.Requires(annotationsByNode != null);
      Contract.Requires(compilation != null);
      Contract.Ensures(Contract.Result<ReplacementDictionary>() != null);
      #endregion CodeContracts

      Output.WriteLine("Precomputing the replacement nodes");

      //GetContractRequiresSymbols(comp);
      var newdict = new ReplacementDictionary();
      foreach (var oldkvp in annotationsByNode)
      {
        var oldsubdict = oldkvp.Value;
        var oldfile = oldkvp.Key;
        var newsubdict = new Dictionary<SyntaxNode, SyntaxNode>();
        if (!oldsubdict.Any()) { continue; /* TODO is this right? we have a node with no annotations? */ }
        SemanticModel = compilation.GetSemanticModel(oldsubdict.First().Key.SyntaxTree);
        Compilation = compilation;
        foreach (var oldnode in oldsubdict.Keys)
        {
          switch (oldnode.Kind())
          {
            case SyntaxKind.MethodDeclaration:
            case SyntaxKind.ConstructorDeclaration:
            case SyntaxKind.SetAccessorDeclaration:
            case SyntaxKind.GetAccessorDeclaration:
            case SyntaxKind.AddAccessorDeclaration: // who knew this was a thing? maybe this will work
            case SyntaxKind.RemoveAccessorDeclaration: // who knew this was a thing? maybe this will work
              //var oldmethod = oldnode as BaseMethodDeclarationSyntax;
              //var newmethod = PrecomputeNewMethod(oldmethod, oldsubdict[oldnode]);
              //if (oldnode.GetText().ToString().Contains("ObjectInvariant")) { int x; }
              var newmethod = PrecomputeNewMethod(oldnode, oldsubdict[oldnode]);
              newsubdict.Add(oldnode, newmethod);
              continue;
            case SyntaxKind.FieldDeclaration:
              continue; // we don't need to do anything to read only fields at this point
            default:
              RBLogger.Error("Unhandled SyntaxNode kind {0}", oldnode.Kind());
              // Debug.Assert(false); // unhandled annotation type
              continue;
          }
        }
        newdict.Add(oldfile, newsubdict);
      }
      return newdict;
    }
    private static IEnumerable<ISymbol> GetContractMemberSymbols(Compilation compilation, string membername)
    {
      var all_members = GetContractTypeMembers(compilation);
      return all_members.Where(member => member.Name.ToString().Equals(membername));
    }
    private static IEnumerable<ISymbol> GetContractTypeMembers(Compilation compilation)
    {
      var contractType = compilation.GetTypeByMetadataName("System.Diagnostics.Contracts.Contract");
      return contractType.GetMembers();
    }

    [ContractVerification(false)]
    private static SyntaxNode PrecomputeNewMethod(SyntaxNode oldmethod, List<BaseAnnotation> anns, bool useRegion = true) 
    {
      SyntaxList<StatementSyntax> oldstmts;
      if (oldmethod is BaseMethodDeclarationSyntax)
      {
        var casted = oldmethod as BaseMethodDeclarationSyntax;
        if (casted.Body == null)
        {
          // TODO I don't know how to rewrite lambdas
          return oldmethod;
        }
        oldstmts = casted.Body.Statements;
      }
      else if (oldmethod is AccessorDeclarationSyntax)
      {
        var casted = oldmethod as AccessorDeclarationSyntax;
        oldstmts = casted.Body.Statements;
      }
      else
      {
        RBLogger.Error("Unhandled syntax node kind {0}", oldmethod.Kind());
      }
      var newstmtlist = SyntaxFactory.Block();
      var newObjectInvariants = anns.Where(x => x.Kind == ClousotSuggestion.Kind.ObjectInvariant).Select(x => x.statement_syntax as StatementSyntax);
      var oldObjectInvariants = oldstmts.Where(IsInvariant);
      var new_requires = anns.Where(x => x.Kind == ClousotSuggestion.Kind.Requires).Select(x => x.statement_syntax as StatementSyntax);
      var old_requires = oldstmts.Where(IsRequires);
      var new_ensures = anns.Where(x => x.Kind == ClousotSuggestion.Kind.Ensures 
                                                  || x.Kind == ClousotSuggestion.Kind.EnsuresNecessary)
                            .Select(x => x.statement_syntax as StatementSyntax);
      var old_ensures = oldstmts.Where(IsEnsures);
      var new_assumes = anns.Where(x => x.Kind == ClousotSuggestion.Kind.AssumeOnEntry).Select(x => x.statement_syntax as StatementSyntax);
      //var old_eveything_else = oldstmts.Where(x => !IsEnsures(x) && !IsRequires(x) && !IsAssumes(x));
      //var old_eveything_else = oldstmts.Where(x => !IsEnsures(x) && !IsRequires(x));
      //var old_assumes = oldstmts.Where(IsAssumes);
      var old_assumes_list = new List<StatementSyntax>();

      // we only want to consider the old assumes to be the ones at the start of the method
      // assumes can be anywhere, but its incorrect to move ones that use declared variables
      foreach (var stmt in oldstmts)
      {
        if (IsEnsures(stmt)) { continue; }
        if (IsRequires(stmt)) { continue; }
        if (IsAssumes(stmt)) { old_assumes_list.Add(stmt); continue; }
        break;
      }
      var old_assumes = old_assumes_list.AsEnumerable();
      var old_eveything_else = oldstmts.Except(old_ensures.Concat(old_requires).Concat(old_assumes));
      var objectInvariants = newObjectInvariants.Union(oldObjectInvariants, new ContractEqualityComparer());

      //if (objectInvariants.Any()) { Debugger.Break(); }
      
      SyntaxTrivia regionStart, regionEnd;
      regionEnd = regionStart = SyntaxFactory.Comment(""); // a dummy initial value
      if (useRegion && !objectInvariants.Any())
      {
        if (TryFindContractsRegion(old_requires, old_ensures, old_assumes, out regionStart, out regionEnd))
        {
          var first = oldstmts.First(x => x.GetLeadingTrivia().Contains(regionStart));
          //var last = oldstmts.First(x => x.GetTrailingTrivia().Contains(regionEnd));
          //var last = oldstmts.First(x => x.GetLeadingTrivia().Contains(regionEnd));
          //Console.WriteLine(first.Parent.DescendantTrivia().Contains(regionEnd));
          // it seems like the #endregion can be essentially anywhere
          var statements = oldstmts.Where(x => x.GetLeadingTrivia().Contains(regionEnd) || x.GetTrailingTrivia().Contains(regionEnd));
          StatementSyntax last, lastModified;
          last = lastModified = null;
          if (oldstmts.Any(x => x.GetLeadingTrivia().Contains(regionEnd)))
          {
            last = oldstmts.First(x => x.GetLeadingTrivia().Contains(regionEnd));
            var lastTrivia = last.GetLeadingTrivia().Where(x => x != regionEnd);
            lastModified = last.WithLeadingTrivia(lastTrivia);
          }
          else if (oldstmts.Any(x => x.GetTrailingTrivia().Contains(regionEnd)))
          {
            last = oldstmts.First(x => x.GetTrailingTrivia().Contains(regionEnd));
            var lastTrivia = last.GetTrailingTrivia().Where(x => x != regionEnd);
            lastModified = last.WithTrailingTrivia(lastTrivia);

          }
          else if (first.Parent.DescendantTrivia().Contains(regionEnd))
          {
            oldmethod = first.Parent.Parent.ReplaceTrivia(regionEnd, SyntaxFactory.Comment(""));
          }

          var firstTrivia = first.GetLeadingTrivia().Where(x => x != regionStart);
          var firstModified = first.WithLeadingTrivia(firstTrivia);

          ReplaceStatement(first, firstModified, old_requires, old_ensures, old_assumes, old_eveything_else, out old_requires, out old_ensures, out old_assumes, out old_eveything_else);
          if (last != null)
          {
            ReplaceStatement(last, lastModified, old_requires, old_ensures, old_assumes, old_eveything_else, out old_requires, out old_ensures, out old_assumes, out old_eveything_else);
          }
        }
        else
        {
          var addNewLine = !old_requires.Any() && !old_requires.Any();
          GetNewRegionTrivia("CodeContracts", addNewLine, out regionStart, out regionEnd);
        }
      }

      var requires = new_requires.Union(old_requires, new ContractEqualityComparer());
      var ensures = new_ensures.Union(old_ensures, new ContractEqualityComparer());
      var assumes = new_assumes.Union(old_assumes, new ContractEqualityComparer());

      RBLogger.ErrorIf(requires.Count() < new_requires.Count(), "Union deleted some items!?");
      RBLogger.ErrorIf(ensures.Count() < new_ensures.Count(), "Union deleted some items!?");
      RBLogger.ErrorIf(assumes.Count() < new_assumes.Count(), "Union deleted some items!?");

      // Scott: there is some weird case where we get duplicate ensures
      // I haven't tracked it down
      // this is for debugging:
      if (ensures.Count() < new_ensures.Count())
      {
        RBLogger.Info("new ensures:");
        RBLogger.Indent();
        foreach (var ensure in new_ensures) 
        {
          RBLogger.Info(ensure);
        }
        RBLogger.Unindent();
      }

      //foreach(var r in requires)
      //{
      //  Console.WriteLine(r);
      //}
      //Console.WriteLine(requires.Count());

      // the if x.Any()'s are unnecesary (IMHO) but there is a roslyn bug for
      // adding an empty StatementSyntax[] to an empty BlockSyntax
      if (requires.Any())
      {
        newstmtlist = newstmtlist.AddStatements(requires.ToArray());
      }
      if (ensures.Any())
      {
        newstmtlist = newstmtlist.AddStatements(ensures.ToArray());
      }
      if (assumes.Any())
      {
        newstmtlist = newstmtlist.AddStatements(assumes.ToArray());
      }
      if (old_eveything_else.Any())
      {
        newstmtlist = newstmtlist.AddStatements(old_eveything_else.ToArray());
      }
      if (objectInvariants.Any())
      {
        newstmtlist = newstmtlist.AddStatements(objectInvariants.ToArray()); // object invariant methods should only have these, so order doesn't matter
      }

      if (useRegion && !objectInvariants.Any())
      {
        var first = newstmtlist.Statements.First();
        var oldTrivia = first.GetLeadingTrivia();
        var newTrivia = SyntaxFactory.TriviaList(regionStart).Concat(oldTrivia);
        var firstModified = first.WithLeadingTrivia(newTrivia);
        newstmtlist = newstmtlist.ReplaceNode(first, firstModified);
        
        var index = requires.Count() + ensures.Count() + assumes.Count();
        var last = newstmtlist.Statements[index - 1];
        oldTrivia = last.GetTrailingTrivia();
        newTrivia = oldTrivia.Concat(SyntaxFactory.TriviaList(new[] { regionEnd }));
        var lastModified = last.WithTrailingTrivia(newTrivia);
        newstmtlist = newstmtlist.ReplaceNode(last, lastModified);
      }
      SyntaxNode newmethod = null;
      if (oldmethod is MethodDeclarationSyntax)
      {
        var casted = oldmethod as MethodDeclarationSyntax;
        //newmethod = casted.WithBody(newstmtlist);
        // the awful line below is partly so awful to preserve the trivia around the method
        newmethod = casted.WithBody(casted.Body.WithStatements(newstmtlist.Statements));
      }
      if (oldmethod is ConstructorDeclarationSyntax)
      {
        var casted = oldmethod as ConstructorDeclarationSyntax;
        //newmethod = casted.WithBody(newstmtlist);
        newmethod = casted.WithBody(casted.Body.WithStatements(newstmtlist.Statements));
      }
      if (oldmethod is AccessorDeclarationSyntax)
      {
        var casted = oldmethod as AccessorDeclarationSyntax;
        //newmethod = casted.WithBody(newstmtlist);
        newmethod = casted.WithBody(casted.Body.WithStatements(newstmtlist.Statements));
      }
      //var newmethod = oldmethod is MethodDeclarationSyntax ? (BaseMethodDeclarationSyntax)((MethodDeclarationSyntax)oldmethod).WithBody(newstmtlist) :
      //  (BaseMethodDeclarationSyntax)((ConstructorDeclarationSyntax)oldmethod).WithBody(newstmtlist);
      //Console.WriteLine("Annotated Method: {0}", newmethod);
      return newmethod;
    }
    private static void ReplaceStatement(StatementSyntax original,
                                         StatementSyntax replacement,
                                         IEnumerable<StatementSyntax> requires,
                                         IEnumerable<StatementSyntax> ensures,
                                         IEnumerable<StatementSyntax> assumes,
                                         IEnumerable<StatementSyntax> other,
                                         out IEnumerable<StatementSyntax> newrequires,
                                         out IEnumerable<StatementSyntax> newensures,
                                         out IEnumerable<StatementSyntax> newassumes,
                                         out IEnumerable<StatementSyntax> newother)
    {
      newassumes = assumes;
      newrequires = requires;
      newensures = ensures;
      newother = other;
      if (IsEnsures(original))
      {
        newensures = ensures.Select(x => x == original ? replacement : x);
      }
      else if (IsRequires(original))
      {
        newrequires = requires.Select(x => x == original ? replacement : x);
      }
      else if (IsAssumes(original))
      {
        newassumes = assumes.Select(x => x == original ? replacement : x);
      }
      else if (other.Contains(original))
      {
        newother = other.Select(x => x == original ? replacement : x);
      }
      else
      {
        RBLogger.Error("The original statement should have been an ensure, require, or assume");
        Contract.Assume(false);
      }
    }
    private static bool IsRequires(StatementSyntax node)
    {
      return IsContract("Requires", node);
    }
    private static bool IsEnsures(StatementSyntax node)
    {
      return IsContract("Ensures", node);
    }
    private static bool IsInvariant(StatementSyntax node)
    {
      return IsContract("Invariant", node);
    }
    private static bool IsAssumes(StatementSyntax node)
    {
      return IsContract("Assume", node);
    }
    private static bool IsContract(string contract_type, SyntaxNode node)
    {

      IEnumerable<ISymbol> symbols = GetContractMemberSymbols(Compilation, contract_type);
      if (node.Kind() == SyntaxKind.ExpressionStatement)
      {
        var invoc = node.ChildNodes().First() as InvocationExpressionSyntax;
        if (invoc != null)
        {
          var memacc = invoc.Expression as MemberAccessExpressionSyntax;
          if (memacc != null && memacc.Name.ToString().Equals(contract_type))
          {
            var si = SemanticModel.GetSymbolInfo(invoc);
            if (si.Symbol != null)
            {
              if (symbols.Where(x => si.Symbol.Equals(x)).Any())
              {
                return true;
              }
            }
          }
        }
      }
      return false;
    }
    public static Compilation RewriteCompilation(Compilation original, ReplacementDictionary dict_old_to_new)
    {
      Contract.Requires(dict_old_to_new != null);
      Contract.Ensures(Contract.Result<Compilation>() != null);

      Output.WriteLine("Rewriting the compilation");

      var curr = original.Clone();
      Contract.Assume(curr != null);
      foreach (var st in original.SyntaxTrees)
      {
        Dictionary<SyntaxNode, SyntaxNode> subdict;
        if (dict_old_to_new.TryGetValue(st.FilePath, out subdict))
        {
          var r = new ReplacerInternal(subdict, original);
          var newtree = r.Replace(st);
          if (newtree != st) // did something change?
          {
            newtree = AddUsingsContracts(newtree);
            curr = curr.ReplaceSyntaxTree(st, SyntaxFactory.SyntaxTree(newtree.GetRoot(), null, st.FilePath));
            Contract.Assume(curr != null);
          }
        }
      }
      return curr;
      //var r = new Replacer(dict_old_to_new, original);
      //foreach (var st in original.SyntaxTrees)
      //{
      //  //var newnode = SyntaxNodeExtensions.ReplaceNodes(st.GetRoot(), dict_old_to_new.Keys, ComputeReplacement);
      //  var newtree = r.Replace(st);
      //  if (newtree != st) // did something change?
      //  {
      //    newtree = AddUsingsContracts(newtree);
      //    curr = curr.ReplaceSyntaxTree(st, newtree);
      //  }
      //}
    }
    internal static SyntaxTree AddUsingsContracts(SyntaxTree orig) 
    {
      // now just add it and let RemoveUnnecessaryImports clean it up if it was already there
      var cu = orig.GetRoot().SyntaxTree.GetCompilationUnitRoot();
      var usecon = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Diagnostics.Contracts"));
      return cu.AddUsings(usecon).SyntaxTree;
      //var cu = orig.GetRoot().SyntaxTree.GetCompilationUnitRoot();

      //if (cu.Usings.All(u => u.Name.ToFullString() != StringConstants.Contract.ContractsNamespace))
      //{
      //  var usecon = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(StringConstants.Contract.ContractsNamespace));
      //  return cu.AddUsings(usecon).SyntaxTree;
      //}

      // "using System.Diagnostics.Contracts" is already there
      //return orig;
    }
    private static bool TryFindContractsRegion(IEnumerable<StatementSyntax> requires, 
                                               IEnumerable<StatementSyntax> ensures, 
                                               IEnumerable<StatementSyntax> assumes, 
                                               out SyntaxTrivia regionStart,
                                               out SyntaxTrivia regionEnd)
    {
      regionStart = regionEnd = SyntaxFactory.Comment(""); // not a valid value
      StatementSyntax firstContract;
      if (TryFindFirstContract(requires, ensures, assumes, out firstContract))
      {
        if (firstContract.GetLeadingTrivia().Any(x => x.IsKind(SyntaxKind.RegionDirectiveTrivia)))
        {
          regionStart = firstContract.GetLeadingTrivia().First(x => x.IsKind(SyntaxKind.RegionDirectiveTrivia));
          var firstStructure = regionStart.GetStructure() as RegionDirectiveTriviaSyntax;
          var endStructure = firstStructure.GetRelatedDirectives()[1]; // the 1st index is the start, the 2nd is the end
          var endRegions = firstContract.Parent.DescendantTrivia().Where(x => x.IsKind(SyntaxKind.EndRegionDirectiveTrivia));
          var endRegionsCasted = endRegions.Cast<SyntaxTrivia>();
          if (!endRegionsCasted.Any())
          {
            return false;
          }
          regionEnd = endRegionsCasted.First(x => ((EndRegionDirectiveTriviaSyntax)x.GetStructure()).GetRelatedDirectives()[1] == endStructure);
          return true;
        }
      }
      return false;
    }

    private static bool TryFindFirstContract(IEnumerable<StatementSyntax> requires, 
                                             IEnumerable<StatementSyntax> ensures, 
                                             IEnumerable<StatementSyntax> assumes, 
                                             out StatementSyntax first)
    {
      first = null;
      if (requires.Any())
      {
        first = requires.First();
        return true;
      }
      if (ensures.Any())
      {
        first = ensures.First();
        return true;
      }
      if (assumes.Any())
      {
        first = assumes.First();
        return true;
      }
      return false;
    }
    private static bool TryFindLastContract (IEnumerable<StatementSyntax> requires, 
                                             IEnumerable<StatementSyntax> ensures, 
                                             IEnumerable<StatementSyntax> assumes, 
                                             out StatementSyntax last)
    {
      last = null;
      if (assumes.Any())
      {
        last = assumes.Last();
        return true;
      }
      if (ensures.Any())
      {
        last = ensures.Last();
        return true;
      }
      if (requires.Any())
      {
        last = requires.Last();
        return true;
      }
      return false;
    }
    private static void GetNewRegionTrivia(string name, bool addnewline, out SyntaxTrivia start, out SyntaxTrivia end)
    {
      var regionStart = SyntaxFactory.RegionDirectiveTrivia(true);
      var codeContractsPreprocessMessage = SyntaxFactory.TriviaList(SyntaxFactory.PreprocessingMessage(String.Format(" {0} \r\n", name)));
      var endToken = SyntaxFactory.Token(codeContractsPreprocessMessage, SyntaxKind.EndOfDirectiveToken, SyntaxFactory.TriviaList());
      regionStart = regionStart.WithEndOfDirectiveToken(endToken);
      start = SyntaxFactory.Trivia(regionStart);

      var regionEnd = SyntaxFactory.EndRegionDirectiveTrivia(true);
      if (addnewline)
      {

        endToken = SyntaxFactory.Token(codeContractsPreprocessMessage, SyntaxKind.EndOfDirectiveToken, SyntaxFactory.TriviaList(SyntaxFactory.LineFeed));
      }
      else
      {
        endToken = SyntaxFactory.Token(codeContractsPreprocessMessage, SyntaxKind.EndOfDirectiveToken, SyntaxFactory.TriviaList());
      }
      regionEnd = regionEnd.WithEndOfDirectiveToken(endToken);
      end = SyntaxFactory.Trivia(regionEnd);
    }
    private class ReplacerInternal
    {
      private readonly Dictionary<SyntaxNode, SyntaxNode> dict_old_to_new;
      private readonly Compilation comp;
      //private readonly List<SyntaxNode> changes;
      public ReplacerInternal(Dictionary<SyntaxNode, SyntaxNode> dict_old_to_new, Compilation comp)
      {
        this.dict_old_to_new = dict_old_to_new;
        this.comp = comp;
      }
      public SyntaxTree Replace(SyntaxTree st) 
      {
        #region CodeContracts
        Contract.Requires(st != null);
        Contract.Ensures(Contract.Result<SyntaxTree>() != null);
        #endregion CodeContracts

        return SyntaxNodeExtensions.ReplaceNodes(st.GetRoot(), dict_old_to_new.Keys, ComputeReplacement).SyntaxTree;
      }
      private SyntaxNode ComputeReplacement(SyntaxNode old1, SyntaxNode old2)
      {
        // old1 is the original node in the tree
        // old2 is the (potentially) modified version of old1
        // that is, old1 is in dict_old_to_new but old2 wont be
        // if any of its chilrden have been modified
        if (dict_old_to_new.Keys.Contains(old1))  // this check is redundant?
        {
          switch (old2.Kind())
          {
            case SyntaxKind.GetAccessorDeclaration:
            case SyntaxKind.SetAccessorDeclaration:
            case SyntaxKind.ConstructorDeclaration:
            case SyntaxKind.MethodDeclaration:
            case SyntaxKind.AddAccessorDeclaration: // who knew this was a thing? maybe this will work
            case SyntaxKind.RemoveAccessorDeclaration: // who knew this was a thing? maybe this will work
              //Console.WriteLine("Replacing {0} with {1}", old2, dict_old_to_new[old2]);
              return dict_old_to_new[old2];
            default:
              RBLogger.Error("Unhandled syntax node kind {0}", old2.Kind());
              break;
          }
        }
        return old2; // don't replace
      }
    }
    private class ContractEqualityComparer : EqualityComparer<StatementSyntax>
    {
      public override bool Equals(StatementSyntax s1, StatementSyntax s2)
      {
        #region CodeContracts
        Contract.Assume(s1 != null);
        Contract.Assume(s2 != null);
        #endregion CodeContracts

        var s1txt = s1.GetText().ToString().Trim();
        var s2txt = s2.GetText().ToString().Trim();
        return s1txt.Equals(s2txt);
      }
      public override int GetHashCode(StatementSyntax s)
      {
        Contract.Assume(s != null);
        return s.GetText().ToString().Trim().GetHashCode();
      }
    }
  }
}
