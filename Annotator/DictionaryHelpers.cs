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

namespace Microsoft.Research.ReviewBot
{
  class DictionaryHelpers
  {
    //public static List<T2> SmushValues<T1,T2>(Dictionary<T1, List<T2>> dict)
    //{
    //  var smushed = new List<T2>();
    //  foreach (var shortlist in dict.Values)
    //  {
    //    smushed = smushed.Concat(shortlist.ToList()).ToList();
    //  }
    //  return smushed;
    //}
    public static int CountTotalItems<T1,T2>(Dictionary<T1, List<T2>> dict)
    {
      #region CodeContracts
      Contract.Requires(dict != null);
      Contract.Ensures(0 <= Contract.Result<int>());
      #endregion CodeContracts

      var count = 0;
      foreach (var v in dict.Values)
      {
        count += v.Count;
      }
      return count;
    }
    //public static List<T2> FindMissingItems<T1,T2>(Dictionary<T1, List<T2>> orig_set, Dictionary<T1, List<T2>> subset)
    //{
    //  var orig = SmushValues<T1,T2>(orig_set);
    //  var sub = SmushValues<T1,T2>(subset);
    //  return null;
    //}

    /// <summary>
    /// This is just Dictionary.Add except the value of the top level dictionary is itself a dictionary
    /// and when the inner dictionary exists, we merge the lists
    /// </summary>
    /// <typeparam name="T1">The type of the keys of the outer most dictionary</typeparam>
    /// <typeparam name="T2">The type of the keys of the inner dictionary</typeparam>
    /// <typeparam name="T3">The type of the values of the inner dictionary</typeparam>
    /// <param name="original">The dictionary to be inserted into</param>
    /// <param name="key">The outer dictionary key where we will insertj</param>
    /// <param name="newentry">The inner dictionary the will be inserts at the key</param>
    public static void InsertItem<T1,T2,T3>(Dictionary<T1, Dictionary<T2,List<T3>>> original, T1 key, Dictionary<T2,List<T3>> newentry) 
    {
      #region CodeContracts
      Contract.Requires(original != null);
      Contract.Requires(key != null);
      Contract.Requires(newentry != null);
      #endregion CodeContracts

      Dictionary<T2, List<T3>> existing;
      if (original.TryGetValue(key, out existing))
      {
        foreach (var kvp in newentry) 
        {
          Contract.Assume(kvp.Key != null);
          Contract.Assume(kvp.Value != null);
          InsertItem(existing, kvp.Key, kvp.Value);
        }
      }
      else
      {
        original.Add(key, newentry);
      }
    }
    /// <summary>
    /// This is just Dictionary.Add except when the key already exists it merges the lists
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <param name="original"></param>
    /// <param name="key"></param>
    /// <param name="values"></param>
    public static void InsertItem<T1,T2>(Dictionary<T1,List<T2>> original, T1 key, List<T2> values)
    {
      #region CodeContracts
      Contract.Requires(original != null);
      Contract.Requires(key != null);
      #endregion CodeContracts

      List<T2> existing;
      if (original.TryGetValue(key, out existing))
      {
        original[key] = existing.Concat(values).ToList();
      }
      else
      {
        original.Add(key, values);
      }
    }
    /// <summary>
    /// This is just Dictionary.Add except when the key already exists it appends the value to the list
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <param name="original"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public static void InsertItem<T1,T2>(Dictionary<T1,List<T2>> original, T1 key, T2 value)
    {
      #region CodeContracts
      Contract.Requires(original != null);
      Contract.Requires(key != null);
      #endregion CodeContracts

      List<T2> existing;
      if (original.TryGetValue(key, out existing))
      {
        existing.Add(value);
      }
      else
      {
        original.Add(key, new List<T2>() {value});
      }
    }
    // Where is InsertItem<T1,T2,T3,T4, ... ,TN>? haha. I hope this isn't a daily WTF
  }
}
