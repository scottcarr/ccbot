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
  public static class Helpers
  {
    public static class IO
    {
      public static void CreateDirAndWriteAllText(string path, string text)
      {
        Contract.Requires(!string.IsNullOrEmpty(path));
        Contract.Requires(text != null);

        if (!File.Exists(path))
        {
          var dirName = Path.GetDirectoryName(path);
          if (!Directory.Exists(dirName))
          {
            Directory.CreateDirectory(dirName);
          }
        }

        File.WriteAllText(path, text);
      }
      public static void DumpBuildOutput(string path, string text)
      {
        Contract.Requires(!string.IsNullOrEmpty(path));
        Contract.Requires(text != null);

        Output.WriteLine("Dumping the output of the build in {0}", path);
        CreateDirAndWriteAllText(path, text);
      }      
    }

    public static int CountSuggestions(string rootdir)
    {
      var files = Directory.EnumerateFiles(rootdir, "*.cs", SearchOption.AllDirectories);
      var lines = files.SelectMany(file => File.ReadLines(file));
      return lines.Where(line => line.Contains(Constants.String.Signature.Trim())).Count();
    }

    public static class IEnumerable
    {
      public static IEnumerable<T> EmptyEnumeration<T>()
      {
        Contract.Ensures(Contract.Result<IEnumerable<T>>() != null);

        return new T[0];
      }
    }
  }
}