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
using System.Diagnostics.Contracts;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Research.ReviewBot.Logging;

namespace Microsoft.Research.ReviewBot.Configuration
{
  /// <summary>
  /// A blob-of-data class holding the configuration options for a ReviewBot run
  /// </summary>
  [Serializable]
  public class Configuration : Options
  {
    //public string Project;
    //public string Solution;
    //public string Zip;
    public string Git;
    //public string GitRoot;
    public string GitBaseBranch;
    public string Cccheck;
    public string CccheckXml;
    public string CccheckOptions;
    public string MSBuild;
    public string RSP;
    public string CodeFlowProject;
    public Configuration() { }

    public static bool TryOpenConfig(string path, out Configuration config)
    {
      Contract.Requires(!string.IsNullOrEmpty(path));
      Contract.Ensures(!Contract.Result<bool>() || Contract.ValueAtReturn(out config) != null);

      Output.WritePhase("Opening the config file {0}", path);

      try
      {
        var xs = new XmlSerializer(typeof(Configuration));
        var file = File.OpenRead(path);
        config = xs.Deserialize(file) as Configuration;
        Contract.Assume(config != null, "Make sure deserialization succeeded");
        file.Close();
      }
      catch (Exception)
      {
        Output.WriteError("Error while opening the config file. Aborting");
        config = default(Configuration);

        return false;
      }
      return true;
    }

    public static Configuration GetDefaultConfiguration()
    {
      const string ClousotPath = @"c:\Program Files (x86)\Microsoft\Contracts\Bin\cccheck.exe";
      const string ClousotXMLPath = @"\\msr\public\logozzo\ReviewBot\test.xml";
      const string projectPath = @"E:\Repro\codeflow-TestReviewBot\cf\main\src\Client\ClientExtensibility\Client.Extensibility.csproj";
      const string solutionPath = @"C:\Users\t-scottc\workspace\ReviewBotRepos\fluentvalidation\FluentValidation.sln";
      const string gitRoot = @"C:\Users\t-scottc\workspace\ReviewBotRepos\fluentvalidation";
      const string RSPPath = @"C:\Users\t-scottc\workspace\ReviewBotRepos\fluentvalidation\src\FluentValidation\obj\Debug\Decl\FluentValidationcccheck.rsp";
      const string ClousotOptions = "@" + RSPPath + " -xml -remote=false -suggest methodensures -suggest propertyensures -suggest objectinvariants -suggest necessaryensures  -suggest readonlyfields -suggest assumes -suggest nonnullreturn -sortWarns=false -warninglevel full -maxwarnings 99999999";
      const string CodeFlowProjectName = "ReviewBotTesting";
      const string gitExePath = @"C:\Program Files (x86)\Git\cmd\git.exe";
      const string MSBuildPath = "C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\MSBuild.exe";
      const string GitBaseBranch = "master";

      return new Configuration()
      {
        Cccheck = ClousotPath,
        CccheckOptions = ClousotOptions,
        CccheckXml = ClousotXMLPath,
        CodeFlowProject = CodeFlowProjectName,
        Git = gitExePath,
        GitBaseBranch = GitBaseBranch,
        GitRoot = gitRoot,
        MSBuild = MSBuildPath,
        Project = projectPath,
        Solution = solutionPath,
        RSP = RSPPath
      };
    }
  }
}
