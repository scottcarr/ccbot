using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System.Xml;
using Microsoft.Research.ReviewBot;
using Microsoft.Research.ReviewBot.Utils;
using System.Reflection;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Runtime;
//using Microsoft.DiaSymReader;

namespace Microsoft.Research.ReviewBot.Github
{
  class Program
  {
    //static readonly string gitCmd = @"C:\Program Files (x86)\Git\bin\git.exe";
    static readonly string gitCmd = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles(x86)"), "git", "bin", "git.exe");
    static readonly string selectedReposPath = @"..\..\selectedRepos.txt";
    static readonly string selectedSolutionsPath = @"..\..\selectedSolutions.txt";
    static readonly string scanResultsPath = @"..\..\scanresults.json";
    static readonly string msbuildResultsPath = @"..\..\msbuildresults.txt";
    static readonly string msbuildPath = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles(x86)"), "MSBuild", "14.0", "Bin", "msbuild.exe");
    static readonly string autoConfResults = "repoInfo.json";
    //static readonly string msbuildPath = "msbuild.exe";
    static string startDir = @"C:\Users\carr27\Documents\Github";
    static readonly string[] badSolutions = {
            @"corefx\src\Microsoft.CSharp\Microsoft.CSharp.sln",
            @"corefx\src\Microsoft.Win32.Registry\Microsoft.Win32.Registry.sln",
            @"corefx\src\System.Collections.Immutable\System.Collections.Immutable.sln",
            @"corefx\src\System.ComponentModel.Annotations\System.ComponentModel.Annotations.sln",
            @"corefx\src\System.Diagnostics.Process\System.Diagnostics.Process.sln",
            @"corefx\src\System.Globalization.Extensions\System.Globalization.Extensions.sln",
            @"corefx\src\System.IO.FileSystem\System.IO.FileSystem.sln",
            @"corefx\src\System.IO.FileSystem.Watcher\System.IO.FileSystem.Watcher.sln",
            @"corefx\src\System.IO.MemoryMappedFiles\System.IO.MemoryMappedFiles.sln",
            @"corefx\src\System.Linq.Expressions\System.Linq.Expressions.sln",
            @"corefx\src\System.Linq.Parallel\System.Linq.Parallel.sln",
            @"corefx\src\System.Private.DataContractSerialization\System.Private.DataContractSerialization.sln",
            @"corefx\src\System.Reflection.Metadata\System.Reflection.Metadata.sln",
            @"corefx\src\System.Runtime.Serialization.Json\System.Runtime.Serialization.Json.sln",
            @"corefx\src\System.Runtime.Serialization.Xml\System.Runtime.Serialization.Xml.sln",
            @"corefx\src\System.Threading.Tasks.Dataflow\System.Threading.Tasks.Dataflow.sln",
            @"corefx\src\System.Threading.Tasks.Parallel\System.Threading.Tasks.Parallel.sln",
            @"corefx\src\System.Xml.ReaderWriter\System.Xml.ReaderWriter.sln",
            @"corefx\src\System.Xml.XDocument\System.Xml.XDocument.sln",
            @"corefx\src\System.Xml.XmlDocument\System.Xml.XmlDocument.sln",
            @"corefx\src\System.Xml.XmlSerializer\System.Xml.XmlSerializer.sln",
            @"corefx\src\System.Xml.XPath\System.Xml.XPath.sln",
            @"corefx\src\System.Xml.XPath.XDocument\System.Xml.XPath.XDocument.sln",
            @"corefx\src\System.Xml.XPath.XmlDocument\System.Xml.XPath.XmlDocument.sln",
            @"mono\mcs\class\System.Net.Http\System.Net.Http-net_4_5.sln",
            @"mono\msvc\scripts\net_4_5.sln",
            @"OpenRA\OpenRA.sln",
            @"EntityFramework\EntityFramework.sln",
            @"ravendb\Imports\Newtonsoft.Json\Src\Newtonsoft.Json.WindowsPhone.sln",
            //@"roslyn\src\Roslyn.sln",
            //@"roslyn\src\RoslynLight.sln",
            //@"roslyn\src\Toolset.sln",
            @"roslyn\src\InteractiveWindow\InteractiveWindow.sln",
            @"ravendb\Imports\Newtonsoft.Json\Src\Newtonsoft.Json.Silverlight.sln",
            @"monodevelop\main\Main.sln"

    };

