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
using Microsoft.Research.ReviewBot.Constants;

namespace Microsoft.Research.ReviewBot.Logging
{

  /// <summary>
  /// Console Output
  /// </summary>
  public static class Output
  {
    public enum ToolAction { PrintUsage, CreateDefaultFile, Run };

    const int INDENT_NO = 0;
    const int INDENT_STANDARD = 2;

    static private int phasecount = 0;

    static public void WritePhase(string format, params string[] p)
    {
      Contract.Requires(format != null);
      Contract.Requires(p != null);

      var phase = string.Format(format, p);

      Console.Title = string.Format("[{0}] {1}", Constants.String.ToolName, phase);

      Output.WriteLine(ConsoleColor.Cyan, string.Format("Phase {0}", phasecount++), phase);
    }

    static public void WriteError(string format, params string[] p)
    {
      Contract.Requires(format != null);
      Contract.Requires(p != null);

      WriteLine(ConsoleColor.Red, "Error", format, p);
    }

    static public void WriteErrorAndQuit(string format, params string[] p)
    {
      Contract.Requires(format != null);
      Contract.Requires(p != null);
      Contract.Ensures(false); // Never returns

      WriteError("Fatal Error. Aborting.");
      WriteError(format, p);

      if (System.Diagnostics.Debugger.IsAttached)
      {
        Output.WriteLine(true, 0, "Press any key to quit");
        Console.ReadKey();
      }

      System.Environment.Exit(-1);
    }

    static public void WriteWarning(string format, params string[] p)
    {
      Contract.Requires(format != null);
      Contract.Requires(p != null);

      WriteLine(ConsoleColor.Yellow, "Warning", format, p);
    }

    static public void WriteLine(string format, params string[] p)
    {
      Contract.Requires(format != null);
      Contract.Requires(p != null);

      Output.WriteLine(true, INDENT_STANDARD, format, p);
    }

    static public void WriteLine(bool addTime, string format, params string[] p)
    {
      Contract.Requires(format != null);
      Contract.Requires(p != null);

      Output.WriteLine(addTime, INDENT_STANDARD, format, p);
    }

    static private void WriteLine(bool addTime, int indent, string format, params string[] p)
    {
      Contract.Requires(format != null);
      Contract.Requires(p != null);

      for (var i = 0; i < indent; i++)
      {
        Console.Write(' ');
      }

      if (addTime)
      {
        Console.WriteLine("[{0}] {1}", DateTime.Now.TimeOfDay, string.Format(format, p));
      }
      else
      {
        Console.WriteLine("{0}", string.Format(format, p));
      }
    }

    static private void WriteLine(ConsoleColor color, string what, string format, params string[] p)
    {
      Contract.Requires(what != null);

      var oldColor = Console.ForegroundColor;
      Console.ForegroundColor = color;
      Output.WriteLine(true, INDENT_NO, "[{0}] {1}", what, string.Format(format, p));
      Console.ForegroundColor = oldColor;
    }
  }
}
