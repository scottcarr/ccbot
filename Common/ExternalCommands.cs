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
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.ReviewBot.Utils
{
  public static class ExternalCommands
  {
    public static bool TryBuildSolution(string solutionPath, string MSBuildPath, bool fixproject = false, string projectPath = "")
    {
      Contract.Requires(solutionPath != null);
      Contract.Requires(MSBuildPath != null);

      if (fixproject)
      {
        var oldLines = File.ReadAllLines(projectPath);
        var csproj = File.OpenWrite(projectPath);
        var sw = new StreamWriter(csproj);
        foreach (var line in oldLines)
        {
          if (line == "<!--  <Import Project=\"..\\..\\CodeReview.Settings.targets\" />-->\n")
          {
            sw.WriteLine("  <Import Project=\"..\\..\\CodeReview.Settings.targets\" />\n");
          }
          else
          {
            sw.WriteLine(line);
          }
        }
      }
      return TryRunMSBuild(solutionPath, MSBuildPath);
    }

    private static bool TryRunMSBuild(string solutionPath, string MSBuildPath)
    {
      Contract.Requires(solutionPath != null);
      Contract.Requires(MSBuildPath != null);

      Output.WriteLine("Building the solution {0}", solutionPath);
      Output.WriteLine("Path for msbuild: {0}", MSBuildPath);

      var p = new Process();
      p.StartInfo.UseShellExecute = false;
      p.StartInfo.FileName = MSBuildPath;
      p.StartInfo.Arguments = solutionPath + " /toolsversion:12.0"; // i needed the version, maybe we need another field?
      p.StartInfo.RedirectStandardOutput = true;

      p.Start();
      var buildOutputAsync = p.StandardOutput.ReadToEndAsync();
      Contract.Assume(buildOutputAsync != null, "Missing contract");
      p.WaitForExit(); // It may take a while, as it also runs the CC static checker

      /*
      while(!p.HasExited)
      {
        var id = p.Id;

        var procs = Process.GetProcessesByName("cccheck.exe");

        Process child = procs[0];

        foreach (Process proc in procs)
        {
          proc.Par
          if (id != proc.Id)

            child = proc;
        }
      }
      */

      // Write the output to the file
      var buildOutputFileName = Constants.String.BuildOutputDir(Path.GetFileNameWithoutExtension(solutionPath));
      var text = buildOutputAsync.Result;
      Contract.Assume(text != null);
      Helpers.IO.DumpBuildOutput(buildOutputFileName, text);

      if (p.ExitCode != 0)
      {
        Output.WriteError("Building the solution failed. Check the build output file {0}", buildOutputFileName);
        return false;
      }

      return true;
    }

    public static bool TryRunClousot(string outputFile, string ClousotPath, string ClousotOptions, string rspPath)
    {
      Contract.Requires(ClousotPath != null);

      Output.WritePhase("Running the CC static checker -- It may take a while, please be patient");

      if (!File.Exists(rspPath))
      {
        Output.WriteError("The rsp file does not exists");
        return false;
      }

      string text = null;
      Process p = null;
      var rspFile = "@" + rspPath;

      try
      {
        // Run Clousot
        p = new Process();
        p.StartInfo.FileName = ClousotPath;
        p.StartInfo.Arguments = rspFile + " " + ClousotOptions;
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardOutput = true;
        p.Start();
        var output = p.StandardOutput;
        var reader = output.ReadToEndAsync();
        Contract.Assume(reader != null, "Missing contract");
        p.WaitForExit();
        text = reader.Result;
      }
      catch (Exception e)
      {
        Output.WriteError("Some exception happen while running Clousot. Details {0}", e.Message);
        return false;
      }
      Contract.Assume(text != null);

      try
      {
        Helpers.IO.CreateDirAndWriteAllText(outputFile, text);
      }
      catch (Exception e)
      {
        Output.WriteError("Can't write the xml file with Clousot output. {0}File Path: {1}{0}Exception{2}", Environment.NewLine, outputFile, e.Message);
        return false;
      }

      if (p.ExitCode != 0)
      {
        // The most common error is that Clousot cannot load the dll

        if (text.Contains(Constants.String.CannotLoadAssembly))
        {
          Output.WriteError("Clousot can't load the assembly, did you forgot to stage the enable static checking?");
        }
        else
        {
          Output.WriteError("Something failed in running Clousot");
        }

        return false;
      }

      return true;
    }
    

  }
}
