using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using Microsoft.Build.Evaluation;
using Microsoft.Research.ReviewBot.Utils;

namespace Microsoft.Research.ReviewBot
{
  public static class MSBuilder
  {
    public static bool TryBuildProject(string projectPath, UberLogger log)
    {
      var p = new Project(projectPath);
      if(!p.Build(new MSBuildLogger(projectPath, log)))
      {
        /*
        var buildOutputFileName = Constants.String.BuildOutputDir(Path.GetFileNameWithoutExtension(projectPath));
        Output.WriteError("Building the solution failed. Check the build output file {0}", buildOutputFileName);
        return false;
        */
      }
      return true;
    }
  }

  /*
  class Program
  {
    static void Main(string[] args)
    {
      var projPath = @"C:\Users\carr27\Documents\GitHub\reviewbot\Github\bin\Debug\Nancy\src\Nancy\Nancy.csproj";
      MSBuilder.TryBuildProject(projPath);
      //var p = new Project(@"C:\Users\carr27\Documents\GitHub\roslyn\BuildAndTest.proj");
      //p.Build(new MSBuildLogger());
      Console.WriteLine("done.");
      Console.ReadKey();
    }
  }
  */
}