    static string stripBaseDir(string otherdir)
    {
      int n = startDir.Length;
      return otherdir.Substring(n);
    }

    static void Main(string[] args)
    {
      /*
      var msbw = MSBuildWorkspace.Create();
      var sln = msbw.OpenSolutionAsync(@"C:\Users\carr27\Documents\GitHub\roslyn\src\RoslynLight.sln").Result;
      var proj = sln.Projects.First(x => x.Name == "CodeAnalysis.Desktop");
      Console.WriteLine(proj.FilePath);
      var facadesDir = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\";
      proj = proj.AddMetadataReference(MetadataReference.CreateFromAssembly(typeof(object).Assembly));
      proj = proj.AddMetadataReference(MetadataReference.CreateFromFile(facadesDir + "System.Runtime.dll"));
      proj = proj.AddMetadataReference(MetadataReference.CreateFromFile(facadesDir + "System.Runtime.Extensions.dll"));
      proj = proj.AddMetadataReference(MetadataReference.CreateFromFile(facadesDir + "System.IO.dll"));
      proj = proj.AddMetadataReference(MetadataReference.CreateFromFile(facadesDir + "System.Threading.Tasks.dll"));
      proj = proj.AddMetadataReference(MetadataReference.CreateFromFile(facadesDir + "System.Text.Encoding.dll"));
      proj = proj.AddMetadataReference(MetadataReference.CreateFromFile(facadesDir + "System.Reflection.dll"));
      var cu = proj.GetCompilationAsync().Result;
      foreach (var e in cu.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error))
      {
        Console.WriteLine("{0}: {1}", e.Location, e.GetMessage());
      }
      Console.WriteLine("done.");
      Console.ReadKey();
      */
      //var githubResp = GetGitHubList(30);

      //CloneAll(githubResp);

      //CreateSolutionAndProjectLists(githubResp);

      WriteRepoInfoFile("roslyn");


      //trOpeningAllWithRoslyn(githubResp);


      //TryBuildAllWithMSBuild();
      //var usables = findUsableProjects();
      //Console.WriteLine(usables.Count);
      //Console.WriteLine(" out of ");
      //Console.WriteLine(countTotalProjects());
      //makeNicerResultsFile(results);
      //runReviewBotOnUsableRepos();
      Console.WriteLine("press a key to exit");
      Console.ReadKey();
    }

#if false
    static void CreateSolutionAndProjectLists(SearchResponse githubResp)
    {
      /*
      List<RepoInfo> repoInfos;
      if (!File.Exists(autoConfResults)) 
      {
        repoInfos = new List<RepoInfo>();
      }
      else
      {
        var text = File.ReadAllText(autoConfResults);
        repoInfos = JsonConvert.DeserializeObject<List<RepoInfo>>(text);
        if (repoInfos == null)
        {
          repoInfos = new List<RepoInfo>();
        }
      }
      */
      //var repoInfos = new List<RepoInfo>();
      foreach (var i in githubResp.items) 
      {
        var entry = new RepoInfo();
        entry.RepoName = i.name;
        var repoDir = Path.Combine(startDir, i.name);
        foreach (var slnPath in Directory.GetFiles(repoDir, "*.sln", SearchOption.AllDirectories))
        {
          Console.WriteLine("Opening solution: " + slnPath);
          var msbw = MSBuildWorkspace.Create();
          Solution sln;
          var slnInfo = new SolutionInfo();
          slnInfo.FilePath = stripBaseDir(slnPath);
          if (badSolutions.Any(x => slnPath.EndsWith(x)))
          {
            slnInfo.canRoslynOpen = false;
            slnInfo.skipped = true;
            entry.SolutionInfos.Add(slnInfo);
          }
          else
          {
            try
            {

              sln = msbw.OpenSolutionAsync(slnPath).Result;
              slnInfo.canRoslynOpen = true;
              slnInfo = new SolutionInfo();
              foreach (var proj in sln.Projects)
              {
                var pInfo = new ProjectInfo();
                pInfo.FilePath = stripBaseDir(proj.FilePath);
                try
                {
                  var cu = proj.GetCompilationAsync().Result;
                  var errors = cu.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error);
                  if (errors.Any())
                  {
                    pInfo.hasErrors = true;
                    pInfo.error = String.Join("\n", errors.Select(x => x.ToString()));
                  }
                }
                catch (Exception e)
                {
                  pInfo.hasErrors = true;
                  pInfo.error = e.ToString();

                }
                slnInfo.Projects.Add(pInfo);
              }
              entry.SolutionInfos.Add(slnInfo);
            }
            catch (Exception e)
            {
              slnInfo.error = e.Message;
              slnInfo.canRoslynOpen = false;
              entry.SolutionInfos.Add(slnInfo);
            }
          }
        }
        //repoInfos.Add(entry);
        var text2 = JsonConvert.SerializeObject(entry, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText(entry.RepoName + "_reviewbot.json", text2);
      }

      //var text2 = JsonConvert.SerializeObject(repoInfos, Newtonsoft.Json.Formatting.Indented);
      //File.WriteAllText(autoConfResults, text2);
    }
#endif

