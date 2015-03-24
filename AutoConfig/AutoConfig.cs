using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Research.ReviewBot;
using Microsoft.Research.ReviewBot.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

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
      if (args.Length != 1)
      {
        PrintUsage();
      }
      var conf = new Configuration();
      conf.GitRoot = args[0];
      conf.GitBaseBranch = "master"; // a guess, TODO check if this is the default branch
      if (!TryFindCcCheck(out conf.Cccheck))
      {
        Output.WriteErrorAndQuit("Couldn't find cccheck.exe");
      }
      if (!TryFindMsbuild(out conf.MSBuild))
      {
        Output.WriteErrorAndQuit("Couldn't find msbuild.exe");
      }
      if (!TryFindGit(out conf.Git))
      {
        Output.WriteErrorAndQuit("Couldn't find msbuild.exe");
      }
      if (!TryFindSolution(conf.GitRoot, out conf.Solution))
      {
        Output.WriteErrorAndQuit("Couldn't find a *.sln file");
      }
      if (!TryFindProject(conf.Solution, out conf.Project))
      {
        Output.WriteErrorAndQuit("Couldn't find a *.csproj file");
      }
      if (!ExternalCommands.TryAutoBuildSolution(conf.Solution))
      {
        Output.WriteErrorAndQuit("Couldn't auto build solution");
      }
      /*
      if (!ExternalCommands.TryRestoreNugetPackages(conf.GitRoot, conf.Solution))
      {
        Output.WriteErrorAndQuit("Couldn't restore NuGet packages");
      }
      */
      var xmls = new XmlSerializer(conf.GetType());
      var sw = new StringWriter();
      xmls.Serialize(sw, conf);
      Console.WriteLine(sw);
      Git.RevertToOriginal(conf.GitRoot, conf.GitBaseBranch, conf.Git);
      Helpers.EnableCodeContractsInProject(conf.Project);
      if (!ExternalCommands.TryAutoBuildSolution(conf.Solution, conf.MSBuild))
      {
        Output.WriteErrorAndQuit("Couldn't build solution.");
      }
      if (!TrySelectFilesWithExtension("cccheck.rsp", Path.GetDirectoryName(conf.Project), out conf.RSP)) 
      {
        Output.WriteErrorAndQuit("Couldn't find a *.rsp file");
      }
      conf.CccheckXml = Path.Combine(Path.GetDirectoryName(conf.Project), Path.GetFileNameWithoutExtension(conf.Project) + "_cccheck.xml");
      conf.CccheckOptions = "-xml -remote=false -suggest methodensures -suggest propertyensures -suggest objectinvariants -suggest necessaryensures  -suggest readonlyfields -suggest assumes -suggest nonnullreturn -sortWarns=false -warninglevel full -maxwarnings 99999999";
      xmls.Serialize(sw, conf);
      Console.WriteLine(sw);
      //ReviewBotMain.BuildAnalyzeInstrument(conf);
      int i = 0;
      var reviewArgs = new string[] {
                                string.Format("{0}.{1}.{2}", conf.CccheckXml, i, "xml"), 
                                "-project", conf.Project, 
                                "-solution", conf.Solution, 
                                "-output", "git", "-gitroot", conf.GitRoot
                              };
      Annotator.DoAnnotate(reviewArgs);
      Output.WriteLine("Done.");
      Console.ReadKey();
    }
    static void PrintUsage()
    {
      Output.WriteErrorAndQuit("Wrong number of arguments");
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
      Output.WriteLine("Opening solution to look for projects.  This might take a second...");
      var msbw = MSBuildWorkspace.Create();
      var sln = msbw.OpenSolutionAsync(solutionPath).Result;
      var projs = sln.Projects.Select(x => x.FilePath);
      if (projs.Any())
      {
        proj = ChooseOne(projs.ToArray());
        return true;
      } else {
        proj = "";
        return false;
      }

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
