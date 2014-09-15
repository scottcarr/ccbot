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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics.Contracts;

namespace Microsoft.Research.ReviewBot
{
  public class XmlDoc
  {
    public static bool TryReadXml(string filename, out CCCheckOutput data)
    {
      #region CodeContracts
      Contract.Requires(!string.IsNullOrEmpty(filename));
      Contract.Ensures(!Contract.Result<bool>() || Contract.ValueAtReturn(out data) != null);
      #endregion CodeContracts

      Contract.Assume(File.Exists(filename));
      data = null;
      try
      {
        var text = File.ReadAllText(filename);
        var serializer = new XmlSerializer(typeof(CCCheckOutput));

        using(var reader = new StringReader(text))
        {
          data = (CCCheckOutput)serializer.Deserialize(reader);
        }
      }
      catch(Exception e)
      {
        Console.WriteLine("Something went wrong in opening the input file");
        Console.WriteLine("This is the exception {0}", e.ToString());

        return false;
      }

      return true;
    }
  }
}
