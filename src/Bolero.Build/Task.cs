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

using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;

namespace Bolero.Build {

    public class BoleroTask : Task {

        public ITaskItem[] AssembliesDir { get; set; }
        public ITaskItem[] ResolveDir { get; set; }

        private void StripFile(string f, IAssemblyResolver resolver) {
            var anyChanged = false;
            var bytes = File.ReadAllBytes(f);
            var basePath = Path.GetDirectoryName(f);
            var param = new ReaderParameters
            {
                AssemblyResolver = resolver,
            };
            using (var mem = new MemoryStream(bytes))
            using (var asm = AssemblyDefinition.ReadAssembly(mem, param))
            {
                var resources = asm.MainModule.Resources;
                for (var i = resources.Count - 1; i >= 0; i--) {
                    var name = resources[i].Name;
                    if (name == "FSharpOptimizationData." + asm.Name.Name ||
                        name == "FSharpSignatureData." + asm.Name.Name ||
                        name == "FSharpOptimizationInfo." + asm.Name.Name ||
                        name == "FSharpSignatureInfo." + asm.Name.Name) {
                        resources.RemoveAt(i);
                        anyChanged = true;
                    }
                }
                if (anyChanged) {
                    asm.Write(f);
                    Log.LogMessage("Stripped F# metadata from {0}", f);
                }
            }
        }

        public override bool Execute() {
            var resolver = new AssemblyResolver(ResolveDir.Select(dir => dir.ItemSpec));
            foreach (var dir in AssembliesDir)
            {
                foreach (var asm in Directory.GetFiles(dir.ItemSpec))
                {
                    if (Path.GetExtension(asm).Equals(".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            StripFile(asm, resolver);
                        }
                        catch (Exception exn)
                        {
                            Log.LogError("Bolero failed to strip F# metadata from {0}: {1}", asm, exn.Message);
                            Log.LogMessage("{0}", exn);
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }
}
