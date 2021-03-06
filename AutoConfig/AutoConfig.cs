﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Research.ReviewBot.Utils;

namespace Microsoft.Research.ReviewBot.AutoConfig
{
  class Program
  {
    static string[] msbuildHints = {
                              @"C:\Program Files (x86)\MSBuild\14.0\bin\MSBuild.exe"
                                   };
    static string[] gitHints = {
                          @"C:\Program Files (x86)\Git\bin\git.exe" 
                               };
    static string[] cccheckHints = {
                          @"C:\Program Files (x86)\Microsoft\Contracts\Bin\cccheck.exe" 
                                   };
    static void Main(string[] args)
    {
      /*
      ExternalCommands.TryBuildProject(@"C:\Users\carr27\Documents\GitHub\reviewbot\Github\bin\Debug\Nancy\src\Nancy\Nancy.csproj");
      return;
      */

      if (args.Length != 1)
      {
        PrintUsage();
        return;
      }
      //var conf = new Configuration();
      var GitRoot = args[0];
      var GitBaseBranch = "master"; // a guess, TODO check if this is the default branch
      string Cccheck;
      string git;
      string Solution;
      string Project;
      string RSP;
      if (!TryFindCcCheck(out Cccheck))
      {
        Console.WriteLine("Couldn't find cccheck.exe");
        return;
      }
      /*
      if (!TryFindMsbuild(out conf.MSBuild))
      {
        Output.WriteErrorAndQuit("Couldn't find msbuild.exe");
      }
      */
      if (!TryFindGit(out git))
      {
        //Output.WriteErrorAndQuit("Couldn't find msbuild.exe");
        Console.WriteLine("Couldn't find msbuild.exe");
        return;
      }
      if (!TryFindSolution(GitRoot, out Solution))
      {
        //Output.WriteErrorAndQuit("Couldn't find a *.sln file");
        Console.WriteLine("Couldn't find a *.sln file");
        return;
      }
      if (!TryFindProject(Solution, out Project))
      {
        //Output.WriteErrorAndQuit("Couldn't find a *.csproj file");
        Console.WriteLine("Couldn't find a *.csproj file");
      }
      /*
      if (!ExternalCommands.TryAutoBuildSolution(conf.Solution))
      {
        Output.WriteErrorAndQuit("Couldn't auto build solution");
      }
      if (!ExternalCommands.TryRestoreNugetPackages(conf.GitRoot, conf.Solution))
      {
        Output.WriteErrorAndQuit("Couldn't restore NuGet packages");
      }
      */
      //var xmls = new XmlSerializer(conf.GetType());
      //var sw = new StringWriter();
      //xmls.Serialize(sw, conf);
      //Console.WriteLine(sw);
      //Git.RevertToOriginal(conf.GitRoot, conf.GitBaseBranch, conf.Git);
      Git.RevertToOriginal(GitRoot, GitBaseBranch, git);
      Helpers.EnableCodeContractsInProject(Project);
      if (!TrySelectFilesWithExtension("cccheck.rsp", Path.GetDirectoryName(Project), out RSP)) 
      {
        //Output.WriteLine("Couldn't find a *.rsp file.  Will try to create one");
        Console.WriteLine("Couldn't find a *.rsp file.  Will try to create one");
        if (!MSBuilder.TryBuildProject(Project))
        {
          //Output.WriteErrorAndQuit("Couldn't build project.");
          Console.WriteLine("Couldn't build project.");
        }
        if (!TrySelectFilesWithExtension("cccheck.rsp", Path.GetDirectoryName(Project), out RSP)) 
        {
          //Output.WriteErrorAndQuit("Couldn't find rsp after enabling code contracts and building.");
          Console.WriteLine("Couldn't find rsp after enabling code contracts and building.");
        }
      } else
      {
        //Output.WriteLine("Found existing .rsp file.");
        Console.WriteLine("Found existing .rsp file.");
      }
      /*
      conf.CccheckXml = Path.Combine(Path.GetDirectoryName(conf.Project), Path.GetFileNameWithoutExtension(conf.Project) + "_cccheck.xml");
      conf.CccheckOptions = "-xml -remote=false -suggest methodensures -suggest propertyensures -suggest objectinvariants -suggest necessaryensures  -suggest readonlyfields -suggest assumes -suggest nonnullreturn -sortWarns=false -warninglevel full -maxwarnings 99999999";
      if (!File.Exists(conf.CccheckXml))
      {
        Output.WriteLine("Running cccheck to create xml file.");
        if (!ExternalCommands.TryRunClousot(conf.CccheckXml, conf.Cccheck, conf.CccheckOptions, conf.RSP))
        {
          Output.WriteErrorAndQuit("Couldn't run clousot");
        }
      }
      Output.WriteLine("Config ready.  Running reviewbot.");
      var xmls = new XmlSerializer(conf.GetType());
      var sw = new StringWriter();
      xmls.Serialize(sw, conf);
      Console.WriteLine(sw);
      //ReviewBotMain.BuildAnalyzeInstrument(conf);
      int i = 0;
      var reviewArgs = new string[] {
                                conf.CccheckXml, 
                                "-project", conf.Project, 
                                "-solution", conf.Solution, 
                                "-output", "git", "-gitroot", conf.GitRoot
                              };
      Annotator.DoAnnotate(reviewArgs);
      */
      Console.WriteLine("Done.");
      Console.ReadKey();
    }
    static void PrintUsage()
    {
      //Output.WriteErrorAndQuit("Wrong number of arguments");
      Console.WriteLine("Wrong number of arguments");
    }
    static bool TryFindCcCheck(out string cccheck)
    {
      return TryFindExe("cccheck.exe", cccheckHints, out cccheck);
    }
    static bool TryFindMsbuild(out string msbuild)
    {
      return TryFindExe("msbuild.exe", msbuildHints, out msbuild);
    }
    static bool TryFindGit(out string git)
    {
      return TryFindExe("git.exe", gitHints, out git);
    }
    static bool TryFindSolution(string basedir, out string sln)
    {
      return TrySelectFilesWithExtension(".sln", basedir, out sln);
    }
    static bool TryFindProject(string solutionPath, out string proj)
    {
      var basedir = Path.GetDirectoryName(solutionPath);
      return TrySelectFilesWithExtension(".csproj", basedir, out proj);
      /*
      Output.WriteLine("Opening solution to look for projects.  This might take a minute...");
      var msbw = MSBuildWorkspace.Create();
      var sln = msbw.OpenSolutionAsync(solutionPath).Result;
      var projs = sln.Projects.Select(x => x.FilePath);
      if (projs.Any())
      {
        proj = ChooseOne(projs.ToArray());
        msbw.CloseSolution();
        msbw.Dispose();
        return true;
      } else {
        proj = "";
        msbw.CloseSolution();
        msbw.Dispose();
        return false;
      }
      */
    }
    static bool TryFindExe(string exename, string[] hints, out string exepath)
    {
      var hits = hints.Where(x => File.Exists(x));
      if (hits.Any()) 
      {
        exepath = ChooseOne(hints);
        return true;
      } else {
        // TODO
        // search program files
        exepath = "";
        return false;
      }
    }
    static string ChooseOne(string[] choices)
    {
      if (choices.Count() == 1) { return choices[0]; }

      for (int i = 0; i < choices.Count(); ++i)
      {
        Console.Write("[{0}]: ", i);
        Console.WriteLine(choices[i]);
      }
      while (true)
      {
        Console.WriteLine("Please select one of the above choices: ");
        var input = Console.ReadLine();
        var choice = Convert.ToInt32(input);
        if (choice >= 0 && choice < choices.Count())
        {
          return choices[choice];
        }
        Console.WriteLine("Invalid choice.  Choose again.");
      }
    }
    static bool TrySelectFilesWithExtension(string extension, string basedir, out string selected)
    {
      var files = Directory.GetFiles(basedir, "*" + extension, SearchOption.AllDirectories);
      if (files.Count() == 1)
      {
        selected = files[0];
        return true;
      }
      if (files.Count() > 0)
      {
        selected = ChooseOne(files);
        return true;
      }
      selected = "";
      return false;
    }
  }
}
