using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace Github
{
  class KnownRepos
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
    public KnownRepos()
    {
      reposDict = new Dictionary<string, RepoInfo>();
      reposDict.Add("SignalR", new SignalR());
      reposDict.Add("SparkleShare", new SparkleShare());
      reposDict.Add("PushSharp", new PushSharp());
    }
    class SparkleShare : RepoInfo
    {
      public SparkleShare() : base("SparkleShare" , "https://github.com/hbons/SparkleShare.git", "") { }
      public override bool Build(string repoDirectory)
      {
        // if this doesn't work because it can't find msbuild when it runs the script:

        /*
        
        http://stackoverflow.com/questions/6319274/how-do-i-run-msbuild-from-the-command-line-using-windows-sdk-7-1
        Came across this question on google, not sure if anyone still needs an answer here, but i got it working.

        To enable msbuild in Command Prompt, you simply have to add the path to the .net4 framework install on your machine to the PATH environment variable.

        You can access the environment variables by right clicking on 'Computer', click 'properties' and click 'Advanced system settings' on the left navigation bar. 
        On the next dialog bog click 'Environment variables,' scroll down to 'PATH' and edit it to include your path to the framework (don't forget a ';' after the last entry in here.

        For reference my path was C:\Windows\Microsoft.NET\Framework\v4.0.30319.

        */
        Environment.SetEnvironmentVariable("EnableNuGetPackageRestore", "true");
        var buildCmd = Path.Combine(repoDirectory, "SparkleShare", "Windows", "build.cmd");
        Process p = new Process();
        p.StartInfo.FileName = buildCmd;
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.WorkingDirectory = repoDirectory;
        p.Start();
        p.WaitForExit();
        return p.ExitCode == 0;
      }
    }
    class SignalR : RepoInfo
    {
      public SignalR() : base("SignalR" , "https://github.com/SignalR/SignalR.git", "") { }
      public override bool Build(string repoDirectory)
      {
        // if this doesn't work because it can't find msbuild when it runs the script:

        /*
        
        http://stackoverflow.com/questions/6319274/how-do-i-run-msbuild-from-the-command-line-using-windows-sdk-7-1
        Came across this question on google, not sure if anyone still needs an answer here, but i got it working.

        To enable msbuild in Command Prompt, you simply have to add the path to the .net4 framework install on your machine to the PATH environment variable.

        You can access the environment variables by right clicking on 'Computer', click 'properties' and click 'Advanced system settings' on the left navigation bar. 
        On the next dialog bog click 'Environment variables,' scroll down to 'PATH' and edit it to include your path to the framework (don't forget a ';' after the last entry in here.

        For reference my path was C:\Windows\Microsoft.NET\Framework\v4.0.30319.

        */
	// you may also need to install: Windows 8 SDK, Silverlight 4 SDK, Windows Phone SDK
	// why can't it find the Microsoft.Web.Administration assembly?
	// it seemed to help to add /p:VisualStudioVersion=14.0 at the end of build.cmd
        Environment.SetEnvironmentVariable("EnableNuGetPackageRestore", "true");
        var buildCmd = Path.Combine(repoDirectory, "build.cmd");
        Process p = new Process();
        p.StartInfo.FileName = buildCmd;
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.WorkingDirectory = repoDirectory;
        p.Start();
        p.WaitForExit();
        return p.ExitCode == 0;
      }
    }
    class PushSharp : RepoInfo
    {
      public PushSharp() : base("PushSharp" , "https://github.com/Redth/PushSharp.git", "") { }
      public override bool Build(string repoDirectory)
      {
        // if this doesn't work because it can't find msbuild when it runs the script:

        /*
        
        http://stackoverflow.com/questions/6319274/how-do-i-run-msbuild-from-the-command-line-using-windows-sdk-7-1
        Came across this question on google, not sure if anyone still needs an answer here, but i got it working.

        To enable msbuild in Command Prompt, you simply have to add the path to the .net4 framework install on your machine to the PATH environment variable.

        You can access the environment variables by right clicking on 'Computer', click 'properties' and click 'Advanced system settings' on the left navigation bar. 
        On the next dialog bog click 'Environment variables,' scroll down to 'PATH' and edit it to include your path to the framework (don't forget a ';' after the last entry in here.

        For reference my path was C:\Windows\Microsoft.NET\Framework\v4.0.30319.

        */
        Environment.SetEnvironmentVariable("EnableNuGetPackageRestore", "true");
        Process p = new Process();
        p.StartInfo.FileName = @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe";
        p.StartInfo.Arguments = "PushSharp.sln";
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.WorkingDirectory = repoDirectory;
        p.Start();
        p.WaitForExit();
        return p.ExitCode == 0;
      }
    }
  }
}
