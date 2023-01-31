// $begin{copyright}
//
// This file is part of Bolero
//
// Copyright (c) 2018 IntelliFactory and contributors
//
// Licensed under the Apache License, Version 2.0 (the "License"); you
// may not use this file except in compliance with the License.  You may
// obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
// implied.  See the License for the specific language governing
// permissions and limitations under the License.
//
// $end{copyright}

namespace Bolero

open System
open Microsoft.AspNetCore.Components

type [<AbstractClass>] Ref() =
    /// <exclude />
    abstract Render : Rendering.RenderTreeBuilder * int -> int

/// <summary>A utility to bind a reference to a rendered component.</summary>
/// <seealso href="https://fsbolero.io/docs/Blazor#html-element-references" />
/// <category>HTML</category>
type Ref<'T>() =
    inherit Ref()

    /// <summary>
    /// The element or component reference. This is <c>Some</c> if it has been bound using <see cref="M:attr.ref" />
    /// or by inserting it in a component computation expression.
    /// </summary>
    member val Value = None with get, set

    override this.Render(builder, sequence) =
        builder.AddComponentReferenceCapture(sequence, fun v -> this.Value <- tryUnbox<'T> v)
        sequence + 1

/// <summary>A utility to bind a reference to a rendered HTML element.</summary>
/// <seealso href="https://fsbolero.io/docs/Blazor#html-element-references" />
/// <category>HTML</category>
type HtmlRef() =
    inherit Ref<ElementReference>()

    override this.Render(builder, sequence) =
        builder.AddElementReferenceCapture(sequence, fun v -> this.Value <- Some v)
        sequence + 1

/// <summary>A utility to bind a reference to a rendered HTML element.</summary>
/// <seealso href="https://fsbolero.io/docs/Blazor#html-element-references" />
/// <category>HTML</category>
[<Obsolete "Use HtmlRef.">]
type ElementReferenceBinder = HtmlRef
