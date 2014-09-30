using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Environment;

namespace Github
{
  class Program
  {
    //static readonly string gitCmd = @"C:\Program Files (x86)\Git\bin\git.exe";
    static readonly string gitCmd = Path.Combine(GetEnvironmentVariable("ProgramFiles(x86)"), "git", "bin", "git.exe");
    static readonly List<Tuple<string,string>> Successes = new List<Tuple<string,string>>();
    static void Main(string[] args)
    {
      const int nPages = 1;
      const int reposPerPage = 10; // max = 100
      var repos = new List<Tuple<string, string>>(); // for now I am just keeping the name and clone url (both strings)
      for (int i = 0; i < nPages; i++)
      {
        var urlWithHole = "https://api.github.com/search/repositories?q=language:csharp&sort=stars&order=desc&page={0}&per_page={1}/";
        var url = String.Format(urlWithHole, i, reposPerPage);
        var request = HttpWebRequest.Create(new Uri(url)) as HttpWebRequest;
        request.UserAgent = "ReviewBot"; // YOU MUST HAVE THIS SET TO SOMETHING !!!!
        var resp = request.GetResponse() as HttpWebResponse;
        var reader = new StreamReader(resp.GetResponseStream());
        var results = JsonConvert.DeserializeObject<SearchResponse>(reader.ReadToEnd());
        foreach (var result in results.items) 
        {
            repos.Add(new Tuple<string,string>(result.name, result.clone_url));
            CloneRepo(result.clone_url);
        }
      }
      repos.ForEach(Console.WriteLine);
      var knownRepos = new KnownRepos();
      foreach (var pair in repos)
      {
        var name = pair.Item1;
        var url = pair.Item2;
        RepoInfo info;
        if (name.Equals("SignalR")) { continue; } // couldn't get this one to buld
        if (name.Equals("ServiceStack")) { continue; } // couldn't get this one to buld
        if (name.Equals("mono")) { continue; } // couldn't get this one to buld
        if (name.Equals("MonoGame")) { continue; } // couldn't get this one to buld
        if (knownRepos.TryGetRepoInfo(name, out info))
        {
          if (info.Build(Path.Combine(Directory.GetCurrentDirectory(), name)))
          {
            Successes.Add(new Tuple<string, string>(name, "custom script"));
          }
        }
        else
        {
          var slns = SolutionTools.ScanForSolutions(name);
          foreach (var sln in slns)
          {
            Console.WriteLine(sln);
          }
          var chosenSln = SolutionTools.HeuristicallyDetermineBestSolution(slns);
          if (chosenSln != null)
          {
            Console.WriteLine("Chosen solution: ", chosenSln.FilePath);
            if (chosenSln != null)
            {
              Console.WriteLine("Trying to MSBuild {0}", chosenSln.FilePath);
              if (Msbuild(chosenSln.FilePath))
              {
                Successes.Add(new Tuple<string, string>(name, chosenSln.FilePath));
              }
            }
          }
        }
      }
      Console.WriteLine("Successes: ");
      Successes.ForEach(Console.WriteLine);
      Console.WriteLine("press a key to exit");
      Console.ReadKey();
    }
    static void CloneRepo(string url)
    {
      Process p = new Process();
      p.StartInfo.FileName = gitCmd;
      p.StartInfo.Arguments = String.Format("clone {0}", url);
      p.StartInfo.UseShellExecute = true;
      p.Start();
      p.WaitForExit();
    }
    static bool Msbuild(string slnPath)
    {
      var p = new Process();
      p.StartInfo.FileName = "msbuild";
      p.StartInfo.Arguments = slnPath;
      p.StartInfo.WorkingDirectory = Directory.GetParent(slnPath).ToString();
      p.Start();
      p.WaitForExit();
      return p.ExitCode == 0;

    }
  }
} 