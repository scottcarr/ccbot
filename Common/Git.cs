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
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.ReviewBot.Utils
{
  public static class Git
  {
    public static void GitAdd(string gitroot, string gitExePath)
    {
      Contract.Requires(!string.IsNullOrEmpty(gitroot));
      Contract.Requires(gitExePath != null);
      Contract.Requires(gitroot.Length < 260);

      RunGit(gitroot, "add -u .", gitExePath);
    }
    public static void RevertToOriginal(string gitroot, string GitBaseBranch, string gitExePath)
    {
      Contract.Requires(!string.IsNullOrEmpty(gitroot));
      Contract.Requires(gitExePath != null);
      Contract.Requires(gitroot.Length < 260);

      Output.WritePhase("Reverting to the original git branch {0}", GitBaseBranch);

      RunGit(gitroot, String.Format("checkout {0}", GitBaseBranch), gitExePath);
      RunGit(gitroot, String.Format("reset --hard {0}", GitBaseBranch), gitExePath);
    }

    public static void CheckoutBranch(string gitroot, string gitexe, string branch)
    {
      Contract.Requires(gitexe != null);
      Contract.Requires(!string.IsNullOrEmpty(gitroot));
      Contract.Requires(gitroot.Length < 260);

      RunGit(gitroot, "checkout " + branch, gitexe);
    }
    public static void RevertToBranch(string gitroot, string gitexe, string branch)
    {
      Contract.Requires(gitexe != null);
      Contract.Requires(!string.IsNullOrEmpty(gitroot));
      Contract.Requires(gitroot.Length < 260);

      CheckoutBranch(gitroot, gitexe, branch);
      RunGit(gitroot, "reset --hard " + branch, gitexe);
    }

    private static void RunGit(string gitroot, string args, string gitExePath)
    {
      if(!Directory.Exists(gitroot))
      {
        Output.WriteErrorAndQuit("The directory {0} does not exists", gitroot);
      }

      Directory.SetCurrentDirectory(gitroot);

      try
      {
        var p = new Process();
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.FileName = gitExePath;
        p.StartInfo.Arguments = args;
        p.Start();
        p.WaitForExit();
        if (p.ExitCode != 0)
        {
          Output.WriteErrorAndQuit("Git exited with an error {0}", p.ExitCode.ToString());
        }
      }
      catch(Exception e)
      {
        Output.WriteErrorAndQuit("Error while running Git. This is the exception message {0}", e.Message);
      }

    }

  }
}
