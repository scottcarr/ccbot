using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Research.ReviewBot.Utils;

namespace Microsoft.Research.ReviewBot
{
  public static class AutoConfig
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
    public static bool TryAutoConfig(string solutionPath, string projectPath, bool CreateBranch, out Configuration conf, UberLogger logger, string gitBaseBranch="master")
    {
      conf = new Configuration();
      conf.GitRoot = Path.GetDirectoryName(solutionPath); // I'm not sure if we really need the root
      conf.GitBaseBranch = gitBaseBranch; 
      conf.Project = projectPath;
      conf.Solution = solutionPath;
      if (!File.Exists(conf.Solution))
      {
        logger.Error("solution path [{0}] doesn't exist", conf.Solution);
        return false;
      }
      if (!File.Exists(conf.Project))
      {
        logger.Error("project path [{0}] doesn't exist", conf.Project);
        return false;
      }
      if (!TryFindProgramPaths(conf, logger))
      {
        return false;
      }
      var repo = new GitRepo(conf);
      if (CreateBranch)
      {
        repo.CreateBranch("reviewbot");
      }
      //Git.RevertToOriginal(conf.GitRoot, conf.GitBaseBranch, conf.Git);
      var contractsProps = Helpers.EnableCodeContractsInProject(conf.Project);
      if (!TrySelectFilesWithExtension("cccheck.rsp", Path.GetDirectoryName(conf.Project), out conf.RSP)) 
      {
        logger.Message("Couldn't find a *.rsp file.  Will try to create one");
        if (!MSBuilder.TryBuildProject(conf.Project, logger))
        {
          logger.Error("Couldn't build project.");
        }
        if (!TrySelectFilesWithExtension("cccheck.rsp", Path.GetDirectoryName(conf.Project), out conf.RSP)) 
        {
          logger.Error("Couldn't find rsp after enabling code contracts and building.");
        }
      }

      repo.Add(conf.RSP);
      repo.Add(conf.Project);
      repo.Add(contractsProps);
      repo.Commit("setup for reviewbot");

      conf.CccheckXml = GenerateCccheckOutputName(conf.Solution, conf.Project);
      conf.CccheckOptions = "-xml -remote=false -suggest methodensures -suggest propertyensures -suggest objectinvariants -suggest necessaryensures  -suggest readonlyfields -suggest assumes -suggest nonnullreturn -sortWarns=false -warninglevel full -maxwarnings 99999999";
      return true;
    }
    public static string GenerateCccheckOutputName(string solutionPath, string projectPath)
    {
      var now = DateTime.Now;
      var when = string.Format("{0}-{1}-{2}-{3}-{4}-{5}", now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
      return Path.Combine(Constants.String.TempDir, Path.GetFileNameWithoutExtension(solutionPath) + "_" + Path.GetFileNameWithoutExtension(projectPath) + "_cccheck" + when + ".xml");

    }
    public static bool TryAutoConfig(string GitRoot, out Configuration conf, UberLogger logger) 
    {

      conf = new Configuration();
      conf.GitRoot = GitRoot;
      conf.GitBaseBranch = "master"; // a guess, TODO check if this is the default branch
      if (!TryFindSolution(conf.GitRoot, out conf.Solution))
      {
        logger.Error("Couldn't find a *.sln file");
        return false;
      }
      if (!TryFindProject(conf.Solution, out conf.Project))
      {
        logger.Error("Couldn't find a *.csproj file");
        return false;
      }
      return TryAutoConfig(conf.Solution, conf.Project, true, out conf, logger);
    }
    static bool TryFindProgramPaths(Configuration conf, UberLogger logger)
    {
      if (!TryFindCcCheck(out conf.Cccheck))
      {
        logger.Error("Couldn't find cccheck.exe");
        return false;
      }
      if (!TryFindGit(out conf.Git))
      {
        logger.Error("Couldn't find msbuild.exe");
        return false;
      }
      return true;
    }

    static void PrintUsage()
    {
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
    public static bool TrySelectFilesWithExtension(string extension, string basedir, out string selected)
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
