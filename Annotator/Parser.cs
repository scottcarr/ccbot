/*
  ReviewBot 0.1
  Copyright (c) Microsoft Corporation
  All rights reserved. 
  
  MIT License
  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the ""Software""), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
  The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
  THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#if DEBUG
  //#define DEBUG_PRINT
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Research.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace Microsoft.Research.ReviewBot
{
  using System.Diagnostics.Contracts;
  using Microsoft.Research.ReviewBot.Annotations;
  using MethodNameId = System.String;
  using Microsoft.Research.ReviewBot.Utils;
  using System.Diagnostics;
  static class Parser 
  {
    public static IEnumerable<BaseAnnotation> GetAnnotationDictionary(CCCheckOutput xml)
    {
      #region CodeContracts
      Contract.Requires(xml != null);
      Contract.Ensures(Contract.Result <IEnumerable<BaseAnnotation>>() != null);
      Contract.Ensures(Contract.ForAll(Contract.Result <IEnumerable<BaseAnnotation>>(), annotation => annotation != null));
      #endregion CodeContracts

      Output.WriteLine("Processing the suggestions");

      var annotations = ProcessSuggestions(xml);
     
      DebugParsing(annotations);
      
      return annotations;
    }
    
    [Conditional("DEBUG_PRINT")]
    private static void DebugParsing(IEnumerable<BaseAnnotation> annotations)
    {
      var total_sugs = 0;
      foreach (var filegroup in annotations.GroupBy(x => x.FileName)) 
      {
        if (filegroup.Any())
        {
          RBLogger.Info("Annotations for file " + filegroup.First());
          RBLogger.Indent();
          foreach (var methodgroup in filegroup.GroupBy(x => x.MethodName))
          {
            if (methodgroup.Any()) 
            {
              RBLogger.Info("Annotations for method " + methodgroup.First().MethodName);
              RBLogger.Indent();
              foreach (var sug in methodgroup)
              {
                total_sugs++;
                RBLogger.Info(sug.Annotation);
              }
            }
            RBLogger.Unindent();
          }
          RBLogger.Unindent();
        }
      }
      RBLogger.Info("Total suggestions: {0}", total_sugs);
    }
    private static IEnumerable<BaseAnnotation> ProcessSuggestions(CCCheckOutput xml)
    {
      var annotations = new List<BaseAnnotation>();
      foreach (var assembly in xml.Items.OfType<CCCheckOutputAssembly>())
      {
        if (assembly.Method != null)
        {
          foreach (var methodgroup in assembly.Method.GroupBy(x => x.Name))
          {
            var first = methodgroup.Last();
            Contract.Assume(first != null);
            ProcessMethod(first, annotations);
          }
        }
      }
      return annotations.AsReadOnly();
    }
    private static void ProcessMethod(CCCheckOutputAssemblyMethod method, List<BaseAnnotation> annotations)
    {
      if (method.Name.EndsWith("..cctor"))
      {
        Output.WriteLine("Ignoring suggestions for {0} as it is a static constructor", method.Name);
        return;
      }

      if (method.Suggestion == null)
      {
#if DEBUG_PRINT
        Output.WriteLine("No suggestions for method {0}", method.Name);
#endif
        return;
      }

      foreach (var suggestion in method.Suggestion)
      {
        Contract.Assume(suggestion != null);

        string path, whyFailed = null;
        Squiggle squiggle;
        BaseAnnotation ann;
        ClousotSuggestion.Kind suggestionKind;
 
        if (Enum.TryParse(suggestion.SuggestionKind, out suggestionKind)
          && TryGetPath(suggestion.SourceLocation, out path, out squiggle, out whyFailed)
          && AnnotationFactory.TryMakeAnnotation(method, suggestion, suggestionKind, path, squiggle, out ann, out whyFailed))
        {
          annotations.Add(ann);
        }
        else
        {
          Output.WriteError("Failed to parse suggestion for {0}. Reason {1}", method.Name, whyFailed);
        }
      }
    }

    static private bool TryGetPath(string location, out string path, out Squiggle span, out string whyFailed)
    {
      // assuming we have @"... ...\... \.cs(a,b-c,d)
      var openBracketPosition = location.LastIndexOf('(');
      int row0 = -1, col0 = -1, row1 = -1, col1 = -1;
      if (openBracketPosition >= 0)
      {
        path = location.Substring(0, openBracketPosition);
        //if (path[0] == 'c') { path = path.Replace("c:\\", "C:\\"); } 

        var currSubString = String.Empty;

        var j = GetNext(location, openBracketPosition, ',', out row0); if (j < 0) goto fail;
        j = GetNext(location, j, '-', out col0); if (j < 0) goto fail;
        j = GetNext(location, j, ',', out row1); if (j < 0) goto fail;
        j = GetNext(location, j, ')', out col1); if (j < 0) goto fail;

        span = new Squiggle(row0: row0, col0: col0, row1: row1, col1: col1);

        whyFailed = null;
        return true;
      }

    fail:
      whyFailed = "The source location is invalid";
      span = default(Squiggle);
      path = null;
      return false;
    }

    static private int GetNext(MethodNameId location, int start, char endChar, out int pos)
    {
      var currStr = String.Empty;
      var j = start;
      pos = -1;
      for (; j < location.Length; j++)
      {
        var currChar = location[j];
        if(currChar == endChar)
        {
          if(Int32.TryParse(currStr, out pos))
          {
            return j;
          }
          else
          {
            // failure
            return -1;
          }
        }
        if(Char.IsDigit(currChar))
        {
          currStr += currChar;
        }
        else if(j != start) // we forgive the first character to be a non-string, but not the others!
        {
          return -1;
        }
      }
      return j;
    }
  }
  public class Squiggle
  {
    public readonly int Row0;
    public readonly int Col0;
    public readonly int Row1;
    public readonly int Col1;
    public Squiggle(int row0, int col0, int row1, int col1)
    {
      Row0 = row0;
      Row1 = row1;
      Col0 = col0;
      Col1 = col1;
    }
    public override string ToString()
    {
      return string.Format("({0},{1} - {2}, {3}", this.Row0, this.Col0, this.Row1, this.Col1);
    }
  }
}
