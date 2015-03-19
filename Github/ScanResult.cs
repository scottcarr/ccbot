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
    public readonly List<SolutionStats> SolutionResults = new List<SolutionStats>();
    public bool skipped = false;
    public string comment;
  }
  class SolutionStats
  {
    public string FilePath;
    public bool canMsBuild;
    public readonly List<ProjectStats> Projects = new List<ProjectStats>();
    public bool canRoslynOpen;
  }
  class ProjectStats
  {
    public string FilePath;
    public List<string> Diagnostics = new List<string>();
    public bool canRoslynOpen;
  }
}
