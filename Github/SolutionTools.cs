/*
  ReviewBot 0.1
  Copyright (c) Microsoft Corporation
  All rights reserved. 
  
  MIT License
  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the ""Software""), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
  The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
  THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis;

namespace Microsoft.Research.ReviewBot.Github
{
  public static class SolutionTools
  {
    public static string[] ScanForSolutions(string repoPath)
    {
      return Directory.GetFiles(repoPath, "*.sln", System.IO.SearchOption.AllDirectories);
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