    static void WriteRepoInfoFile(string repo_name)
    {
      var entry = new RepoInfo();
      entry.RepoName = repo_name;
      var repoDir = Path.Combine(startDir, repo_name);
      foreach (var slnPath in Directory.GetFiles(repoDir, "*.sln", SearchOption.AllDirectories))
      {
        Console.WriteLine("Opening solution: " + slnPath);
        var msbw = MSBuildWorkspace.Create();
        Solution sln;
        var slnInfo = new SolutionInfo();
        slnInfo.FilePath = stripBaseDir(slnPath);
        if (badSolutions.Any(x => slnPath.EndsWith(x)))
        //if (false)
        {
          slnInfo.canRoslynOpen = false;
          slnInfo.skipped = true;
          entry.SolutionInfos.Add(slnInfo);
        }
        else
        {
          try
          {

            sln = msbw.OpenSolutionAsync(slnPath).Result;
            slnInfo.canRoslynOpen = true;
            //slnInfo = new SolutionInfo();
            foreach (var proj in sln.Projects)
            {
              var pInfo = new ProjectInfo();
              pInfo.FilePath = stripBaseDir(proj.FilePath);
              try
              {
                var facadesDir = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\";

                var newproj = proj.AddMetadataReference(MetadataReference.CreateFromAssembly(typeof(object).Assembly));
                newproj = newproj.AddMetadataReference(MetadataReference.CreateFromFile(facadesDir + "System.Runtime.dll"));
                newproj = newproj.AddMetadataReference(MetadataReference.CreateFromFile(facadesDir + "System.Runtime.Extensions.dll"));
                newproj = newproj.AddMetadataReference(MetadataReference.CreateFromFile(facadesDir + "System.IO.dll"));
                newproj = newproj.AddMetadataReference(MetadataReference.CreateFromFile(facadesDir + "System.Threading.Tasks.dll"));
                newproj = newproj.AddMetadataReference(MetadataReference.CreateFromFile(facadesDir + "System.Text.Encoding.dll"));
                newproj = newproj.AddMetadataReference(MetadataReference.CreateFromFile(facadesDir + "System.Reflection.dll"));
                newproj = newproj.AddMetadataReference(MetadataReference.CreateFromFile(facadesDir + "System.Linq.dll"));
                newproj = newproj.AddMetadataReference(MetadataReference.CreateFromFile(facadesDir + "System.Collections.dll"));
                var cu = newproj.GetCompilationAsync().Result;
                var errors = cu.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error);
                if (errors.Any())
                {
                  pInfo.Diagnostics = errors.Select(x => x.GetMessage()).Distinct().ToList();
                }
              }
              catch (Exception e)
              {
                  pInfo.Exception = e.ToString();
              }
              slnInfo.Projects.Add(pInfo);
            }
            entry.SolutionInfos.Add(slnInfo);
          }

          catch (AggregateException e)
          {
            slnInfo.Exceptions.Add(e.Message);
            foreach (var ie in e.InnerExceptions)
            {
              slnInfo.Exceptions.Add(ie.Message);
            }
            slnInfo.canRoslynOpen = false;
            entry.SolutionInfos.Add(slnInfo);
          }
          catch (Exception e)
          {
            slnInfo.Exceptions.Add(e.Message);
            slnInfo.canRoslynOpen = false;
            entry.SolutionInfos.Add(slnInfo);
          }
        }
      }
      //repoInfos.Add(entry);
      var text2 = JsonConvert.SerializeObject(entry, Newtonsoft.Json.Formatting.Indented);
      File.WriteAllText(entry.RepoName + "_reviewbot.json", text2);
    }

