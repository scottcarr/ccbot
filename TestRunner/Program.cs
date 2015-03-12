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
using Microsoft.Research.ReviewBot;
using Microsoft.Research.ReviewBot.Utils;

namespace Microsoft.Research.ReviewBot.TestRunner
{
  class Program
  {
    static void Main(string[] args)
    {

      // This assumes you didn't move the exe from the directory where VS puts it
      var cwd = Directory.GetCurrentDirectory();
      var slnDir = Directory.GetParent(Directory.GetParent(Directory.GetParent(cwd).FullName).FullName);
      var sln = Path.Combine(slnDir.FullName, "ReviewBot.sln"); 
      var testDir = Path.Combine(slnDir.FullName, "Tests");
      var tmpDir = Path.Combine(slnDir.FullName, "tmp");
      Console.WriteLine(testDir);
      var projs = Directory.GetFiles(testDir, @"*.csproj", SearchOption.AllDirectories);
      var MSBuild = @"C:\Program Files (x86)\MSBuild\12.0\Bin\amd64\MSBuild.exe";
      var Cccheck = @"C:\Program Files (x86)\Microsoft\Contracts\Bin\cccheck.exe";
      var CccheckOptions = @" -xml -remote=false -suggest objectinvariants -suggest necessaryensures  -suggest readonlyfields -suggest assumes -suggest nonnullreturn -sortWarns=false -warninglevel full";
      foreach (var p in projs)
      {
        var projDir = Directory.GetParent(p);
        var pName = Path.GetFileNameWithoutExtension(p);
        var ccCheckXml = Path.Combine(tmpDir, pName + "_ccCheck.xml");

        var rsp = Directory.GetFiles(projDir.FullName, @"*cccheck.rsp", SearchOption.AllDirectories).First();
        Console.WriteLine(p);

        // run clousot
        /*
        if (!ExternalCommands.TryBuildSolution(sln, MSBuild))
        {
          Output.WriteErrorAndQuit("Couldn't build the solution");
        }
        */

        if (!ExternalCommands.TryRunClousot(ccCheckXml, Cccheck, CccheckOptions, rsp))
        {
          Output.WriteErrorAndQuit("Cant' run Clousot");
        } 

        // run reviewbot
        var reviewArgs = new string[] {
                                ccCheckXml, 
                                "-project", p, 
                                "-solution", sln, 
                                "-output", "inplace"
                              };
        if (Annotator.DoAnnotate(reviewArgs) != 0)
        {
          Console.WriteLine("Annotating failed.");
        }
      }
      Console.WriteLine("done.");
      Console.ReadKey();
    }
  }
}
