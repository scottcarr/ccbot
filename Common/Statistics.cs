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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.ReviewBot.Utils
{
  public class Statistics
  {
    [ContractInvariantMethod]
    private void ObjectInvariant()
    {
      Contract.Invariant(NumberOfSuggestions != null);
      Contract.Invariant(NumberOfWarnings != null);
      Contract.Invariant(RunTimes != null);
    }

    public readonly List<int> NumberOfSuggestions = new List<int>();
    public readonly List<int> NumberOfWarnings = new List<int>();
    public readonly List<TimeSpan> RunTimes = new List<TimeSpan>();

    public void PrintStatistics()
    {
      Output.WritePhase("Printing statistics");

      Output.WriteLine("Number of suggestions: ");
      PrintList(NumberOfSuggestions);
      
      Output.WriteLine("Number of warnings: ");
      PrintList(NumberOfWarnings);

      Output.WriteLine("Execution Times: ");
      PrintList(RunTimes);
    }

    private void PrintList(IEnumerable what)
    {
      var count = 0;
      foreach (var sugg in what)
      {
        if (sugg != null)
        {
          Output.WriteLine(false, "Iteration {0}: {1}", count.ToString(), sugg.ToString());
          count++;
        }
      }
    }
  }
}
