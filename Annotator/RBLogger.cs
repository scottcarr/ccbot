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
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;

namespace Microsoft.Research.ReviewBot
{
  class ErrorInfo
  {
    public readonly string FilePath;
    public readonly string Method;
    public readonly int LineNumber;
    public ErrorInfo(string path, string method, int line)
    {
      #region CodeContracts
      Contract.Requires(path != null);
      Contract.Requires(method != null);
      Contract.Ensures(path == this.FilePath);
      Contract.Ensures(method == this.Method);
      Contract.Ensures(line == this.LineNumber);
      #endregion CodeContracts

      this.FilePath = path;
      this.Method = method;
      this.LineNumber = line;
    }
    public override string ToString()
    {
      return String.Format("[ERROR @ {1}:{2}:{0}] ", LineNumber, FilePath, Method);
    }
  }
  class RBLogger
  {
    static readonly string[] FilteredFiles = { "RBParser.cs", "CommentHelpers.cs", "RBSearcher.cs" };
    private static bool isStarted = false;
    private static int frame_level = 2;
    public static void StartLogging()
    {
      #region CodeContracts
      Contract.Ensures(RBLogger.isStarted == true);
      #endregion CodeContracts

      var logfile = System.IO.File.CreateText("log.txt");
      Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
      Debug.Listeners.Add(new TextWriterTraceListener(logfile));
      Debug.AutoFlush = true;
      isStarted = true;
    }
    private static ErrorInfo GetErrorInfo()
    {
      #region CodeContracts
      Contract.Ensures(Contract.Result<ErrorInfo>() != null);
      Contract.Ensures(Contract.Result<ErrorInfo>().Method != null);
      Contract.Ensures(Contract.Result<ErrorInfo>().FilePath != null);
      #endregion CodeContracts

      var st = new StackTrace(true);
      var frames = st.GetFrames();
      Contract.Assert(frame_level < frames.Length);
      Contract.Assert(0 <= frame_level);
      var caller = frames[frame_level];
      Contract.Assert(caller != null);
      var fp = caller.GetFileName();
      var linenum = caller.GetFileLineNumber();
      var method = caller.GetMethod();
      Contract.Assert(method != null);
      return new ErrorInfo(Path.GetFileName(fp), method.ToString(), linenum);
    }
    public static void Error(object arg0, params object[] args)
    {
      #region CodeContracts
      Contract.Ensures(isStarted);
      #endregion CodeContracts

      var prevColor = System.Console.ForegroundColor;
      if (!isStarted) { StartLogging(); }
      if (IsFilteredFile(GetErrorInfo())) { return; }
      try
      {
        System.Console.ForegroundColor = ConsoleColor.Red;
        Debug.WriteLine(GetErrorInfo() + String.Format((String) arg0, args));
      }
      catch (Exception)
      {
        Debug.WriteLine(GetErrorInfo().ToString() + arg0 + args);
      }
      finally
      {
        System.Console.ForegroundColor = prevColor;
      }
    }
    public static void Info(object arg0, params object[] args)
    {
      #region CodeContracts
      Contract.Ensures(isStarted);
      #endregion CodeContracts

      if (!isStarted) { StartLogging(); }
      if (IsFilteredFile(GetErrorInfo())) { return; }
      try 
      { 
        // maybe we got a format string and arguments
        Debug.WriteLine(String.Format((String) arg0, args));
      }
      catch
      {
        Debug.WriteLine(arg0);
      } 
    }
    public static void ErrorIf(bool condition, String format, params object[] args)
    {
      #region CodeContracts
      Contract.Requires(format != null);
      #endregion CodeContracts

      if (condition) { frame_level = 3; Error(format, args); frame_level = 2; }
    }
    public static void Indent()
    {
      Debug.Indent();
    }
    public static void Unindent()
    {
      Debug.Unindent();
    }
    private static bool IsFilteredFile(ErrorInfo error)
    {
      if (FilteredFiles.Any(x => error.FilePath.Equals(x, StringComparison.OrdinalIgnoreCase)))
      {
        return true;
      }
      return false;
    }
  }
}
