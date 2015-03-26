using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.ReviewBot.Github
{
  class RepoInfo
  {
    public string RepoName;
    public readonly List<SolutionInfo> SolutionInfos = new List<SolutionInfo>();
    public string comment;
  }
  class SolutionInfo
  {
    public string FilePath;
    public readonly List<ProjectInfo> Projects = new List<ProjectInfo>();
    public bool canRoslynOpen;
    public bool skipped = false;
    public string error;
  }
  class ProjectInfo
  {
    public string FilePath;
    public bool canMsBuild;
    public bool hasErrors;
    public string error;
  }
}
