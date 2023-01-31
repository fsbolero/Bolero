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

/// <exclude />
module Bolero.Virtualize.Internals

open System
open System.Collections
open System.Collections.Generic

type Collection<'T>(coll: IReadOnlyCollection<'T>) =

    interface IEnumerable<'T> with
        member _.GetEnumerator() =
            (coll :> IEnumerable<'T>).GetEnumerator()

    interface IEnumerable with
        member _.GetEnumerator() =
            (coll :> IEnumerable).GetEnumerator()

    interface ICollection<'T> with

        member _.Count = coll.Count

        member _.IsReadOnly = true

        member _.CopyTo(destinationArray, arrayIndex) =
            if isNull destinationArray then
                raise (ArgumentNullException(nameof destinationArray))
            if arrayIndex < 0 then
                raise (ArgumentOutOfRangeException(nameof arrayIndex))
            if arrayIndex + coll.Count > destinationArray.Length then
                raise (ArgumentException("Destination array was not long enough.", nameof destinationArray))
            let mutable i = arrayIndex
            for x in coll do
                destinationArray.[i] <- x
                i <- i + 1

        member _.Contains(_item) = false

        member _.Add(_item) = raise (NotSupportedException())
        member _.Clear() = raise (NotSupportedException())
        member _.Remove(_item) = raise (NotSupportedException())
