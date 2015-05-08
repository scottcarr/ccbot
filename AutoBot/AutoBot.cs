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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Research.ReviewBot.Utils;
using Microsoft.Build.Framework;

/* The goal of this project is to make running ReviewBot easier
 * There's too much manual configuration in the old way.
 */
namespace Microsoft.Research.ReviewBot
{
  class AutoBot
  {
    static void AutoSetupOnce(string solutionPath, string projectPath, string gitBaseBranch="master")
    {

      using (var logger = new UberLogger("scriptcs", LoggerVerbosity.Detailed))
      {
        logger.Message("Solution: " + solutionPath);
        logger.Message("Project: " + projectPath);
        Configuration conf;
        if (!AutoConfig.TryAutoConfig(solutionPath, projectPath, true, out conf, logger, gitBaseBranch))
        {
          logger.Error("Could not autoconfig");
          return;
        }

        /*
        var contractsProps = Helpers.EnableCodeContractsInProject(conf.Project);
        if (!AutoConfig.TrySelectFilesWithExtension("cccheck.rsp", Path.GetDirectoryName(conf.Project), out conf.RSP))
        {
          Console.WriteLine("Couldn't find a *.rsp file.  Will try to create one");
          if (!MSBuilder.TryBuildProject(conf.Project, logger))
          {
            Console.WriteLine("Couldn't build project.");
          }
          if (!AutoConfig.TrySelectFilesWithExtension("cccheck.rsp", Path.GetDirectoryName(conf.Project), out conf.RSP))
          {
            Console.WriteLine("Couldn't find rsp after enabling code contracts and building.");
          }
        }

        repo.Add(conf.RSP);
        repo.Add(conf.Project);
        repo.Add(contractsProps);
        repo.Commit("setup for reviewbot");

        if (!MSBuilder.TryBuildProject(projPath, logger))
        {
          logger.Error("Could not build");
        }
        */
      }

    }
    static void AutoAnnotate(string solutionPath, string projectPath, bool fromScratch, string gitBaseBranch="master")
    {
      // TODO how is autconfig setting clousotxml?
      using (var logger = new UberLogger("scriptcs", LoggerVerbosity.Normal))
      {
        //var projPath = @"C:\Users\carr27\Documents\GitHub\scriptcs\src\ScriptCs.Core\ScriptCs.Core.csproj";
        //var slnPath = @"C:\Users\carr27\Documents\GitHub\scriptcs\ScriptCs.sln";
        logger.Message("Solution: " + solutionPath);
        logger.Message("Project: " + projectPath);
        Configuration conf;
        if (!AutoConfig.TryAutoConfig(solutionPath, projectPath, false, out conf, logger, gitBaseBranch))
        {
          logger.Error("Could not autoconfig");
          return;
        }

        if (fromScratch)
        {
          var repo = new GitRepo(conf);
          repo.HardReset("reviewbot");

          /*
          if (!MSBuilder.TryBuildProject(conf.Project, logger))
          {
            logger.Error("Failed to build");
            return;

          }
          */

          // run clousot
          if (!ExternalCommands.TryRunClousot(conf.CccheckXml, conf.Cccheck, conf.CccheckOptions, conf.RSP))
          {
            logger.Error("Failed to run cccheck");
            return;
          }

        }

        Annotator.DoAnnotate(conf);

      }

    }
    static void Main(string[] args)
    {
      //var projPath = @"C:\Users\carr27\Documents\GitHub\scriptcs\src\ScriptCs.Core\ScriptCs.Core.csproj";
      //var slnPath = @"C:\Users\carr27\Documents\GitHub\scriptcs\ScriptCs.sln";
      //var projPath = @"C:\Users\carr27\Documents\GitHub\Nancy\src\Nancy\Nancy.csproj";
      //var slnPath = @"C:\Users\carr27\Documents\GitHub\Nancy\src\Nancy.sln";
      //var projPath = @"C:\Users\carr27\Documents\GitHub\Ninject\src\Ninject\Ninject.csproj";
      //var slnPath = @"C:\Users\carr27\Documents\GitHub\Ninject\Ninject.sln";

      // don't work:
      //var projPath = @"C:\Users\carr27\Documents\GitHub\roslyn\src\Compilers\Core\Desktop\CodeAnalysis.Desktop.csproj";
      //var projPath = @"C:\Users\carr27\Documents\GitHub\roslyn\src\Compilers\Core\Portabjle\CodeAnalysis.csproj";
      //var projPath = @"C:\Users\carr27\Documents\GitHub\roslyn\src\Scripting\Core\Scripting.csproj";
      //var projPath = @"C:\Users\carr27\Documents\GitHub\roslyn\src\Workspaces\Core\Portable\Workspaces.csproj";
      //var projPath = @"C:\Users\carr27\Documents\GitHub\roslyn\src\Tools\Source\CompilerGeneratorTools\Source\CSharpErrorFactsGenerator\CSharpErrorFactsGenerator.csproj";
      //var slnPath = @"C:\Users\carr27\Documents\GitHub\roslyn\src\RoslynLight.sln";
      //var projPath = @"C:\Users\carr27\Documents\GitHub\netmq\src\NetMQ\NetMQ.csproj";
      //var slnPath = @"C:\Users\carr27\Documents\GitHub\netmq\src\NetMQ.sln";
      //var projPath = @"C:\Users\carr27\Documents\GitHub\Newtonsoft.Json\Newtonsoft.Json\Src\Newtonsoft.Json\Newtonsoft.Json.csproj";
      //var slnPath = @"C:\Users\carr27\Documents\GitHub\Newtonsoft.Json\Newtonsoft.Json\Src\Newtonsoft.Json.sln";

      // this entire project is one function:
      //var projPath = @"C:\Users\carr27\Documents\GitHub\roslyn\src\Compilers\CSharp\csc2\csc2.csproj";

      // they don't do param validation
      //var projPath = @"C:\Users\carr27\Documents\GitHub\choco\src\chocolatey\chocolatey.csproj";
      //var slnPath = @"C:\Users\carr27\Documents\GitHub\choco\src\chocolatey.sln";

      // this works if I prebuild the project
      //var projPath = @"C:\Users\carr27\Documents\GitHub\mongo-csharp-driver\src\MongoDB.Driver.Core\MongoDB.Driver.Core.csproj";
      //var slnPath = @"C:\Users\carr27\Documents\GitHub\mongo-csharp-driver\src\CSharpDriver.sln";

      // they don't seem to do param validation
      //var projPath = @"C:\Users\carr27\Documents\GitHub\ServiceStack.Redis\src\ServiceStack.Redis\ServiceStack.Redis.csproj";
      //var slnPath = @"C:\Users\carr27\Documents\GitHub\ServiceStack.Redis\src\ServiceStack.Redis.sln";
      //var projPath = @"C:\Users\carr27\Documents\GitHub\SignalR\src\Microsoft.AspNet.SignalR.Core\Microsoft.AspNet.SignalR.Core.csproj";
      //var slnPath = @"C:\Users\carr27\Documents\GitHub\SignalR\Microsoft.AspNet.SignalR.sln";
      //var projPath = @"C:\Users\carr27\Documents\GitHub\SignalR\src\Microsoft.AspNet.SignalR.Core\Microsoft.AspNet.SignalR.Core.csproj";
      //var slnPath = @"C:\Users\carr27\Documents\GitHub\SignalR\Microsoft.AspNet.SignalR.sln";
      //var projPath = @"C:\Users\carr27\Documents\GitHub\ravendb\Raven.Smuggler\Raven.Smuggler.csproj";
      //var slnPath = @"C:\Users\carr27\Documents\GitHub\ravendb\RavenDB.sln";

      // build issues
      //var projPath = @"C:\Users\carr27\Documents\GitHub\EntityFramework\src\EntityFramework.Core\EntityFramework.Core.csproj";
      //var slnPath = @"C:\Users\carr27\Documents\GitHub\EntityFramework\EntityFramework.sln";

      //var projPath = @"C:\Users\carr27\Documents\GitHub\DotNetOpenAuth\src\DotNetOpenAuth.OAuth\DotNetOpenAuth.OAuth.csproj";
      //var projPath = @"C:\Users\carr27\Documents\GitHub\DotNetOpenAuth\src\DotNetOpenAuth.OAuth2\DotNetOpenAuth.OAuth2.csproj";
      //var slnPath = @"C:\Users\carr27\Documents\GitHub\DotNetOpenAuth\src\DotNetOpenAuth.sln";

      //var projPath = @"C:\Users\carr27\Documents\GitHub\msbuild\src\Utilities\Microsoft.Build.Utilities.csproj";
      //var slnPath = @"C:\Users\carr27\Documents\GitHub\msbuild\src\MSBuild.sln";

      /*
      var projPath = @"C:\Users\carr27\Documents\GitHub\ravendb\Raven.Database\Raven.Database.csproj";
      var slnPath = @"C:\Users\carr27\Documents\GitHub\ravendb\RavenDB.sln";

      AutoSetupOnce(slnPath, projPath);
      AutoAnnotate(slnPath, projPath, true, "reviewbot");
      */
      //var commentsFile = @"C:\Users\carr27\AppData\Local\Temp\ReviewBot\ScriptCs_ScriptCs.Core_cccheck2015-4-6-12-18-53.xml";
      //var commentsFile = @"C:\Users\carr27\AppData\Local\Temp\ReviewBot\Ninject_Ninject_cccheck2015-4-14-16-31-51.xml";
      //var commentsFile = @"C:\Users\carr27\AppData\Local\Temp\ReviewBot\CSharpDriver_MongoDB.Driver.Core_cccheck2015-4-8-13-39-49.xml";
      var commentsFile = @"C:\Users\carr27\AppData\Local\Temp\ReviewBot\Nancy_Nancy_cccheck2015-4-14-13-17-39.xml";
      Console.WriteLine(HelpersForClousotXML.GetChecks(commentsFile).Count());
      Console.WriteLine("done.");
      Console.ReadKey();
    }
  }
}
