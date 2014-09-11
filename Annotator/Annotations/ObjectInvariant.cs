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
using System.Diagnostics.Contracts;
using Microsoft.Research.CodeAnalysis;

namespace Microsoft.Research.ReviewBot.Annotations
{
  public class UnresolvedObjectInvariant : BaseAnnotation
  {
    public readonly String TypeName;
     public UnresolvedObjectInvariant(string filename, string methodname, string typename, string annotation, Squiggle squiggle, ClousotSuggestion.Kind kind)
      : base(filename, methodname, annotation, squiggle, kind)
    {
      #region CodeContracts
      Contract.Requires(methodname != null);
      Contract.Requires(annotation != null);
      Contract.Requires(typename != null);
      Contract.Requires(kind == ClousotSuggestion.Kind.ObjectInvariant);
      Contract.Ensures(typename == this.TypeName);
      #endregion CodeContracts

      this.TypeName = typename;
    }

    ///// <summary>
    ///// Return a clone of this ObjectInvariant with a different MethodName
    ///// </summary>
    ///// <param name="dna"></param>
    ///// <param name="methodname"></param>
    // public ObjectInvariant WithMethodName(string methodname)
    // {
    //  #region CodeContracts
    //  Contract.Requires(!string.IsNullOrEmpty(methodname));
    //  Contract.Ensures(Contract.Result<ObjectInvariant>() != null);
    //  #endregion CodeContracts
    //  return new ObjectInvariant(FileName, methodname, TypeName, Annotation.ToString().Replace(Reviewer.signature, ""), Squiggle, Kind);
    // }

    ///// <summary>
    ///// Return a clone of this ObjectInvariant with a different FileName
    ///// </summary>
    ///// <param name="dna"></param>
    ///// <param name="methodname"></param>
    // public ObjectInvariant WithFileName(string filename)
    // {
    //  #region CodeContracts
    //  Contract.Requires(!string.IsNullOrEmpty(filename));
    //  Contract.Ensures(Contract.Result<ObjectInvariant>() != null);
    //  #endregion CodeContracts
    //  return new ObjectInvariant(filename, MethodName, TypeName, Annotation.ToString().Replace(Reviewer.signature, ""), Squiggle, Kind);
    // }
    // //public void SetMethodName(String documentationCommentId)
    // //{
    // //  this.MethodName = documentationCommentId;
    // //}
  }

  public class ResolvedObjectInvariant : BaseAnnotation
  {
    public readonly UnresolvedObjectInvariant OriginalAnnotation;
    public ResolvedObjectInvariant(UnresolvedObjectInvariant original, string filename, string methodname) 
      : base(filename, methodname, original.Annotation, original.Squiggle, original.Kind)
    {
      #region CodeContracts
      Contract.Requires(original != null);
      Contract.Requires(filename != null);
      Contract.Requires(methodname != null);
      Contract.Ensures(this.FileName == filename);
      Contract.Ensures(this.MethodName == methodname);
      Contract.Ensures(this.OriginalAnnotation == original);
      #endregion CodeContracts
      
      this.OriginalAnnotation = original;
    }
  }

  public class ReadonlyField : BaseAnnotation
  {
    public readonly string TypeName;
    public readonly string FieldName;
    public ReadonlyField(string fieldname, string filename, string typename, string methodname, Squiggle squiggle, ClousotSuggestion.Kind kind)
      : base(filename, methodname, String.Empty, squiggle, kind)
    {
      Contract.Requires(typename != null);
      Contract.Requires(fieldname != null);
      Contract.Requires(methodname != null);

      this.TypeName = typename;
      this.FieldName = fieldname;
    }
  }
}
