using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.ReviewBot.Github
{
  class ScanResult
  {
    public readonly List<RepoResult> results = new List<RepoResult>();
  }
  class RepoResult
  {
    public string RepoName;
    public readonly List<SolutionStats> SolutionCanBuildPairs = new List<SolutionStats>();
  }
  class SolutionStats
  {
    public string FilePath;
    public bool canRoslynOpen;
    public List<string> Diagnostics = new List<string>();
    public bool isUserChoice = false;
  }
}
