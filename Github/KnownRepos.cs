using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace Github
{
  class KnowRepos
  {
    private Dictionary<string, RepoInfo> reposDict;
    public bool TryGetRepoInfo(string repoName, out RepoInfo info)
    {
      info = null;
      if (reposDict.ContainsKey(repoName))
      {
        info = reposDict[repoName];
        return true;
      }
      return false;
    }
    public KnowRepos()
    {
      reposDict = new Dictionary<string, RepoInfo>();
      reposDict.Add("SignalR", new SignalR());
    }
    class SignalR : RepoInfo
    {
      public SignalR() : base("SignalR" , "https://github.com/SignalR/SignalR.git", "") { }
      public override void Build(string repoDirectory)
      {
        // TODO this doesn't work because it can't find msbuild when it runs the script
        var buildCmd = Path.Combine(repoDirectory, "build.cmd");
        Process p = new Process();
        p.StartInfo.FileName = buildCmd;
        p.StartInfo.UseShellExecute = false;
        p.Start();
        p.WaitForExit();
      }
    }
  }
}