    /*
    static int countTotalProjects()
    {
      int i = 0;
      var text = File.ReadAllText(scanResultsPath);
      var json = JsonConvert.DeserializeObject<ScanResult>(text);
      foreach (var result in json.results)
      {
        foreach (var sln in result.SolutionResults)
        {
          foreach (var p in sln.Projects)
          {
            ++i;
          }
        }
      }
      return i;
    }

    static List<Usable> findUsableProjects()
    {
      var usables = new List<Usable>();
      var text = File.ReadAllText(scanResultsPath);
      var json = JsonConvert.DeserializeObject<ScanResult>(text);
      foreach (var result in json.results)
      {
        foreach (var sln in result.SolutionResults)
        {
          if (sln.canMsBuild)
          {
            foreach (var p in sln.Projects)
            {
              if (p.canRoslynOpen)
              {
                var usable = new Usable();
                usable.SolutionPath = sln.FilePath;
                usable.ProjectPath = p.FilePath;
                usable.RepoName = result.RepoName;
                usables.Add(usable);
                //Console.WriteLine(p.FilePath);
              }
            }
          }
        }
      }
      return usables;
    }

     * */

    static void CloneAll(SearchResponse resp)
    {
      foreach (var result in resp.items)
      {
        CloneRepo(result.clone_url);
      }
    }

    static SearchResponse GetGitHubList(int nRepos)
    {
      int nPages = 1;
      var urlWithHole = "https://api.github.com/search/repositories?q=language:csharp&sort=stars&order=desc&page={0}&per_page={1}/";
      var url = String.Format(urlWithHole, nPages, nRepos);
      var request = HttpWebRequest.Create(new Uri(url)) as HttpWebRequest;
      request.UserAgent = "ReviewBot"; // YOU MUST HAVE THIS SET TO SOMETHING !!!!
      var resp = request.GetResponse() as HttpWebResponse;
      var reader = new StreamReader(resp.GetResponseStream());
      return JsonConvert.DeserializeObject<SearchResponse>(reader.ReadToEnd());
    }

