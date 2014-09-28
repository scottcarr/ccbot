using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using System.Diagnostics;

namespace Github
{
  class Program
  {
    static readonly string gitCmd = @"C:\Program Files (x86)\Git\bin\git.exe";
    static void Main(string[] args)
    {
      const int nPages = 1;
      const int reposPerPage = 1; // max = 100
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
      var knownRepos = new KnowRepos();
      foreach (var pair in repos)
      {
        var name = pair.Item1;
        var url = pair.Item2;
        RepoInfo info;
        if (knownRepos.TryGetRepoInfo(name, out info))
        {
          info.Build(Path.Combine(Directory.GetCurrentDirectory(), name));
        }
        else
        {
          var slns = SolutionTools.ScanForSolutions(name);
          foreach (var sln in slns)
          {
            Console.WriteLine(sln);
          }
          var chosenSln = SolutionTools.HeuristicallyDetermineBestSolution(slns);
          Console.WriteLine("Chosen solution: ", chosenSln.FilePath);
        }
      }
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
  }
} 