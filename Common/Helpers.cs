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
using System.Diagnostics.Contracts;
using System.IO;
using System.Xml;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.ReviewBot
{
  public static class Helpers
  {
    public static class IO
    {
      public static void CreateDirAndWriteAllText(string path, string text)
      {
        Contract.Requires(!string.IsNullOrEmpty(path));
        Contract.Requires(text != null);

        if (!File.Exists(path))
        {
          var dirName = Path.GetDirectoryName(path);
          if (!Directory.Exists(dirName))
          {
            Directory.CreateDirectory(dirName);
          }
        }

        File.WriteAllText(path, text);
      }
      public static void DumpBuildOutput(string path, string text)
      {
        Contract.Requires(!string.IsNullOrEmpty(path));
        Contract.Requires(text != null);

        Output.WriteLine("Dumping the output of the build in {0}", path);
        CreateDirAndWriteAllText(path, text);
      }      
    }

    public static int CountSuggestions(string rootdir)
    {
      Contract.Requires(rootdir != null);

      var files = Directory.EnumerateFiles(rootdir, "*.cs", SearchOption.AllDirectories);
      var lines = files.SelectMany(file => File.ReadLines(file));
      return lines.Where(line => line.Contains(Constants.String.Signature.Trim())).Count();
    }

    public static class IEnumerable
    {
      public static IEnumerable<T> EmptyEnumeration<T>()
      {
        Contract.Ensures(Contract.Result<IEnumerable<T>>() != null);

        return new T[0];
      }
    }
    public static string EnableCodeContractsInProject(string csprojPath)
    {
      var cc_props_text = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
   <!-- Enable CC only in debug builds -->
  <PropertyGroup Condition=""'$(Configuration)' == 'Debug'"">
    <CodeContractsReferenceAssembly>Build</CodeContractsReferenceAssembly>
    <CodeContractsEnableRuntimeChecking>True</CodeContractsEnableRuntimeChecking>
    <CodeContractsRunCodeAnalysis>True</CodeContractsRunCodeAnalysis>
    <CodeContractsRunInBackground>True</CodeContractsRunInBackground>
	<CodeContractsShowSquigglies>True</CodeContractsShowSquigglies>
    <CodeContractsEnableCheckedExceptionChecking>False</CodeContractsEnableCheckedExceptionChecking>
    <CodeContractsFailBuildOnWarnings>False</CodeContractsFailBuildOnWarnings>
  </PropertyGroup>

  <PropertyGroup>
	<!-- Runtime checking -->
	<CodeContractsAssemblyMode>0</CodeContractsAssemblyMode>
	<CodeContractsRuntimeOnlyPublicSurface>False</CodeContractsRuntimeOnlyPublicSurface>
    <CodeContractsRuntimeThrowOnFailure>True</CodeContractsRuntimeThrowOnFailure>
    <CodeContractsRuntimeCallSiteRequires>False</CodeContractsRuntimeCallSiteRequires>
    <CodeContractsRuntimeSkipQuantifiers>False</CodeContractsRuntimeSkipQuantifiers>
    <CodeContractsCustomRewriterAssembly />
    <CodeContractsCustomRewriterClass />
    <CodeContractsExtraRewriteOptions />
    <CodeContractsLibPaths />
    <CodeContractsReferenceAssembly>Build</CodeContractsReferenceAssembly>
       
	<!-- Static checking -->
	
	<!-- Proof obligations -->
    <CodeContractsNonNullObligations>True</CodeContractsNonNullObligations>
    <CodeContractsBoundsObligations>True</CodeContractsBoundsObligations>
    <CodeContractsArithmeticObligations>True</CodeContractsArithmeticObligations>
    <CodeContractsEnumObligations>True</CodeContractsEnumObligations>

	<!-- Code Quality -->
    <CodeContractsRedundantAssumptions>True</CodeContractsRedundantAssumptions>
    <CodeContractsAssertsToContractsCheckBox>True</CodeContractsAssertsToContractsCheckBox>
    <CodeContractsRedundantTests>True</CodeContractsRedundantTests>
    <CodeContractsMissingPublicRequiresAsWarnings>True</CodeContractsMissingPublicRequiresAsWarnings>
    <CodeContractsMissingPublicEnsuresAsWarnings>False</CodeContractsMissingPublicEnsuresAsWarnings>

	<!-- Inference -->
    <CodeContractsInferRequires>True</CodeContractsInferRequires>
    <CodeContractsInferEnsures>True</CodeContractsInferEnsures>
    <CodeContractsInferObjectInvariants>True</CodeContractsInferObjectInvariants>
   
	<!-- Suggestions -->
	<CodeContractsSuggestAssumptions>False</CodeContractsSuggestAssumptions>
    <CodeContractsSuggestAssumptionsForCallees>False</CodeContractsSuggestAssumptionsForCallees>
    <CodeContractsSuggestRequires>False</CodeContractsSuggestRequires>
    <CodeContractsNecessaryEnsures>False</CodeContractsNecessaryEnsures>
    <CodeContractsSuggestObjectInvariants>False</CodeContractsSuggestObjectInvariants>
    <CodeContractsSuggestReadonly>True</CodeContractsSuggestReadonly>
    
	<!-- Baseline -->
    <CodeContractsBaseLineFile />
    <CodeContractsUseBaseLine>False</CodeContractsUseBaseLine>
    <CodeContractsEmitXMLDocs>False</CodeContractsEmitXMLDocs>


	<!-- Cache -->
    <CodeContractsSQLServerOption>cloudotserver</CodeContractsSQLServerOption>
    <CodeContractsCacheAnalysisResults>True</CodeContractsCacheAnalysisResults>
    <CodeContractsSkipAnalysisIfCannotConnectToCache>False</CodeContractsSkipAnalysisIfCannotConnectToCache>

	<!-- Customize -->
    <CodeContractsExtraAnalysisOptions>-maxpathsize=120 -warnIfSuggest  readonlyfields -warnIfSuggest asserttocontracts</CodeContractsExtraAnalysisOptions>
    <CodeContractsBeingOptimisticOnExternal>True</CodeContractsBeingOptimisticOnExternal>
    <CodeContractsAnalysisWarningLevel>1</CodeContractsAnalysisWarningLevel>

	</PropertyGroup>
</Project>
";
      var ccFile = Path.Combine(Path.GetDirectoryName(csprojPath), "Common.CodeContracts.props");
      File.WriteAllText(ccFile, cc_props_text);
      var oldDoc = new XmlDocument();
      oldDoc.LoadXml(File.ReadAllText(csprojPath));
      var children = oldDoc.ChildNodes;
      foreach (var child in children)
      {
        var node = child as XmlElement;
        if (node != null)
        {
          var newnode = oldDoc.CreateElement("Import", oldDoc.DocumentElement.NamespaceURI);
          newnode.SetAttribute("Project", "$(ProjectDir)\\Common.CodeContracts.props");
          node.AppendChild(newnode);

        }
      }
      oldDoc.Save(csprojPath);
      return ccFile;
    }
  }
}