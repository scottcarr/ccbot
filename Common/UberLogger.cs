using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.IO;

namespace Microsoft.Research.ReviewBot.Utils
{
  public class UberLogger : IDisposable
  {
    private StreamWriter logFile;
    private string logPath;
    private readonly LoggerVerbosity _verbosity;
    public UberLogger(string runName, LoggerVerbosity verbosity)
    {
      _verbosity = verbosity;
      logPath = Constants.String.BuildOutputDir(runName);
      logFile = new StreamWriter(logPath);
      if (!File.Exists(logPath))
      {
        var dirName = Path.GetDirectoryName(logPath);
        if (!Directory.Exists(dirName))
        {
          Directory.CreateDirectory(dirName);
        }
      }
    }
    public void Dispose()
    {
      Console.WriteLine("Exiting.  See {0} for log.", logPath);
      logFile.Flush();
      logFile.Close();
    }
    public void Error(string format, params string[] p)
    {
      DoLog(ConsoleColor.Red, "[ERROR]: " + format, p);
    }
    public void Warn(string format, params string[] p)
    {
      DoLog(ConsoleColor.Yellow, "[Warn]: " + format, p);
    }
    public void Message(string format, params string[] p)
    {
      if (_verbosity == LoggerVerbosity.Detailed)
      {
        DoLog(ConsoleColor.Cyan, "[MSG]: " + format, p);
      }
    }

    public void DoLog(ConsoleColor color, string format, params string[] p)
    {
      var msg = String.Format(format, p);
      Console.ForegroundColor = color;
      Console.WriteLine(msg);
      logFile.WriteLine(msg);
    }
  }
  class MSBuildLogger : Logger
  {
    private readonly string projPath;
    //private readonly List<String> messages = new List<String>();
    private readonly UberLogger _uberloger;

    public MSBuildLogger(string projectPath, UberLogger uberlogger)
    {
      projPath = projectPath;
      _uberloger = uberlogger;
    }
    void handleError(object sender, BuildErrorEventArgs e)
    {
      _uberloger.Error(e.Message);
    }

    void handleWarning(object sender, BuildWarningEventArgs e)
    {
      _uberloger.Warn(e.Message);
    }

    void handleMessage(object sender, BuildMessageEventArgs e)
    {
      //Console.Write("[MSG]:");
      //Console.WriteLine(e.Message);
    }

    void handleProjectStart(object sender, ProjectStartedEventArgs e)
    {
      /*
      Console.ForegroundColor = ConsoleColor.Cyan;
      Console.WriteLine(e.Message);
      Console.ForegroundColor = ConsoleColor.White;
      */
    }

    void handleProjectFinish(object sender, ProjectFinishedEventArgs e)
    {
      _uberloger.Message(e.Message);
    }
    void handleTargetStart(object sender, TargetStartedEventArgs e)
    {
      _uberloger.Message(e.Message);
    }

    public override void Shutdown()
    {
      /*
      var buildOutputFileName = Constants.String.BuildOutputDir(Path.GetFileNameWithoutExtension(projPath));
      Helpers.IO.DumpBuildOutput(buildOutputFileName, messages);
      */
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
