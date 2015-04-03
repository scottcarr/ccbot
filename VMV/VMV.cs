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
using System.Linq;
using Microsoft.Research.ReviewBot;

namespace Microsoft.Research.ReviewBot.Utils
{
  class VMV
  {
    const string baselineBranch = "baseline";
    const string baselineAnnotatedBranch = "baselineAnnotated";
    //const string masterBranch = "newer";
    const string masterBranch = "newerMerged";
    const string masterAnnotatedBranch = "newerMergedAnnotated";
    const string baselineName = "fluentvalidatiobaseLine2";
    public static void DoVMV(Func<string, int> CountSuggestions)
    {
      Contract.Requires(CountSuggestions != null);

      Configuration config;
      if (!Configuration.TryOpenConfig(Constants.String.ConfigDefault, out config))
      {
        Console.WriteLine("Failed to open default config");
        return;
      }

      var baseXML = config.CccheckXml + "baseline.xml";
      if (!File.Exists(baseXML))
      {
        Git.RevertToBranch(config.GitRoot, config.Git, baselineBranch);
        if (!ExternalCommands.TryBuildSolution(config.Solution, config.MSBuild)) { throw new Exception("Couldn't build solution"); }
        ExternalCommands.TryRunClousot(baseXML, config.Cccheck, config.CccheckOptions, config.RSP);
      }

      //CheckoutBranch(config.GitRoot, config.Git, baselineAnnotatedBranch);
      //string[] reviewArgs = {
      //                        baseXML, 
      //                        "-project", config.Project, 
      //                        "-solution", config.Solution, 
      //                        "-output", "git", "-gitroot", config.GitRoot
      //                      };
      //if (Reviewer.Review(reviewArgs.ToArray()) != 0) 
      //{
      //  throw new Exception("ReviewBot failed.");
      //}

      var baseAnnotatedXML = config.CccheckXml + "baselineAnnotated.xml";
      if (!File.Exists(baseAnnotatedXML))
      {
        Git.CheckoutBranch(config.GitRoot, config.Git, baselineAnnotatedBranch);
        if (!ExternalCommands.TryBuildSolution(config.Solution, config.MSBuild)) { throw new Exception("Couldn't build solution"); }
        var newOptions = config.CccheckOptions + " -saveSemanticbaseline " + baselineName;
        ExternalCommands.TryRunClousot(baseAnnotatedXML, config.Cccheck, newOptions, config.RSP);
      }
      //Utils.RunGit(config.GitRoot, "add -u .", config.Git);
      //Utils.RunGit(config.GitRoot, "commit -m \"add annotations\"", config.Git);

      var masterXML = config.CccheckXml + "master.xml";
      if (!File.Exists(masterXML))
      {
        //RevertToBranch(config.GitRoot, config.Git, masterBranch);
        Git.CheckoutBranch(config.GitRoot, config.Git, masterBranch);
        //Utils.RunGit(config.GitRoot, "merge " + baselineAnnotatedBranch, config.Git);
        if (!ExternalCommands.TryBuildSolution(config.Solution, config.MSBuild)) { throw new Exception("Couldn't build solution"); }
        var newOptions = config.CccheckOptions + " -useSemanticbaseline " + baselineName;
        ExternalCommands.TryRunClousot(masterXML, config.Cccheck, newOptions, config.RSP);
      }

      //CheckoutBranch(config.GitRoot, config.Git, masterAnnotatedBranch);
      //if (!Utils.TryBuildSolution(config.Solution, config.MSBuild)) { throw new Exception("Couldn't build solution"); }
      //string[] reviewArgs = {
      //                        masterXML, 
      //                        "-project", config.Project, 
      //                        "-solution", config.Solution, 
      //                        "-output", "git", "-gitroot", config.GitRoot
      //                      };
      //if (Reviewer.Review(reviewArgs.ToArray()) != 0) 
      //{
      //  throw new Exception("ReviewBot failed.");
      //}
      //if (!Utils.TryBuildSolution(config.Solution, config.MSBuild)) { throw new Exception("Couldn't build solution"); }
      //Utils.RunGit(config.GitRoot, "add -u .", config.Git);
      //Utils.RunGit(config.GitRoot, "commit -m \"add annotations\"", config.Git);

      var masterAnnotatedXML = config.CccheckXml + "masterAnnotated.xml";
      if (!File.Exists(masterAnnotatedXML))
      {
        Git.CheckoutBranch(config.GitRoot, config.Git, masterAnnotatedBranch);
        if (!ExternalCommands.TryBuildSolution(config.Solution, config.MSBuild)) { throw new Exception("Couldn't build solution"); }
        var newOptions = config.CccheckOptions + " -useSemanticbaseline " + baselineName;
        ExternalCommands.TryRunClousot(masterAnnotatedXML, config.Cccheck, newOptions, config.RSP);
      }

      var masterAnnotatedXMLFinal = config.CccheckXml + "masterAnnotatedFinal.xml";
      if (!File.Exists(masterAnnotatedXMLFinal))
      {
        Git.CheckoutBranch(config.GitRoot, config.Git, masterAnnotatedBranch);
        if (!ExternalCommands.TryBuildSolution(config.Solution, config.MSBuild)) { throw new Exception("Couldn't build solution"); }
        var newOptions = config.CccheckOptions + " -useSemanticbaseline " + baselineName;
        ExternalCommands.TryRunClousot(masterAnnotatedXMLFinal, config.Cccheck, newOptions, config.RSP);
      }

      var masterWithoutBaseline = config.CccheckXml + "masterWithoutBaseline.xml";
      if (!File.Exists(masterWithoutBaseline))
      {
        Git.CheckoutBranch(config.GitRoot, config.Git, "master");
        if (!ExternalCommands.TryBuildSolution(config.Solution, config.MSBuild)) { throw new Exception("Couldn't build solution"); }
        ExternalCommands.TryRunClousot(masterWithoutBaseline, config.Cccheck, config.CccheckOptions, config.RSP);
      }

      var mergedWithoutBaseline = config.CccheckXml + "mergedWithoutBaseline.xml";
      if (!File.Exists(mergedWithoutBaseline))
      {
        Git.CheckoutBranch(config.GitRoot, config.Git, masterBranch);
        if (!ExternalCommands.TryBuildSolution(config.Solution, config.MSBuild)) { throw new Exception("Couldn't build solution"); }
        ExternalCommands.TryRunClousot(mergedWithoutBaseline, config.Cccheck, config.CccheckOptions, config.RSP);
      }

      //Console.WriteLine("Baseline warnings {0}", ReviewBotStaticAnalysisProvider.GetChecks(baseXML).Count());
      //Console.WriteLine("Baseline Annotated warnings {0}", ReviewBotStaticAnalysisProvider.GetChecks(baseAnnotatedXML).Count());
      Console.WriteLine("master warnings {0}", HelpersForClousotXML.GetChecks(masterWithoutBaseline).Count());
      //Console.WriteLine("master warnings with baseline {0}", ReviewBotStaticAnalysisProvider.GetChecks(masterXML).Count());
      //Console.WriteLine("master warnings with baseline {0}", ReviewBotStaticAnalysisProvider.GetChecks(masterXML).Count());
      Console.WriteLine("master merged warnings {0}", HelpersForClousotXML.GetChecks(mergedWithoutBaseline).Count());
      //Console.WriteLine("master merged with baseline warnings {0}", ReviewBotStaticAnalysisProvider.GetChecks(masterXML).Count());
      Console.WriteLine("master merged annotated warnings with baseline {0}", HelpersForClousotXML.GetChecks(masterAnnotatedXML).Count());
      Console.WriteLine("master merged annotated with baseline final warnings {0}", HelpersForClousotXML.GetChecks(masterAnnotatedXMLFinal).Count());

      Git.CheckoutBranch(config.GitRoot, config.Git, baselineAnnotatedBranch);
      Console.WriteLine("baseline annotated suggestions {0}", CountSuggestions(config.GitRoot));
      Git.CheckoutBranch(config.GitRoot, config.Git, masterBranch);
      Console.WriteLine("master merged suggestions {0}", CountSuggestions(config.GitRoot));
      Git.CheckoutBranch(config.GitRoot, config.Git, masterAnnotatedBranch);
      Console.WriteLine("master annotated suggestions {0}", CountSuggestions(config.GitRoot));
      Console.ReadKey();

    }
  }
}
