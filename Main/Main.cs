/*
  ReviewBot 0.1
  Copyright (c) Microsoft Corporation
  All rights reserved. 
  
  MIT License
  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the ""Software""), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
  The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
  THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#undef ENABLE_CODE_FLOW_INTEGRATION
#undef FORCE_BUILD_TO_TEST_CHANGES


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Xml.Serialization;
using Microsoft.Research.ReviewBot.Utils;



namespace Microsoft.Research.ReviewBot
{
  public static class ReviewBotMain
  {

    static bool GenerateCodeFlowReview =
#if ENABLE_CODE_FLOW_INTEGRATION
 true;
#else
 false;
#endif

    #region Statistics

    readonly static Statistics statistics = new Statistics();

    #endregion

    public static int Main(string[] args)
    {
      // Step 1: Parse the command line
      Output.ToolAction action;
      string configFileName;
      if (!ParseCommandLine.TryParseCommandLine(args, out action, out configFileName) || action == Output.ToolAction.PrintUsage)
      {
        ParseCommandLine.PrintUsage();
        return action == Output.ToolAction.PrintUsage ? 0 : 1;
      }

      // Step 2: Perform the action
      var config = default(Configuration);
      switch (action)
      {
        case Output.ToolAction.Run:
          {
            if (!Configuration.TryOpenConfig(configFileName, out config))
            {
              Output.WriteError("Failed to open default config");
              return -1;
            }
            else
            {
              // We continue with the execution of Main
            }
          }
          break;

        case Output.ToolAction.CreateDefaultFile:
          {
            var path = Constants.String.PathForDefaultFile;
            Output.WriteLine("Creating a default config file {0}", path);
            WriteDefaultConfig(path);

            return 0;
          }

        default:
          {
            Contract.Assert(false, "Should be unreachable");
            return 1;
          }
      }

      Contract.Assert(action == Output.ToolAction.Run, "If we reach this point, it's because we want to run the tool");

#if ENABLE_CODE_FLOW_INTEGRATION
      string errorMsg;
      if (!CodeFlowIntegration.TryPackageExtension(config.CodeFlowProject, config.MSBuild, out errorMsg))
      {
        Output.WriteWarning("Couldn't create package extension for CodeFlow. We will skip generating a review");
#if DEBUG
        System.Diagnostics.Debugger.Launch();
#endif
        GenerateCodeFlowReview = false; // We had an error, therefore we will skip generating a review
      }
#endif

      // Step 3: Let's revert to the base branch
      Git.RevertToOriginal(config.GitRoot, config.GitBaseBranch, config.Git);

      // Step 4: Let's run the build/analyze/instrument to a fixpoint
      BuildAnalyzeInstrument(config);

#if ENABLE_CODE_FLOW_INTEGRATION
      // Step 5: Create and Submit the review
      if (GenerateCodeFlowReview)
      {
        CodeFlowIntegration.SubmitReview(config.GitRoot, config.Git, true, false);
      }
#endif

      // Step 6: Print the statistics
      statistics.PrintStatistics();

      // Step 7: we are done
      Output.WriteLine(Constants.String.ToolName + " Done!");
      if (System.Diagnostics.Debugger.IsAttached)
      {
        Output.WriteLine("Press any key...");
        Console.ReadKey();
      }
      return 0;
    }

    public static void BuildAnalyzeInstrument(Configuration config)
    {

      // this was for a workaround for a Roslyn bug
      //FixProjectForRoslyn(projectPath);

      Output.WriteLine("Iterate the Build/Analyze/Instrument {0} time(s)", Constants.Numerical.NumberOfIterations.ToString());

      for (var i = 0; i < Constants.Numerical.NumberOfIterations; i++)
      {
        BuildAnalyzeInstrumentInternalLoop(config, i);
      }

      // this was for a workaround for a Roslyn bug
      //FixProjectForMSBuild(projectPath);

#if FORCE_BUILD_TO_TEST_CHANGES 
      if (!ExternalCommands.TryBuildSolution(config.Solution, config.MSBuild)) 
      {
        Output.WriteErrorAndQuit("Couldn't build solution after ReviewBot changes");
      }
#endif

      Output.WriteLine("Done with the iterations");

      Output.WritePhase("Run clousot one last time to collect the comments");
      var commentsFile = Path.Combine(Constants.String.PathForCommentsDirectory, "comments.xml");

      if (!ExternalCommands.TryRunClousot(commentsFile, config.Cccheck, config.CccheckOptions, config.RSP))
      {
        Output.WriteErrorAndQuit("Got an error running Clousot");
      }

      var alarms = HelpersForClousotXML.GetChecks(commentsFile).Count();

      Output.WriteLine("Number of alarms {0}", alarms.ToString());

      statistics.NumberOfWarnings.Add(alarms);
    }

    private static void BuildAnalyzeInstrumentInternalLoop(Configuration config, int i)
    {
      Output.WriteLine("Iteration {0} of Build/Analyze/Instrument", i.ToString());

      var watch = new Stopwatch();

      // We generate a new xml file for each iteration
      var reviewArgs = new string[] {
                                string.Format("{0}.{1}.{2}", config.CccheckXml, i, "xml"), 
                                "-project", config.Project, 
                                "-solution", config.Solution, 
                                "-output", "git", "-gitroot", config.GitRoot
                              };

#if FORCE_BUILD_TO_TEST_CHANGES 
        if (!ExternalCommands.TryBuildSolution(config.Solution, config.MSBuild)) 
        {
          Output.WriteErrorAndQuit("Can't build the solution ...");
        }
#endif
      watch.Start();

      var clousotXMLOutput = reviewArgs[0];

      if (!File.Exists(clousotXMLOutput))
      {
        Output.WriteLine("The CC static checker output file does not exits. Let's build the solution and run the checker");

        if (!ExternalCommands.TryBuildSolution(config.Solution, config.MSBuild))
        {
          Output.WriteErrorAndQuit("Couldn't build the solution");
        }

        if (!ExternalCommands.TryRunClousot(clousotXMLOutput, config.Cccheck, config.CccheckOptions, config.RSP))
        {
          Output.WriteErrorAndQuit("Cant' run Clousot");
        }
      }
      else
      {
        Output.WriteLine("The xml file {0} already exists. We skip running the static checker again", clousotXMLOutput);
      }

      // Count the number of warning in this iteration
      var nWarnings = HelpersForClousotXML.GetChecks(clousotXMLOutput).Count();

      Output.WriteLine("Clousot reports {0} alarms", nWarnings.ToString());

      statistics.NumberOfWarnings.Add(nWarnings);

      Output.WritePhase("Instrumenting the source code");
      if (Annotator.DoAnnotate(reviewArgs) != 0)
      {
        Output.WriteErrorAndQuit("Contract instrumentation failed");
      }

      watch.Stop();

      Output.WriteLine("This iteration took {0} to complete", watch.Elapsed.ToString());

      statistics.RunTimes.Add(watch.Elapsed);

      Output.WritePhase("Building the solution with contracts");
      if (!ExternalCommands.TryBuildSolution(config.Solution, config.MSBuild))
      {
        Output.WriteErrorAndQuit("Can't build the solution");
      }

      statistics.NumberOfSuggestions.Add(Helpers.CountSuggestions(config.GitRoot));
    }

    public static void FixProjectForRoslyn(string projectPath)
    {
      var oldLines = File.ReadAllLines(projectPath);
      var target = "  <Import Project=\"..\\..\\CodeReview.Settings.targets\" />";
      var replacement = "<!--  <Import Project=\"..\\..\\CodeReview.Settings.targets\" />-->";
      var newLines = oldLines.Select(x => x.Equals(target) ? replacement : x);
      File.WriteAllLines(projectPath, newLines);
    }
    public static void FixProjectForMSBuild(string projectPath)
    {
      var oldLines = File.ReadAllLines(projectPath);
      var replacement = "  <Import Project=\"..\\..\\CodeReview.Settings.targets\" />";
      var target = "<!--  <Import Project=\"..\\..\\CodeReview.Settings.targets\" />-->";
      var newLines = oldLines.Select(x => x.Equals(target) ? replacement : x);
      File.WriteAllLines(projectPath, newLines);
    }

    public static void WriteDefaultConfig(string path)
    {
      Contract.Requires(!string.IsNullOrEmpty(path));

      new XmlSerializer(typeof(Configuration)).Serialize(File.OpenWrite(path), Configuration.GetDefaultConfiguration());
    }
  }
}
