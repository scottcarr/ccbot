using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Directory;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis;

namespace Github
{
  public static class SolutionTools
  {
    public static string[] ScanForSolutions(string repoPath)
    {
      return GetFiles(repoPath, "*.sln", System.IO.SearchOption.AllDirectories);
    }
    public static Solution 	HeuristicallyDetermineBestSolution(string[] solutionPaths)
    {
      var mbsw = MSBuildWorkspace.Create();
      var slns = new List<Solution>();
      foreach (var slnPath in solutionPaths)
      {
        try
        {
         slns.Add(mbsw.OpenSolutionAsync(slnPath).Result);
        } catch (Exception e)
        {
          Console.WriteLine("something went wrong opening: {0}", slnPath);
          //Console.WriteLine(e);
        }
      }
      if (slns.Any())
      {
        var maxPrjs = slns.Max(s => s.Projects.Count());
        var winner = slns.First(s => s.Projects.Count() == maxPrjs);
        return winner;
      } else { return null; }
    }
  }
}
