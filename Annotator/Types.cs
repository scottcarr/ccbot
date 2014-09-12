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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.ObjectModel;

namespace Microsoft.Research.ReviewBot
{
  using Microsoft.Research.ReviewBot.Annotations;
  using FilePath = System.String;
  using MethodNameId = System.String;

  /* A nested dictionary mapping a file/SyntaxTree to a dictionary mapping method names to suggestions
  */
  public class AnnotationDictionary : Dictionary<FilePath, Dictionary<MethodNameId, List<BaseAnnotation>>> 
  {
    public AnnotationDictionary () : base(StringComparer.OrdinalIgnoreCase) {}
  }

  /* A nested dictionary mapping a file/SyntaxTree to a dictionary mapping SyntaxNodes to the associated
   * suggestions
  */
  public class SyntaxDictionary : Dictionary<FilePath, Dictionary<SyntaxNode, List<BaseAnnotation>>> 
  {
    public SyntaxDictionary () : base(StringComparer.OrdinalIgnoreCase) {}
  }
  
  /* A nested dictionary mapping a file/SyntaxTree to a dictionary mapping SyntaxNodes to the SyntaxNodes that
   * are their replacements (if precomputing a replacement is possible, otherwise the true replace is calculated
   * on-the-fly during SyntaxTree.ReplaceNodes()
  */
  public class ReplacementDictionary : Dictionary<FilePath, Dictionary<SyntaxNode, SyntaxNode>> 
  {
    public ReplacementDictionary () : base(StringComparer.OrdinalIgnoreCase) {}
  }
}
