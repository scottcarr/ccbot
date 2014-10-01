using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using System.IO.Directory;
using System.Diagnostics;
using System.Environment;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System.Xml;
using Microsoft.Research.ReviewBot;
using Microsoft.Research.ReviewBot.Utils;

namespace Microsoft.Research.ReviewBot.Github
{
  class Program
  {
    //static readonly string gitCmd = @"C:\Program Files (x86)\Git\bin\git.exe";
    static readonly string gitCmd = Path.Combine(GetEnvironmentVariable("ProgramFiles(x86)"), "git", "bin", "git.exe");
    static readonly string selectedReposPath = @"..\..\selectedRepos.txt";
    static readonly string selectedSolutionsPath = @"..\..\selectedSolutions.txt";
    static readonly string scanResultsPath = @"..\..\scanresults.json";
    static void Main(string[] args)
    {
      /*
      const int nPages = 1;
      const int reposPerPage = 100; // max = 100
      SearchResponse results = null;
      for (int i = 0; i < nPages; i++)
      {
        var urlWithHole = "https://api.github.com/search/repositories?q=language:csharp&sort=stars&order=desc&page={0}&per_page={1}/";
        var url = String.Format(urlWithHole, i, reposPerPage);
        var request = HttpWebRequest.Create(new Uri(url)) as HttpWebRequest;
        request.UserAgent = "ReviewBot"; // YOU MUST HAVE THIS SET TO SOMETHING !!!!
        var resp = request.GetResponse() as HttpWebResponse;
        var reader = new StreamReader(resp.GetResponseStream());
        results = JsonConvert.DeserializeObject<SearchResponse>(reader.ReadToEnd());
        foreach (var result in results.items) 
        {
            CloneRepo(result.clone_url);
        }
      }
      */

      /*
      // this is generally the order to run but doing this whole thing takes forever
      tryBuildingAllWithRoslyn(results);
      // now go and mark "true" isUserSelected for the solutions you want
      crossCheckWithMsbuild();
      makeNicerResultsFile(results);
      */
      runReviewBotOnSelectedRepos();
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
      //p.StartInfo.WorkingDirectory = Directory.GetParent(slnPath).ToString();
      p.StartInfo.UseShellExecute = false;
      p.Start();
      p.WaitForExit();
      return p.ExitCode == 0;

    }
    static void crossCheckWithMsbuild()
    {
      var hits = new List<string>();
      var misses = new List<string>();
      var text = File.ReadAllText(scanResultsPath);
      var json = JsonConvert.DeserializeObject<ScanResult>(text);
      foreach (var result in json.results)
      {
        foreach(var sln in result.SolutionCanBuildPairs)
        {
          if (sln.canRoslynOpen && sln.isUserChoice)
          {
            if (Msbuild(sln.FilePath))
            {
              hits.Add(sln.FilePath);
            }
            else
            {
              misses.Add(sln.FilePath);
            }
          }
        }
      }
      Console.WriteLine("MSBuild couldn't build:");
      misses.ForEach(Console.WriteLine);
      Console.WriteLine("MSBuild built:");
      hits.ForEach(Console.WriteLine);
      File.WriteAllLines(selectedSolutionsPath, hits);
    }
    static void makeNicerResultsFile(SearchResponse rsp)
    {
      var selections = File.ReadAllLines(@"..\..\selectedSolutions.txt");
      var data = new List<RepoData>();
      foreach (var hit in selections)
      {
        var hitName = hit.Remove(hit.IndexOf("\\"));
        var githubInfo = rsp.items.First(x => x.name == hitName);
        var rd = new RepoData();
        rd.cloneUrl = githubInfo.clone_url;
        rd.name = hitName;
        rd.selectedSolutionPath = hit;
        data.Add(rd);
      }
      var text = JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
      File.WriteAllText(selectedReposPath, text);
      Console.WriteLine(text);

    }
    static void tryBuildingAllWithRoslyn(SearchResponse resp)
    {
      var sr = new ScanResult();
      foreach (var pair in resp.items)
      {
        var name = pair.name;
        var url = pair.clone_url;

        var slnPaths = SolutionTools.ScanForSolutions(name);
        var localResults = new RepoResult();
        localResults.RepoName = name;
        foreach (var slnPath in slnPaths)
        {
          var msbw = MSBuildWorkspace.Create();
          var stats = new SolutionStats();
          stats.FilePath = slnPath;
          try
          {
            var sln = msbw.OpenSolutionAsync(slnPath).Result;
            if (!sln.Projects.Any()) { throw new FileNotFoundException("no projects"); }
            var depGraph = sln.GetProjectDependencyGraph();
            var projs = depGraph.GetTopologicallySortedProjects();
            var assemblies = new List<Stream>();
            var results = new List<bool>();
            foreach(var projId in projs)
            {
              var proj = sln.GetProject(projId);
              var stream = new MemoryStream();
              var result = proj.GetCompilationAsync().Result.Emit(stream);
              results.Add(result.Success);
              if (!result.Success)
              {
                stats.Diagnostics = result.Diagnostics.Select(x => x.ToString()).ToList();
              }
              assemblies.Add(stream);
            }
            stats.canRoslynOpen = results.All(x => x);
          }
          catch (Exception e)
          {
            var l = new List<string>();
            l.Add(e.Message);
            l.Add(e.StackTrace);
            stats.Diagnostics.AddRange(l);
            stats.canRoslynOpen = false;
          }
          localResults.SolutionCanBuildPairs.Add(stats);
        }
        sr.results.Add(localResults);
      }
      var text = JsonConvert.SerializeObject(sr, Newtonsoft.Json.Formatting.Indented);
      File.WriteAllText(@"..\..\scanresults.json", text);
      /*
      var msbw = MSBuildWorkspace.Create();
      var sln = msbw.OpenSolutionAsync(@"C:\Users\carr27\Documents\Visual Studio 14\Projects\TestCase1\TestCase1.sln").Result;
      var depGraph = sln.GetProjectDependencyGraph();
      var projs = depGraph.GetTopologicallySortedProjects();
      var assemblies = new List<Stream>();
      foreach (var projId in projs)
      {
        var proj = sln.GetProject(projId);
        var stream = new MemoryStream();
        var er = proj.GetCompilationAsync().Result.Emit(stream);
        if (!er.Success)
        {
          foreach(var d in er.Diagnostics)
          {
            Console.WriteLine(d);
          }
        }
        assemblies.Add(stream);
      }
      */
    }
    static void runReviewBotOnSelectedRepos()
    {
      var text = File.ReadAllText(selectedReposPath);
      var repos = JsonConvert.DeserializeObject<List<RepoData>>(text);
      foreach (var repo in repos)
      {
        CloneRepo(repo.cloneUrl);

        // copy our props file to the solution dir
        try
        {

          File.Copy(@"..\..\Common.CodeContracts.props", Path.Combine(Path.GetDirectoryName(repo.selectedSolutionPath), "Common.CodeContracts.props"));
        }
        catch (IOException)
        { }

        // rewrite every project
        var msbw = MSBuildWorkspace.Create();
        var sln = msbw.OpenSolutionAsync(repo.selectedSolutionPath).Result;
        foreach (var proj in sln.Projects)
        {
          RewriteProject(proj.FilePath);
        }
        Msbuild(repo.selectedSolutionPath);

        var rsps = GetFiles(Path.GetDirectoryName(repo.selectedSolutionPath), "*.rsp", SearchOption.AllDirectories);

        // run reviewbot
        foreach(var proj in sln.Projects)
        {
          var conf = new Configuration();
          var defaultConfig = Configuration.GetDefaultConfiguration();
          conf.GitRoot = Path.Combine(Directory.GetCurrentDirectory(), repo.name);
          conf.Git = gitCmd;
          conf.MSBuild = "msbuild"; // it should just be in your path
          conf.Solution = repo.selectedSolutionPath;
          conf.Cccheck = Path.Combine(GetEnvironmentVariable("ProgramFiles(x86)"), "Microsoft", "Contracts", "bin", "cccheck.exe");
          conf.Project = proj.FilePath;
          conf.CccheckOptions = "-xml -remote=false -suggest methodensures -suggest propertyensures -suggest objectinvariants -suggest necessaryensures  -suggest readonlyfields -suggest assumes -suggest nonnullreturn -sortWarns=false -warninglevel full -maxwarnings 99999999";
          conf.RSP = Path.Combine(Directory.GetCurrentDirectory(), rsps.First(x => x.Contains(proj.Name + "cccheck.rsp")));
          conf.GitBaseBranch = "master";
          conf.CccheckXml = Path.Combine(Directory.GetCurrentDirectory(), proj.Name + "_clousot.xml");

          ReviewBotMain.BuildAnalyzeInstrument(conf);

          //if (!ExternalCommands.TryRunClousot(conf.CccheckXml, conf.Cccheck, conf.CccheckOptions, conf.RSP))
          //{
          //  Console.WriteLine("couldn't run clousot");
          //  return;
          //}
          //return;

        }
      }
    }
    static void RewriteProject(string path)
    {
      var oldDoc = new XmlDocument();
      oldDoc.LoadXml(File.ReadAllText(path));
      var children = oldDoc.ChildNodes;
      foreach (var child in children)
      {
        var node = child as XmlElement;
        if (node != null )
        {
          var newnode = oldDoc.CreateElement("Import", oldDoc.DocumentElement.NamespaceURI);
          newnode.SetAttribute("Project", "$(SolutionDir)\\Common.CodeContracts.props");
          node.AppendChild(newnode);

        }
      }
      oldDoc.Save(path);
    }
  }
} 