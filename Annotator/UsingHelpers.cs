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
using System.Reflection;
using System.Threading;
using System.IO;
using System.Diagnostics.Contracts;
using Microsoft.Research.ReviewBot.Utils;

namespace Microsoft.Research.ReviewBot
{
    internal static class UsingHelpers
    {
        internal static Compilation CleanImports(Project project, Compilation compilation)
        {
            #region CodeContracts
            Contract.Requires(project != null);
            Contract.Requires(compilation != null);
            Contract.Ensures(Contract.Result<Compilation>() != null);
            #endregion CodeContracts

            var newCompilation = compilation;
            foreach (var st in compilation.SyntaxTrees)
            {
                var doc = project.Documents.First(x => x.FilePath == st.FilePath);
                Contract.Assert(doc != null);
                doc = doc.WithSyntaxRoot(st.GetRoot()); // I am not updating the project as I go
                doc = RemoveUnnecessaryUsings(doc, newCompilation);
                var newst = SyntaxFactory.SyntaxTree(doc.GetSyntaxRootAsync().Result, doc.FilePath);
                newCompilation = newCompilation.ReplaceSyntaxTree(st, newst);
            }
            return newCompilation;
        }
        private static Document RemoveUnnecessaryUsings(Document doc, Compilation compilation)
        {
            #region CodeContracts
            Contract.Requires(doc != null);
            Contract.Requires(compilation != null);
            Contract.Ensures(Contract.Result<Document>() != null);
            #endregion CodeContracts

            var st = doc.GetSyntaxTreeAsync().Result;
            var sm = doc.GetSemanticModelAsync().Result;
            //var assembly = Assembly.LoadFrom(@"C:\cci\Microsoft.Research\Imported\Tools\Roslyn\v4.5.1\Microsoft.CodeAnalysis.CSharp.Features.dll");
            //var assembly = Assembly.LoadFrom(@"C:\cci\Microsoft.Research\CCTools\ReviewBot\bin\Debug\Microsoft.CodeAnalysis.CSharp.Features.dll");
            //var assembly = Assembly.LoadFrom(@"..\..\..\packages\Microsoft.CodeAnalysis.Features.0.7.4040207-beta\lib\net45\Microsoft.CodeAnalysis.CSharp.Features.dll");
            //var assembly = Assembly.LoadFrom(@"C:\Users\t-scottc\Desktop\Signed_20140201.1\Microsoft.CodeAnalysis.CSharp.Features.dll");
            //var assembly = Assembly.LoadFrom(@"C:\Users\t-scottc\workspace\roslyn\Binaries\Debug\Microsoft.CodeAnalysis.CSharp.Workspaces.dll");
            Assembly assembly;

            if (TryGetMicrosoftCodeAnalysisCSharpFeatures(out assembly))
            {
                var type = assembly.GetType("Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryImports.CSharpRemoveUnnecessaryImportsService");
                var method = type.GetMethod("RemoveUnnecessaryImports");
                var service = Activator.CreateInstance(type);
                return method.Invoke(service, new object[] { doc, sm, st.GetRoot(), CancellationToken.None }) as Document;
            }
            else
            {
                //Output.WriteWarning("Can't run the refactoring to remove using");
                var uv = new UsingVisitor(sm.Compilation);
                var root = st.GetRoot();
                uv.Visit(root);
                var newnode = root.RemoveNodes(uv.duplicates, SyntaxRemoveOptions.KeepNoTrivia);
                var newdoc = doc.WithSyntaxRoot(newnode);
                return doc;
            }
        }
        // Currently, I'm not alphabetizing the usings but this could be added
        //private static Document OrganizeUsings(Document doc)
        //{
        //  var assembly = Assembly.LoadFrom("Microsoft.CodeAnalysis.CSharp.Features.dll");
        //  var type = assembly.GetType("Microsoft.CodeAnalysis.CSharp.OrganizeImports.CSharpOrganizeImportsService");
        //  var method = type.GetMethod("OrganizeImportsAsync");
        //  var service = Activator.CreateInstance(type);
        //  var result = method.Invoke(service, new object[] { doc, false, CancellationToken.None }) as Task<Document>;
        //  return result.Result;
        //}
        public static bool TryGetMicrosoftCodeAnalysisCSharpFeatures(out Assembly assembly)
        {
            Contract.Ensures(!Contract.Result<bool>() || Contract.ValueAtReturn(out assembly) != null);

            // DONT REMOVE THE REFERENCES TO Microsoft.CodeAnalysis.Features.dll and Microsoft.CodeAnalysis.Features.CSharp.dll
            // from ReviewBot.csproj or this code will break!!!

            var assemblyPath = GetPath(Assembly.GetExecutingAssembly());
            if (assemblyPath == null)
            {
                assemblyPath = GetPath(Assembly.GetAssembly(typeof(UsingHelpers)));
            }

            if (assemblyPath != null)
            {
                assembly = Assembly.LoadFrom(assemblyPath);
                return true;
            }
            else
            {
                assembly = null;
                return false;
            }
        }

        static private string GetPath(Assembly assembly)
        {
            var dir = Path.GetDirectoryName(assembly.Location);
            var assemblyPath = Path.Combine(dir, @"Microsoft.CodeAnalysis.CSharp.Features.dll");

            if (!File.Exists(assemblyPath))
            {
                Output.WriteWarning("Can't find Microsoft.CodeAnalysis.CSharp.Features.dll in {0}", dir);
                return null;
            }
            else
            {
                return assemblyPath;
            }
        }
        class UsingVisitor : CSharpSyntaxRewriter
        {
            SemanticModel sm;
            Compilation cmp;
            readonly List<SymbolInfo> directives;
            public readonly List<UsingDirectiveSyntax> duplicates;
            public UsingVisitor(Compilation compilation) : base()
            {
                cmp = compilation;
                directives = new List<SymbolInfo>();
                duplicates = new List<UsingDirectiveSyntax>();
            }
            public override SyntaxNode Visit(SyntaxNode node)
            {
                if (node != null)
                {
                    sm = cmp.GetSemanticModel(node.SyntaxTree);
                }
                return base.Visit(node);
            }
            public override SyntaxNode VisitUsingDirective(UsingDirectiveSyntax node)
            {
                var name = node.Name;
                var symInfo = sm.GetSymbolInfo(name);
                //Console.WriteLine(symInfo.Symbol.Name);
                if (directives.Contains(symInfo))
                {
                    //Console.WriteLine("Duplicate using");
                    duplicates.Add(node);
                }
                directives.Add(symInfo);
                return base.VisitUsingDirective(node);
            }
        }
    }
}
