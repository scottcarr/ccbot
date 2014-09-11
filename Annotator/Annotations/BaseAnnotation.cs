/*
  ReviewBot 0.1
  Copyright (c) Microsoft Corporation
  All rights reserved. 
  
  MIT License
  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the ""Software""), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
  The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
  THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Research.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace Microsoft.Research.ReviewBot.Annotations
{
  using System.Diagnostics.Contracts;
  using Microsoft.Research.ReviewBot.Utils;
  public abstract class BaseAnnotation
  {
    public readonly string FileName, MethodName, Annotation;
    public readonly ClousotSuggestion.Kind Kind;
    public readonly Squiggle Squiggle;
    public readonly SyntaxNode statement_syntax;

    public BaseAnnotation(string filename, string methodName, string annotation, Squiggle squiggle, ClousotSuggestion.Kind kind)
    {
      #region CodeContracts
      Contract.Requires(annotation != null, "We need an annotation!!!");
      Contract.Ensures(this.FileName == filename);
      Contract.Ensures(this.Annotation == annotation + Constants.String.Signature);
      Contract.Ensures(this.MethodName == methodName);
      Contract.Ensures(this.Squiggle == squiggle);
      Contract.Ensures(this.Kind == kind);
      #endregion CodeContracts

      this.Squiggle = squiggle;
      this.FileName = filename;
      this.MethodName = methodName;
      this.Annotation = annotation + Constants.String.Signature;
      this.Kind = kind;
      this.statement_syntax = SyntaxFactory.ParseStatement(Annotation);
    }

    public override string ToString()
    {
      return string.Format("{0} - {1}", this.FileName, this.Annotation);
    }
  }
  public static class AnnotationFactory
  {
    public static bool TryMakeAnnotation(CCCheckOutputAssemblyMethod method,
                                         CCCheckOutputAssemblyMethodSuggestion suggestion,
                                         ClousotSuggestion.Kind suggestionKind,
                                         string path,
                                         Squiggle squiggle,
                                         out BaseAnnotation annotation, out string whyFailed)
    {
      #region CodeContracts
      Contract.Ensures(!Contract.Result<bool>() || Contract.ValueAtReturn(out annotation) != null);
      Contract.Ensures(Contract.Result<bool>() || Contract.ValueAtReturn(out whyFailed) != null);
      #endregion CodeContracts

      whyFailed = null;
      annotation = null;
      switch (suggestionKind)
      {
        case ClousotSuggestion.Kind.AssumeOnEntry:
        case ClousotSuggestion.Kind.Ensures:
        case ClousotSuggestion.Kind.Requires:
          {
            annotation = new Precondition(path, method.Name, suggestion.Suggested, squiggle, suggestionKind);
            return true;
          }

        case ClousotSuggestion.Kind.ObjectInvariant:
          {
            Contract.Assert(suggestion.SuggestionExtraInfo != null, "We expect to have extra info for the suggestion of an object invariant");
            var exinfo2 = suggestion.SuggestionExtraInfo[0];
            annotation = new UnresolvedObjectInvariant(path, method.Name, exinfo2.TypeDocumentId, exinfo2.SuggestedCode, squiggle, suggestionKind);
            return true;
          }

        case ClousotSuggestion.Kind.EnsuresNecessary:
          {
            Contract.Assert(suggestion.SuggestionExtraInfo != null, "We expect to have extra info for the suggestion of an ensures necessary");
            if (suggestion.SuggestionExtraInfo.First().CalleeMemberKind == "Interface")
            {
              var exinfo = suggestion.SuggestionExtraInfo[0];
              if (exinfo.CalleeIsDeclaredInTheSameAssembly.Equals("False"))
              {
                whyFailed = "Don't know what to do when callee is in another assembly";
                return false;
              }
              //annotation = new UnresolvedInterfaceAnnotation(exinfo, squiggle, suggestionKind);
              UnresolvedInterfaceAnnotation ann;
              if (InterfaceAnnotationHelpers.TryMakeUnresolvedInterfaceAnnotation(exinfo, squiggle, suggestionKind, out ann))
              {
                annotation = ann;
                return true;
              }
              whyFailed = "Unknown suggestion";
              return false;
            }
            whyFailed = string.Format("Unhandled clousot suggestion kind {0} {1}", suggestionKind, suggestion.SuggestionExtraInfo.First().CalleeMemberKind);
            return false;
          }

        case ClousotSuggestion.Kind.ReadonlyField:
          {
            Contract.Assert(suggestion.SuggestionExtraInfo != null, "We expect to have extra info for the suggestion of a readonly field");
            var extraInfo = suggestion.SuggestionExtraInfo[0];
            annotation = new ReadonlyField(extraInfo.CalleeDocumentId, path, extraInfo.TypeDocumentId, method.Name, squiggle, ClousotSuggestion.Kind.ReadonlyField);
            return true;
          }

        default:
          {
            whyFailed = string.Format("Unhandled clousot suggestion kind {0}", suggestionKind);
            return false;
          }
      }
    }
  }
}
