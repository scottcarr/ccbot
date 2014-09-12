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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.Research.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace Microsoft.Research.ReviewBot.Annotations
{
  using System.Diagnostics.Contracts;
  //using CCCheckExtraInfo = MethodSuggestionSuggestionExtraInfo;
  using CCCheckExtraInfo = CCCheckOutputAssemblyMethodSuggestionSuggestionExtraInfo;
  using System.Reflection;
  using Microsoft.Research.ReviewBot.Utils;
  internal class UnresolvedInterfaceAnnotation : BaseAnnotation
  {
    public readonly String InterfaceName;
    public readonly String InterfaceShortName;
    public readonly String InterfaceMethod;
    //public readonly CCCheckExtraInfo extraInfo;
    //public readonly bool IsParameterized;
    //public readonly int NumberParameters;
    public readonly string ParametersString;
    //public UnresolvedInterfaceAnnotation(
    //                            CCCheckExtraInfo extra_info,
    //                            Squiggle squiggle, 
    //                            ClousotSuggestion.Kind kind) 
    // : base(null, null, extra_info.SuggestedCode, squiggle, kind)
    //{
    //  // TODO there are ways this can fail to create a valid annotation
    //  // there is a lot of string parsing going on that could go wrong
    //  // could be refactored into a TryXXXX?
    //  //this.extraInfo = extra_info;
    //  this.InterfaceMethod = extra_info.CalleeDocumentId;
    //  this.InterfaceName = extra_info.TypeDocumentId;
    //  if (InterfaceName.Contains('<'))
    //  {
    //    var lastAngle = InterfaceName.LastIndexOf('<');
    //    var beforeAngle = InterfaceName.Remove(lastAngle);
    //    var afterAngle = InterfaceName.Replace(beforeAngle, "");
    //    InterfaceName = beforeAngle;
    //    var parts = afterAngle.Trim('<', '>').Split(',');
    //    NumberParameters = parts.Count();
    //    ParametersString = "<" + parts[0].Replace("type parameter.", "");
    //    for (int i = 1; i < parts.Length; i++)
    //    {
    //      ParametersString += ',' + parts[i].Replace("type parameter.", "");
    //    }
    //    ParametersString += ">";
    //  }
    //  this.InterfaceShortName = InterfaceAnnotationHelpers.GetUnqualifiedName(InterfaceName);
    //}
    public UnresolvedInterfaceAnnotation(string annotation, Squiggle squiggle, ClousotSuggestion.Kind kind, string parameterstring, string interfacename, string interfacemethod)
      : base(null, null, annotation, squiggle, kind)
    {
      this.ParametersString = parameterstring;
      this.InterfaceName = interfacename;
      this.InterfaceMethod = interfacemethod;
      this.InterfaceShortName = InterfaceAnnotationHelpers.GetUnqualifiedName(interfacename);
    }
  }
  /// <summary>
  /// Once we've created/found the contracts class, we treat the InterfaceAnnotation as a BaseAnnotation
  /// </summary>
  internal class ResolvedInterfaceAnnotation : BaseAnnotation
  {
    public readonly UnresolvedInterfaceAnnotation OriginalAnnotation;
    public ResolvedInterfaceAnnotation(string filename, 
                                        string methodname, 
                                        string annotation, 
                                        Squiggle squiggle, 
                                        ClousotSuggestion.Kind kind,
                                        UnresolvedInterfaceAnnotation originalAnnotation)
      : base(filename, methodname, annotation, squiggle, kind)
    {
      #region CodeContracts
      Contract.Requires(methodname != null);
      Contract.Requires(filename != null);
      Contract.Requires(kind == ClousotSuggestion.Kind.EnsuresNecessary);
      #endregion CodeContracts

      this.OriginalAnnotation = originalAnnotation;
    }
  }
  internal static class InterfaceAnnotationHelpers 
  {
    internal static bool TryMakeUnresolvedInterfaceAnnotation(CCCheckExtraInfo extrainfo, Squiggle squiggle, ClousotSuggestion.Kind kind, out UnresolvedInterfaceAnnotation annotation)
    {
      #region CodeContracts
      Contract.Requires(extrainfo != null);
      Contract.Requires(extrainfo.CalleeDocumentId != null);
      Contract.Requires(extrainfo.TypeDocumentId != null);
      Contract.Requires(extrainfo.SuggestedCode != null);
      Contract.Requires(squiggle != null);
      Contract.Requires(kind == ClousotSuggestion.Kind.EnsuresNecessary);
      Contract.Ensures(!Contract.Result<bool>() || Contract.ValueAtReturn<UnresolvedInterfaceAnnotation>(out annotation) != null);
      #endregion CodeContracts

      annotation = null;
      var InterfaceMethod = extrainfo.CalleeDocumentId;
      var InterfaceName = extrainfo.TypeDocumentId;
      var ParametersString = "";
      if (InterfaceName.Contains('<'))
      {
        var lastAngle = InterfaceName.LastIndexOf('<');
        var beforeAngle = InterfaceName.Remove(lastAngle);
        var afterAngle = InterfaceName.Replace(beforeAngle, "");
        if (string.IsNullOrEmpty(afterAngle))
        {
          RBLogger.Error("Failed to parse interface name {0}", InterfaceName);
          return false;
        }
        InterfaceName = beforeAngle;
        var parts = afterAngle.Trim('<', '>').Split(',');
        ParametersString = "<" + parts[0].Replace("type parameter.", "");
        for (int i = 1; i < parts.Length; i++)
        {
          ParametersString += ',' + parts[i].Replace("type parameter.", "");
        }
        ParametersString += ">";
      }
      annotation = new UnresolvedInterfaceAnnotation(extrainfo.SuggestedCode, squiggle, kind, ParametersString, InterfaceName, InterfaceMethod);
      return true;
    }

    /// <summary>
    /// Take all the annotations, select only the interface annotations, create their contract classes (if necessary),
    /// resolve the contract class name and contract class method names
    /// </summary>
    /// <param name="annotations"></param>
    /// <param name="project"></param>
    /// <param name="compilation"></param>
    /// <param name="resolvedannotations">All the annotations with the interface annotation fields completed</param>
    /// <returns>The compilation with potentially new empty contract classes</returns>
    internal static Compilation CreateOrFindContractsClasses(IEnumerable<BaseAnnotation> annotations,
                                                             Project project,
                                                             Compilation compilation,
                                                             out IEnumerable<BaseAnnotation> resolvedannotations)
    {
      #region CodeContracts
      Contract.Requires(annotations != null);
      Contract.Requires(project != null);
      Contract.Requires(compilation != null);
      Contract.Ensures(Contract.ValueAtReturn<IEnumerable<BaseAnnotation>>(out resolvedannotations) != null);
      Contract.Ensures(Contract.Result<Compilation>() != null);
      #endregion CodeContracts

      Output.WriteLine("Gathering (and creating) contract classes for interfaces and abstract classes");

      var resolvedList = annotations.Where(x => !(x is UnresolvedInterfaceAnnotation)).ToList();
      foreach (var interfaceGroup in annotations.OfType<UnresolvedInterfaceAnnotation>().GroupBy(x => x.InterfaceName))
      {
        var first = interfaceGroup.First();
        var interfaceSymbol = InterfaceAnnotationHelpers.GetInterfaceSymbol(project, compilation, first.InterfaceName, first.InterfaceShortName);
        string contractName, fileName, interfaceName;
        interfaceName = first.InterfaceName;
        if (!InterfaceAnnotationHelpers.TryFindContractsClass(compilation, interfaceSymbol, out contractName, out fileName))
        {
          //fileName = first.FileName;
          fileName = interfaceSymbol.Locations[0].SourceTree.FilePath; // hopefully index 0 is where the declaration is?
          var interfaceMethod = first.InterfaceMethod;
          contractName = GetCreatedContractClassName(interfaceName, interfaceMethod);
          compilation = InterfaceAnnotationHelpers.MakeContractsClassFor(project, compilation, interfaceName, fileName, first.ParametersString);
          compilation = InterfaceAnnotationHelpers.ImplementContractsClass(project, compilation, fileName, interfaceName, contractName);
          var syntaxTree = compilation.SyntaxTrees.First(x => x.FilePath.Equals(fileName, StringComparison.OrdinalIgnoreCase));
          var newSyntaxTree = Replacer.AddUsingsContracts(syntaxTree);
          compilation = compilation.ReplaceSyntaxTree(syntaxTree, SyntaxFactory.SyntaxTree(newSyntaxTree.GetRoot(), fileName));
          compilation = InterfaceAnnotationHelpers.AddContractsClassAttributeToInterface(project, compilation, fileName, interfaceName, first.ParametersString);
        }
        foreach (var methodgroup in interfaceGroup.GroupBy(annotation => annotation.InterfaceMethod))
        {
          first = methodgroup.First();
          var methodName = InterfaceAnnotationHelpers.GetMethodName(first.InterfaceName, first.InterfaceMethod, contractName);
          resolvedList.AddRange(methodgroup.Select(x => new ResolvedInterfaceAnnotation(fileName, methodName, x.Annotation, x.Squiggle, x.Kind, x)));
        }
        //var methodName = InterfaceAnnotationHelpers.GetMethodName(first.InterfaceName, first.InterfaceMethod, contractName);
        //resolvedList.AddRange(interfaceGroup.Select(x => new ResolvedInterfaceAnnotation(fileName, methodName, x.Annotation, x.Squiggle, x.Kind, x)));
        //return compilation;
        //compilation = first.MakeOrFindContractsClass(project, compilation);
        //foreach (var intf in interfaceGroup)
        //{
        //  //intf.CopyFileNameAndContractsClass(first);
        //  
        //}
        //foreach (var grp in interfaceGroup.GroupBy(x => x.MethodName))
        //{
        //  first = grp.First();
        //  first.FindMethodName(compilation);
        //  foreach (var intf in grp)
        //  {
        //    intf.CopyMethodName(first);
        //  }
        //}
      }
      resolvedannotations = resolvedList.AsReadOnly();
      return compilation;
    }
    private static ISymbol GetInterfaceSymbol(Project project, Compilation compilation, String interfacefullname, string interfaceshortname)
    {
      Contract.Assume(interfaceshortname.Length > 0);
      var isymbols = SymbolFinder.FindDeclarationsAsync(project, interfaceshortname, false).Result;
      //foreach (var isymbol in isymbols)
      //{
      //  RBLogger.Info(isymbol.GetDocumentationCommentId());
      //}
      return isymbols.Where(x => x.GetDocumentationCommentId().Equals(interfacefullname)).First();
    }

    private static bool TryFindContractsClass(Compilation compilation,  
                                      ISymbol interfacesymbol,
                                      out string contractclassname,
                                      out string filename)
    {
      #region CodeContracts
      Contract.Requires(compilation != null);
      Contract.Requires(interfacesymbol != null);
      Contract.Ensures(!Contract.Result<bool>() || Contract.ValueAtReturn<string>(out contractclassname) != null);
      Contract.Ensures(!Contract.Result<bool>() || Contract.ValueAtReturn<string>(out filename) != null);
      #endregion CodeContracts

      contractclassname = null;
      filename = null;
      var contractclass_t = compilation.GetTypeByMetadataName("System.Diagnostics.Contracts.ContractClassAttribute");
      var attributes = interfacesymbol.GetAttributes();
      var conattrs = attributes.Where(x => x.AttributeClass == contractclass_t);
      //hasContractsClass = conattrs.Any();
      if (conattrs.Any())
      {
        var conattr = conattrs.First();
        var ContractsClass = (INamedTypeSymbol) conattr.ConstructorArguments[0].Value;
        contractclassname = ContractsClass.GetDocumentationCommentId();
        filename = ContractsClass.Locations[0].SourceTree.FilePath;
        return true;
      }
      return false;
    }
    /// <summary>
    /// This method implements our naming convention for ContractsClassFor classes.
    /// i.e.: class Foo's contracts class is FooContracts
    /// </summary>
    /// <param name="interfacename"></param>
    /// <param name="interfacemethod"></param>
    /// <param name="contractclassname"></param>
    /// <returns></returns>
    private static string GetCreatedContractClassName(string interfacename, string interfacemethod) 
    {
      var interfaceShortName = GetUnqualifiedName(interfacename);
      var conclassname = String.Format("{0}Contracts", interfaceShortName);
      return interfacename.Replace(interfaceShortName, conclassname);
    }
    /// <summary>
    /// Get the name of the method inside the contracts class with the same signature as the interface method
    /// </summary>
    /// <param name="interfacename"></param>
    /// <param name="interfacemethod"></param>
    /// <param name="contractname"></param>
    /// <returns></returns>
    private static string GetMethodName(string interfacename, string interfacemethod, string contractname) 
    {
      var prefix = interfacename.Replace("T:", "M:");
      //var intersection = prefix.Intersect(interfacemethod).ToArray();
      //Console.WriteLine(intersection);
      var methodname = interfacemethod.Replace(prefix, "");
      return contractname.Replace("T:", "M:") + methodname;
    }
    internal static string GetUnqualifiedName(String orig)
    {
      var lastTick = orig.LastIndexOf('`');
      var lastdot = orig.LastIndexOf('.') + 1;
      if (lastTick == -1)
      {
        return orig.Replace(orig.Remove(lastdot), "");
      }
      else
      {
        var beforeTick = orig.Remove(lastTick);
        return GetUnqualifiedName(beforeTick);
      }
    }
    private static Compilation AddContractsClassAttributeToInterface(Project project, Compilation original, string filename, string interfacename, string parameterstring)
    {
      #region CodeContracts
      Contract.Requires(project != null);
      Contract.Requires(original != null);
      Contract.Requires(filename != null);
      Contract.Requires(interfacename != null);
      Contract.Ensures(Contract.Result<Compilation>() != null);
      #endregion CodeContracts

      var interfaceShortName = InterfaceAnnotationHelpers.GetUnqualifiedName(interfacename);
      var st = original.SyntaxTrees.Where(x => x.FilePath.Equals(filename, StringComparison.OrdinalIgnoreCase)).First();
      var doc = project.GetDocument(st);
      var sm = original.GetSemanticModel(st);
      var node = st.GetRoot().DescendantNodes().Where(x => x.CSharpKind() == SyntaxKind.InterfaceDeclaration
        && sm.GetDeclaredSymbol(x).GetDocumentationCommentId().Equals(interfacename)).First() as InterfaceDeclarationSyntax;
      Contract.Assert(node != null);
      var parameters = "";
      var numberparams = GetNumberParameters(parameterstring);
      if (numberparams > 0)
      {
        parameters = "<";
        var commas = new String(',', numberparams - 1);
        parameters += commas;
        parameters += ">";
      }
      var attr_name = SyntaxFactory.ParseName("ContractClass");
      var attr_args = SyntaxFactory.ParseAttributeArgumentList(String.Format("(typeof({0}Contracts{1}))", interfaceShortName, parameters));
      var attr = SyntaxFactory.Attribute(attr_name, attr_args);
      var attributes = SyntaxFactory.SeparatedList<AttributeSyntax>().Add(attr);
      var attr_list = SyntaxFactory.AttributeList(attributes);
      var newnode = node.AddAttributeLists(attr_list) as SyntaxNode;
      newnode = node.SyntaxTree.GetRoot().ReplaceNode(node, newnode);
      var newst = CSharpSyntaxTree.Create(newnode.SyntaxTree.GetRoot() as CSharpSyntaxNode, st.FilePath, null);
      return original.ReplaceSyntaxTree(st, newst);
    }
    private static Compilation ImplementContractsClass(Project project, Compilation original, string filename, string interfacename, string contractname)
    {
      #region CodeContracts
      Contract.Requires(project != null);
      Contract.Requires(original != null);
      Contract.Requires(filename != null);
      Contract.Requires(interfacename != null);
      Contract.Requires(contractname != null);
      Contract.Ensures(Contract.Result<Compilation>() != null);
      #endregion CodeContracts

      var interfaceShortName = InterfaceAnnotationHelpers.GetUnqualifiedName(interfacename);
      var doc = project.Documents.First(x => x.FilePath.Equals(filename, StringComparison.OrdinalIgnoreCase));
      var orig_st = original.SyntaxTrees.First(x => x.FilePath.Equals(filename, StringComparison.OrdinalIgnoreCase));
      doc = doc.WithSyntaxRoot(orig_st.GetRoot());
      var st = doc.GetSyntaxTreeAsync().Result;
      Contract.Assert(st != null);
      //var st = CSharpSyntaxTree.Create(doc.GetSyntaxRootAsync().Result as CSharpSyntaxNode, orig_st.FilePath, null);
      var newcomp = original.ReplaceSyntaxTree(orig_st, st);
      var sm = newcomp.GetSemanticModel(st);
      //Console.WriteLine(doc.GetTextAsync().Result);
      var classes = st.GetRoot().DescendantNodesAndSelf().Where(x => x.CSharpKind().Equals(SyntaxKind.ClassDeclaration));
      var node = classes.First(x => sm.GetDeclaredSymbol(x).GetDocumentationCommentId().Equals(contractname));
      //var node = st.GetRoot().DescendantNodesAndSelf().First(x => x.CSharpKind().Equals(SyntaxKind.ClassDeclaration)
      //                                                        && sm.GetDeclaredSymbol(x).GetDocumentationCommentId().Equals(contractname));
      var baselist = node.DescendantNodes().First(x => x.CSharpKind().Equals(SyntaxKind.BaseList));
      var inode = baselist.DescendantTokens().First(x => x.Text.Equals(interfaceShortName));
      //var assembly = Assembly.LoadFrom(@"C:\cci\Microsoft.Research\Imported\Tools\Roslyn\v4.5.1\Microsoft.CodeAnalysis.CSharp.Features.dll");
      //var assembly = Assembly.LoadFrom(@"C:\Users\t-scottc\Desktop\Signed_20140201.1\Microsoft.CodeAnalysis.CSharp.Features.dll");
      //var assembly = Assembly.LoadFrom(@"C:\cci\Microsoft.Research\CCTools\ReviewBot\bin\Debug\Microsoft.CodeAnalysis.CSharp.Features.dll");
      Assembly assembly;

      if (UsingHelpers.TryGetMicrosoftCodeAnalysisCSharpFeatures(out assembly))
      {
        try
        {
          var type = assembly.GetType("Microsoft.CodeAnalysis.CSharp.ImplementInterface.CSharpImplementInterfaceService");
          var method = type.GetMethod("ImplementInterfaceAsync");
          var service = Activator.CreateInstance(type);
          var result = method.Invoke(service, new object[] { doc, inode.Parent, CancellationToken.None }) as Task<Document>;
          var newdoc = result.Result;

          return newcomp.ReplaceSyntaxTree(st, newdoc.GetSyntaxTreeAsync().Result);
        }
        catch(Exception e)
        {
          Output.WriteWarning("Something went wrong while trying to invoke Roslyn refactoring engine. Details: {0}", e.Message);
          Output.WriteLine("We continue skipping this interface");
          return original;
        }
      }
      else
      {
        Output.WriteWarning("Can't add the interface contract to {0} as we failed loading the Roslyn refactoring engine", interfacename);
        return original;
      }
    }
    private static Compilation MakeContractsClassFor(Project project, Compilation original, string interfacename, string filename, string parameters)
    {
      #region CodeContracts
      Contract.Requires(project != null);
      Contract.Requires(original != null);
      Contract.Requires(interfacename != null);
      Contract.Requires(filename != null);
      Contract.Ensures(Contract.Result<Compilation>() != null);
      #endregion CodeContracts

      var st = original.SyntaxTrees.Where(x => x.FilePath.Equals(filename, StringComparison.OrdinalIgnoreCase)).First();
      var doc = project.GetDocument(st);
      var sm = original.GetSemanticModel(st);
      var node = st.GetRoot().DescendantNodes().Where(x => x.CSharpKind() == SyntaxKind.InterfaceDeclaration
        && sm.GetDeclaredSymbol(x).GetDocumentationCommentId().Equals(interfacename)).First() as InterfaceDeclarationSyntax;
      var parent = node.Parent;
      var newclass = MakeContractsClassForNode(interfacename, parameters);
      var mem = newclass as MemberDeclarationSyntax;
      SyntaxNode newnode = null;
      if (parent.CSharpKind() == SyntaxKind.ClassDeclaration) 
      {
        var classdecl = parent as ClassDeclarationSyntax;
        var newdecl = classdecl.AddMembers(mem);
        newnode = parent.Parent.ReplaceNode(parent, newdecl); 
      }
      if (parent.CSharpKind() == SyntaxKind.NamespaceDeclaration)
      {
        var namedecl = parent as NamespaceDeclarationSyntax;
        var newdecl = namedecl.AddMembers(mem);
        newnode = parent.Parent.ReplaceNode(parent, newdecl); 
      }
      var newst = CSharpSyntaxTree.Create(newnode.SyntaxTree.GetRoot() as CSharpSyntaxNode, st.FilePath, null);
      return original.ReplaceSyntaxTree(st, newst);
    }
    private static SyntaxNode GetContractsClassNode(Compilation compilation, string filename, string contractname)
    {
      var st = compilation.SyntaxTrees.First(x => x.FilePath.Equals(filename, StringComparison.OrdinalIgnoreCase));
      var sm = compilation.GetSemanticModel(st);
      return st.GetRoot().DescendantNodesAndSelf().First(x => x.CSharpKind().Equals(SyntaxKind.ClassDeclaration)
                                                              && sm.GetDeclaredSymbol(x).GetDocumentationCommentId().Equals(contractname));
    }
    private static SyntaxNode MakeContractsClassForNode(string interfacename, string parameters)
    {
      string intRawName = InterfaceAnnotationHelpers.GetUnqualifiedName(interfacename);
      string intName = InterfaceAnnotationHelpers.GetUnqualifiedName(interfacename);
      var numberparams = GetNumberParameters(parameters);
      //string contractParams = "";
      //var IsInterfaceParameterized = interfacename.Contains('`');
      //if (IsInterfaceParameterized)
      if (numberparams > 0)
      {
        //var tickIndex = interfacename.LastIndexOf('`') + 1;
        //var afterTick = interfacename.Replace(interfacename.Remove(tickIndex), "");
        //var numberParameters = int.Parse(afterTick);
        intName += '<';
        for (int i = 1; i < numberparams; i++)
        {
          intName += ',';
        }
        intName += '>';

        //contractParams += "<T";
        //for (int i = 1; i < numberParameters; i++)
        //{
        //contractParams += ",T" + i;
        //}
        //contractParams += '>';
      }
        var text = String.Format(@"
using System; 
[ContractClassFor(typeof({0}))]
internal abstract class {1}Contracts{2} : {1}{2} 
{{
}}"
          , intName, intRawName, parameters);
      var parsed = SyntaxFactory.ParseCompilationUnit(text);
      return parsed.SyntaxTree.GetRoot().DescendantNodesAndSelf().OfType<ClassDeclarationSyntax>().First();
    }
    private static int GetNumberParameters(string parameters)
    {
      if (string.IsNullOrEmpty(parameters))
      {
        return 0;
      }
      else
      {
        return parameters.Count(c => c == ',') + 1;
      }
    }
  }
}