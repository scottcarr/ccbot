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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Microsoft.Research.ReviewBot
{
  enum OutputOption { unintialized, git, inplace };
  class Options
  {
    public string ClousotXML { private set; get; }

    public string Project { private set; get;}

    public string SourceFile { private set; get; }

    public string Solution { private set; get; }

    public OutputOption Output { private set; get; }

    public string GitRoot { private set; get; }

    /// <summary>
    /// Parse the args into an Option object
    /// </summary>
    /// <param name="args">the args param to Main</param>
    /// <param name="options">the parsed arguements, valid if return is ture</param>
    /// <param name="why">an error message if parsing fails and the return is false</param>
    /// <returns>true if parsing succeeded, false otherwise</returns>
    internal static bool TryParseOptions(string[] args, out Options options, out string why)
    {
      #region CodeContracts
      Contract.Requires(Contract.ForAll(args, arg => arg != null));
      Contract.Ensures(!Contract.Result<bool>() || Contract.ValueAtReturn(out options) != null);
      Contract.Ensures(!Contract.Result<bool>() || Contract.ValueAtReturn(out options).Project != null);
      Contract.Ensures(!Contract.Result<bool>() || Contract.ValueAtReturn(out options).Solution != null);
      Contract.Ensures(!Contract.Result<bool>() || Contract.ValueAtReturn(out options).ClousotXML != null);
      Contract.Ensures(!Contract.Result<bool>() || Contract.ValueAtReturn(out options).Output != OutputOption.unintialized);
      Contract.Ensures(Contract.Result<bool>() || Contract.ValueAtReturn(out why) != null);
      // there should be a contract for the output option, but it's complicated
      //Contract.Ensures(
      //    !Contract.Result<bool>()
      // || ((Contract.ValueAtReturn(out options).Output == OutputOption.git && Contract.ValueAtReturn(out options).GitRoot != null) 
      //    ^ (Contract.ValueAtReturn(out options).Output == OutputOption.inplace))
      //);
      #endregion CodeContracts

      options = new Options();
      why = null;

      for (var i = 0; i < args.Length; i++)
      {
        var arg = args[i];
        if (IsOption(arg, out arg))
        {
          switch (arg)
          {
            case "project":
              if (i == args.Length - 1)
              {
                why = "The last argument can't be a keyword";
                return false;
              }
              options.Project = args[++i];
              break;

            case "source":
              if (i == args.Length - 1)
              {
                why = "The last argument can't be a keyword";
                return false;
              }
              options.SourceFile = args[++i];
              break;

            case "break":
              System.Diagnostics.Debugger.Launch();
              break;

            case "solution":
              if (i == args.Length - 1)
              {
                why = "The last argument can't be a keyword";
                return false;
              }
              options.Solution = args[++i];
              break;

            case "output":
              if (i == args.Length - 1)
              {
                why = "The last argument can't be a keyword";
                return false;
              }
              OutputOption oo;
              if (Enum.TryParse(args[++i], true, out oo)) 
              {
                options.Output = oo;
              }
              else
              {
                why = "Unrecognized output option: " + args[i];
                return false;
              }
              break;

            case "gitroot":
              if (i == args.Length - 1)
              {
                why = "The last argument can't be a keyword";
                return false;
              }
              options.GitRoot = args[++i];
              if (!Directory.Exists(options.GitRoot))
              {
                why = "git root directory must exist";
                return false;
              }
              break;

            default:
              options = null;
              why = "Unrecognized option " + arg;
              RBLogger.Error("Invalid option {0}", arg);
              return false;
          }
        }
        else
        {
          if (options.ClousotXML != null)
          {
            why = "Cannot express two (or more) .xml files";
            return false;
          }
          else
          {
            options.ClousotXML = args[0];
          }
        }
      }

      return options.CheckRequiredArguments(ref why);
    }

    private bool CheckRequiredArguments(ref string why)
    {
      #region CodeContracts
      Contract.Ensures(!Contract.Result<bool>() || this.ClousotXML != null);
      Contract.Ensures(!Contract.Result<bool>() || this.Project != null);
      Contract.Ensures(!Contract.Result<bool>() || this.Solution != null);
      Contract.Ensures(!Contract.Result<bool>() || this.Output != OutputOption.unintialized);
      // I have to think about this:
      //Contract.Ensures(Contract.Result<bool>() ^ !((this.Output == OutputOption.git && this.GitRoot == null) ^ this.Output == OutputOption.inplace));
      #endregion CodeContracts
      // I'm not positive this is correct:
      //Contract.Ensures(Contract.Result<bool>() == false || (this.Output != OutputOption.git && this.GitRoot != null) || this.Output != OutputOption.inplace);

      // right now, only support all options provided
      var ok = this.ClousotXML != null && this.Project != null && this.Solution != null && this.Output != OutputOption.unintialized;
      if(!ok)
      {
        why = "You need to specify all of: Clousot XML, Project, Solution, output";
        RBLogger.Error(why);
      }
      var ok2 = (this.Output == OutputOption.git && this.GitRoot != null) ^ this.Output == OutputOption.inplace;
      if(!ok2)
      {
        why = "You need to either: (1) give -output git -gitroot <some_path> (2) give -output inplace but not both";
        RBLogger.Error(why);
      }
      return ok & ok2;
    }

    internal static void PrintUsage(string error =null)
    {
      if(error != null)
      {
        RBLogger.Error("Error in parsing the command line: {0}", error);
      }
      RBLogger.Error("USAGE: $ ReviewBot.exe <cccheckoutput.xml> -project <projectfile> -solution <solutionfile> -output [inplace|git -gitroot <gitdirectory>]");
    }

    /// <summary>
    /// If string is a valid option, get the name of the option.  A valid option starts with either "-" or "/"
    /// </summary>
    /// <param name="inStr">The option string. Ex: "-output"</param>
    /// <param name="outStr">The name of the option. Ex: "output"</param>
    /// <returns>true if inStr starts with "-" or "/"</returns>
    private  static bool IsOption(string inStr, out string outStr)
    {
      #region CodeContracts
      Contract.Requires(inStr != null);
      #endregion CodeContracts

      if(inStr.StartsWith("-") || inStr.StartsWith(@"/"))
      {
        outStr = inStr.Substring(1);
        return true;
      }
      else
      {
        outStr = inStr;
        return false;
      }
    }
  }

}
