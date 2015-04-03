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
    static void AutoSetupOnce(string solutionPath, string projectPath)
    {

      using (var logger = new UberLogger("scriptcs", LoggerVerbosity.Normal))
      {
        logger.Message("Solution: " + solutionPath);
        logger.Message("Project: " + projectPath);
        Configuration conf;
        if (!AutoConfig.TryAutoConfig(solutionPath, projectPath, true, out conf, logger))
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
    static void AutoAnnotate(string solutionPath, string projectPath, bool fromScratch)
    {
      // TODO how is autconfig setting clousotxml?
      using (var logger = new UberLogger("scriptcs", LoggerVerbosity.Normal))
      {
        var projPath = @"C:\Users\carr27\Documents\GitHub\scriptcs\src\ScriptCs.Core\ScriptCs.Core.csproj";
        var slnPath = @"C:\Users\carr27\Documents\GitHub\scriptcs\ScriptCs.sln";
        logger.Message("Solution: " + slnPath);
        logger.Message("Project: " + projPath);
        Configuration conf;
        if (!AutoConfig.TryAutoConfig(slnPath, projPath, false, out conf, logger))
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

        //Annotator.DoAnnotate(conf);

      }

    }
    static void Main(string[] args)
    {
      var projPath = @"C:\Users\carr27\Documents\GitHub\scriptcs\src\ScriptCs.Core\ScriptCs.Core.csproj";
      var slnPath = @"C:\Users\carr27\Documents\GitHub\scriptcs\ScriptCs.sln";
      //AutoSetupOnce();
      AutoAnnotate(slnPath, projPath, true);
      Console.WriteLine("done.");
      Console.ReadKey();
    }
  }
}
