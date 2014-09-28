using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Github
{
  abstract class RepoInfo
  {
    readonly string Name;
    readonly string CloneUrl;
    private string MainSolution;
    public Solution OpenMainSolution(string repoDirectory)
    {
      var msbw = MSBuildWorkspace.Create();
      return msbw.OpenSolutionAsync(repoDirectory + "/" + MainSolution).Result;
    }
    public abstract void Build(string repoDirectory);
    public RepoInfo(string name, string cloneurl, string mainSolution)
    {
      Name = name;
      CloneUrl = cloneurl;
      MainSolution = mainSolution;
    }
  }
}
