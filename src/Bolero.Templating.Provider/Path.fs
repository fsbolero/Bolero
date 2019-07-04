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

module Bolero.Templating.Path

open System.IO

/// Canonicalize a path: remove . and .. components, unify slashes.
let Canonicalize (path: string) =
    FileInfo(path).FullName

/// Given a base directory and a full path, return the corresponding relative path.
/// eg: if baseDir = "c:/foo" and fullPath = "c:/foo/bar/baz.html", then return "bar/baz.html".
/// Assumes that both fullPath and baseDir are canonical (see Canonicalize above).
/// Fails if fullPath is not a subdirectory of baseDir.
let GetRelativePath (baseDir: string) (fullPath: string) =
    let rec go (thisDir: string) =
        if thisDir = baseDir then
            fullPath.[thisDir.Length + 1..]
        elif thisDir.Length <= baseDir.Length then
            invalidArg "fullPath" (sprintf "'%s' is not a subdirectory of '%s'" fullPath baseDir)
        else
            go (Path.GetDirectoryName thisDir)
    go fullPath
