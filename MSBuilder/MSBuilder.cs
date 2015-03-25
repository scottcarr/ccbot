using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Research.ReviewBot.Utils;
using Microsoft.Research.ReviewBot;

namespace Microsoft.Research.ReviewBot.MSBuilder
{
  class MSBuilder
  {
    public static bool TryBuildProject(string projectPath)
    {
      var p = new Project(projectPath);
      if(!p.Build(new MSBuildLogger(projectPath)))
      {
        var buildOutputFileName = Constants.String.BuildOutputDir(Path.GetFileNameWithoutExtension(projectPath));
        Output.WriteError("Building the solution failed. Check the build output file {0}", buildOutputFileName);
        return false;

      }
      return true;
    }
    internal class MSBuildLogger : Logger
    {
      private readonly string projPath;
      //private readonly List<String> messages = new List<String>();
      private string messages; 
      public MSBuildLogger(string projectPath)
      {
        projPath = projectPath;
      }
      void handleError(object sender, BuildErrorEventArgs e)
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("[ERROR]:");
        Console.WriteLine(e.Message);
        Console.ForegroundColor = ConsoleColor.White;

        messages += e.Message + "\n" ;
      }

      void handleWarning(object sender, BuildWarningEventArgs e)
      {

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("[WARN]:");
        Console.WriteLine(e.Message);
        Console.ForegroundColor = ConsoleColor.White;
      }

      void handleMessage(object sender, BuildMessageEventArgs e)
      {
        //Console.Write("[MSG]:");
        //Console.WriteLine(e.Message);
      }

      void handleProjectStart(object sender, ProjectStartedEventArgs e)
      {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(e.Message);
        Console.ForegroundColor = ConsoleColor.White;
      }

      void handleProjectFinish(object sender, ProjectFinishedEventArgs e)
      {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(e.Message);
        Console.ForegroundColor = ConsoleColor.White;
      }
      void handleTargetStart(object sender, TargetStartedEventArgs e)
      {
        /*
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(e.Message);
        Console.ForegroundColor = ConsoleColor.White;
        */
      }

      public override void Shutdown()
      {
        var buildOutputFileName = Constants.String.BuildOutputDir(Path.GetFileNameWithoutExtension(projPath));
        Helpers.IO.DumpBuildOutput(buildOutputFileName, messages);
      }

      public override void Initialize(IEventSource eventSource)
      {
        eventSource.ProjectStarted += new ProjectStartedEventHandler(handleProjectStart);
        eventSource.TargetStarted += new TargetStartedEventHandler(handleTargetStart);
        eventSource.ErrorRaised += new BuildErrorEventHandler(handleError);
        eventSource.WarningRaised += new BuildWarningEventHandler(handleWarning);
        eventSource.MessageRaised += new BuildMessageEventHandler(handleMessage);
        eventSource.ProjectFinished += new ProjectFinishedEventHandler(handleProjectFinish);
      }
    }
  }

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
}

