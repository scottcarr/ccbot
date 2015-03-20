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
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.ReviewBot.Utils
{

  /// <summary>
  /// Common constants used by ReviewBot
  /// </summary>
  public static class Constants
  {
    public static class Numerical
    {
      public const int NumberOfIterations = 1;
    }

    public static class String
    {
      public const string ToolName = "ReviewBot";
      public const string Signature = " // Suggested By ReviewBot \r\n";

      public const string CannotLoadAssembly = "Cannot load assembly";

      public const string SharedMSRPublicDir = @"\\msr\public\logozzo\ReviewBot";
      public const string ExtensionDestinationPath = SharedMSRPublicDir + @"\workspace\Share\ReviewBotStaticAnalysisProvider.zip"; // was  "\\\\Z3476638\\Users\\t-scottc\\workspace\\Share\\ReviewBotStaticAnalysisProvider.zip";

      public const string CodeFlowProjectConfigPath = @"\\CodeFlow\Public\cfproj.cmd";
      public const string CodeFlowClientPath = @"\\CodeFlow\Public\cf.cmd"; // To run the client

      private const string CommentsSubDir = @"\Comments";
      private const string SharedMSRCommentsDir = SharedMSRPublicDir + CommentsSubDir;

      // where the default configuration lives
      public const string ConfigDefault = @"..\..\ConfigDefault.xml";
      public static string PathForDefaultFile
      {
        get
        {
          return TempDir + @"\ConfigDefault.xml";
        }
      }

      public static string PathForCommentsDirectory
      {
        get
        {
          if (Directory.Exists(SharedMSRCommentsDir))
          {
            return SharedMSRCommentsDir;
          }
          else
          {
            return TempDir + CommentsSubDir;
          }
        }
      }

      public static string BuildOutputDir(string buildName)
      {
        Contract.Ensures(!string.IsNullOrEmpty(Contract.Result<string>()));

        var now = DateTime.Now;
        var when = string.Format("{0}-{1}-{2}-{3}-{4}-{5}", now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
        return string.Format(@"{0}\\{1}\\{2}", TempDir, buildName, when);
      }

      private static string TempDir
      {
        get
        {
          // Contract.Ensures(Directory.Exists(Contract.Result<string>()));

          var res = System.Environment.GetEnvironmentVariable("TEMP");
          if (!string.IsNullOrEmpty(res) && Directory.Exists(res))
          {
            return ReviewBotTemp(res);
          }
          res = System.Environment.GetEnvironmentVariable("TMP");
          if (!string.IsNullOrEmpty(res) && Directory.Exists(res))
          {
            return ReviewBotTemp(res);
          }

          Output.WriteErrorAndQuit(@"Can't find a %TEMP% dir");
          // Unreachable
          throw new Exception("Should be unreached");
        }
      }

      private static string ReviewBotTemp(string dir)
      {
        var reviewBotTempDirName = string.Format(@"{0}\{1}", dir, ToolName);
        Contract.Assume(!string.IsNullOrEmpty(reviewBotTempDirName));

        if (!Directory.Exists(reviewBotTempDirName))
        {
          Directory.CreateDirectory(reviewBotTempDirName);
        }

        return reviewBotTempDirName;

      }
    }

    public static class HardCodedToBeDeleted
    {
      // Extension string constants -- You do not need to change these unless you're changing the CodeFlow Extension
      public const string ExtensionProjectPath = @"C:\cci\Microsoft.Research\CCTools\ReviewBotStaticAnalysisProvider\ReviewBotStaticAnalysisProvider.csproj";
      public const string ExtensionTargetDir = @"C:\cci\Microsoft.Research\CCTools\ReviewBotStaticAnalysisProvider\bin\Debug";
      public const string zipExePath = @"C:\Program Files\7-Zip\7z.exe";
    }

  }
}