    static void writeJson(List<RepoInfo> sr, string path)
    {
      var text = JsonConvert.SerializeObject(sr, Newtonsoft.Json.Formatting.Indented);
      File.WriteAllText(scanResultsPath, text);
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
    //static bool Msbuild(string slnPath)
    //{
    //  var p = new Process();
    //  p.StartInfo.FileName = msbuildPath;
    //  p.StartInfo.Arguments = slnPath + " /toolsversion:12.0"; // I needed the tools version.  maybe you dont
    //  //p.StartInfo.WorkingDirectory = Directory.GetParent(slnPath).ToString();
    //  p.StartInfo.UseShellExecute = false;
    //  p.Start();
    //  p.WaitForExit();
    //  return p.ExitCode == 0;

    //}
    /*
    static void TryBuildAllWithMSBuild()
    {
      var hits = new List<string>();
      var misses = new List<string>();
      var text = File.ReadAllText(scanResultsPath);
      var json = JsonConvert.DeserializeObject<List<RepoInfo>>(text);
      var res = File.OpenWrite(msbuildResultsPath);
      foreach (var result in json)
      {
        foreach (var sln in result.SolutionResults)
        {
          sln.canMsBuild = ExternalCommands.TryBuildSolution(sln.FilePath, msbuildPath);
        }
      }
      writeJson(json, scanResultsPath);
    }
    */
    /*
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
    */
#if false
    static void trOpeningAllWithRoslyn(SearchResponse resp)
    {
      var sr = new ScanResult();
      foreach (var pair in resp.items)
      {
        var name = pair.name;
        var url = pair.clone_url;

        var slnPaths = SolutionTools.ScanForSolutions(name);
        var localResults = new RepoInfo();
        localResults.RepoName = name;
        if (name.StartsWith("corefx"))
        {
          localResults.skipped = true;
          localResults.comment = "project Microsoft.CSharp doesn't build with roslyn";
          sr.results.Add(localResults);
          continue;
        }
        if (name.StartsWith("mono"))
        {
          localResults.skipped = true;
          localResults.comment = "mono takes forever to even open in roslyn";
          sr.results.Add(localResults);
          continue;
        }
        if (name.StartsWith("OpenRA"))
        {
          localResults.skipped = true;
          localResults.comment = "openRA takes forever to open in roslyn";
          sr.results.Add(localResults);
          continue;
        }
        if (name.StartsWith("Newtonsoft.Json"))
        {
          localResults.skipped = true;
          localResults.comment = "roslyn throws assert";
          sr.results.Add(localResults);
          continue;
        }
        if (name.StartsWith("roslyn"))
        {
          localResults.skipped = true;
          localResults.comment = "roslyn throws assert";
          sr.results.Add(localResults);
          continue;
        }
        if (name.StartsWith("ravendb"))
        {
          localResults.skipped = true;
          localResults.comment = "doesn't build with roslyn";
          sr.results.Add(localResults);
          continue;
        }
        if (name.StartsWith("SignalR"))
        {
          localResults.skipped = true;
          localResults.comment = "roslyn cant open the solution";
          sr.results.Add(localResults);
          continue;
        }
        /*
        if (name.StartsWith("ILSpy"))
        { 
          localResults.skipped = true;
          localResults.comment = "doesn't build with roslyn";
          sr.results.Add(localResults);
          continue;
        }
        */
        foreach (var slnPath in slnPaths)
        {
          Console.WriteLine("In solution: " + slnPath);
          var msbw = MSBuildWorkspace.Create();
          var stats = new SolutionInfo();
          stats.FilePath = slnPath;
          Solution sln = null;
          try
          {
            sln = msbw.OpenSolutionAsync(slnPath).Result;
            stats.canRoslynOpen = true;
            if (!sln.Projects.Any()) { throw new FileNotFoundException("no projects"); }
          }
          catch
          {
            stats.canRoslynOpen = false;
            continue;
          }
          var depGraph = sln.GetProjectDependencyGraph();
          var projs = depGraph.GetTopologicallySortedProjects();
          //var assemblies = new List<Stream>();
          //var results = new List<bool>();
          foreach (var projId in projs)
          {
            var pstats = new ProjectInfo();
            pstats.canRoslynOpen = true;
            var proj = sln.GetProject(projId);
            pstats.FilePath = proj.FilePath;
            Console.WriteLine("Building project: " + proj.Name);
            var stream = new MemoryStream();
            try
            {
              var comp = proj.GetCompilationAsync().Result;
              string pathToDll = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5.2\Facades\";
              var metadataReferences = new List<string> {
              "System.Runtime.dll",
              "System.Runtime.Extensions.dll",
              "System.Threading.Tasks.dll",
              "System.IO.dll",
              "System.Reflection.dll",
              "System.Text.Encoding.dll"}.Select(s => MetadataReference.CreateFromFile(pathToDll + s));
              comp = comp.AddReferences(metadataReferences);
              comp.AddReferences(new MetadataReference[] { MetadataReference.CreateFromAssembly(typeof(object).Assembly) });
              var errors = comp.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error);
              if (errors.Any())
              {
                pstats.Diagnostics = errors.Select(x => x.ToString()).ToList();
                pstats.canRoslynOpen = false;
              }
              //var result = proj.GetCompilationAsync().Result.Emit(stream);
              //results.Add(result.Success);
              //if (!result.Success)
              //{
              //  stats.Diagnostics = result.Diagnostics.Select(x => x.ToString()).ToList();
              //}
              //assemblies.Add(stream);
            }
            catch (Exception e)
            {
              var l = new List<string>();
              l.Add(e.Message);
              l.Add(e.StackTrace);
              pstats.Diagnostics.AddRange(l);
              pstats.canRoslynOpen = false;
            }
            stats.Projects.Add(pstats);
            //stats.canRoslynOpen = results.All(x => x);
          }
          localResults.SolutionResults.Add(stats);
        }
        sr.results.Add(localResults);
      }
      writeJson(sr, scanResultsPath);

    }
/*
    static void runReviewBotOnUsableRepos()
    {
      var text = File.ReadAllText(scanResultsPath);
      var scan = JsonConvert.DeserializeObject<ScanResult>(text);
      var usable = findUsableProjects();
      var usableBySln = usable.GroupBy(x => x.SolutionPath);
      foreach (var slnGrp in usableBySln)
      {


        // rewrite every project
        MSBuildWorkspace msbw = null;
        try
        {
          msbw = MSBuildWorkspace.Create();

        }
        catch (ReflectionTypeLoadException ex)
        {
          StringBuilder sb = new StringBuilder();
          foreach (Exception exSub in ex.LoaderExceptions)
          {
            sb.AppendLine(exSub.Message);
            FileNotFoundException exFileNotFound = exSub as FileNotFoundException;
            if (exFileNotFound != null)
            {
              if (!string.IsNullOrEmpty(exFileNotFound.FusionLog))
              {
                sb.AppendLine("Fusion Log:");
                sb.AppendLine(exFileNotFound.FusionLog);
              }
            }
            sb.AppendLine();
          }
          string errorMessage = sb.ToString();
          //Display or log the error based on your application.
          Console.WriteLine(errorMessage);
        }

        // enable code contracts first -- you cant edit a project file while roslyn has it open
        foreach (var p in slnGrp)
        {
          Output.WriteLine("Editing {0} to enable code contracts", p.ProjectPath);
          var slnDir = Path.GetDirectoryName(p.SolutionPath);
          var ccFile = Path.Combine(slnDir, "Common.CodeContracts.props");
          if (!File.Exists(ccFile))
          {
            File.Copy(@"..\..\Common.CodeContracts.props", ccFile);
          }
          Helpers.EnableCodeContractsInProject(p.ProjectPath);
        }
        var slnPath = slnGrp.First().SolutionPath;
        Output.WriteLine("Building {0} to generate RSP files", slnPath);
        if (!ExternalCommands.TryBuildSolution(slnPath, msbuildPath))
        {
          Output.WriteErrorAndQuit("MSBuild couldn't build: " + slnPath);
          return;
        }
        var sln = msbw.OpenSolutionAsync(slnPath).Result;
        var rsps = Directory.GetFiles(Path.GetDirectoryName(sln.FilePath), "*.rsp", SearchOption.AllDirectories);
        // run reviewbot
        foreach (var proj in sln.Projects)
        {
          if (slnGrp.Any(x => x.ProjectPath == proj.FilePath)) // don't both with projects that don't work with rolsyn/msbuild
          {
            var projDir = Path.GetDirectoryName(proj.FilePath);
            var repoName = slnGrp.First().RepoName;
            var conf = new Configuration();
            var defaultConfig = Configuration.GetDefaultConfiguration();
            conf.GitRoot = Path.Combine(Directory.GetCurrentDirectory(), repoName);
            conf.Git = gitCmd;
            conf.MSBuild = msbuildPath;
            conf.Solution = sln.FilePath;
            conf.Cccheck = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles(x86)"), "Microsoft", "Contracts", "bin", "cccheck.exe");
            conf.Project = proj.FilePath;
            conf.CccheckOptions = "-xml -remote=false -suggest methodensures -suggest propertyensures -suggest objectinvariants -suggest necessaryensures  -suggest readonlyfields -suggest assumes -suggest nonnullreturn -sortWarns=false -warninglevel full -maxwarnings 99999999";

            // if the next line dies, it couldnt find the rsp.  do you have cccheck installed?
            conf.RSP = Path.Combine(Directory.GetCurrentDirectory(), rsps.First(x => x.Contains(proj.Name + "cccheck.rsp")));
            conf.GitBaseBranch = "master";
            conf.CccheckXml = Path.Combine(projDir, proj.Name, "_clousout.xml");

            ReviewBotMain.BuildAnalyzeInstrument(conf);
            //return; // run only once for debugging
          }
        }
      }
    }
#endif
  }
} 