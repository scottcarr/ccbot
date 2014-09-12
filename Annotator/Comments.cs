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
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Research.ReviewBot
{
  /// <summary>
  /// A blob-of-data class representing a CodeFlow comment
  /// </summary>
  [Serializable]
  public class Comment
  {
    public string Message;
    public string Path;
    public int StartChar;
    public int EndChar;
    public int StartLine;
    public int EndLine;
    public Comment() { } // the serializer wants a parameterless contructor
    public override string ToString()
    {
      //return String.Format("At: {0}({1},{4})\nMessage: {5}", Path, StartChar, EndChar, Message);
      return String.Format("At: {0}({1},{4})\nMessage: {5}", Path, StartLine, EndLine, Message);
    }
  }
}
