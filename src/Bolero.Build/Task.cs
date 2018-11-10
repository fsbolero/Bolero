using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;

namespace Bolero.Build{

    public class BoleroTask : Task {

        public ITaskItem[] Assemblies { get; set; }

        private void StripFile(string f) {
            var anyChanged = false;
            var bytes = File.ReadAllBytes(f);
            using (var mem = new MemoryStream(bytes))
            using (var asm = AssemblyDefinition.ReadAssembly(mem))
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
                    this.Log.LogMessage("Stripped F# metadata from {0}", f);
                }
            }
        }

        public override bool Execute() {
            foreach (var asm in this.Assemblies) {
                try {
                    this.StripFile(asm.ItemSpec);
                } catch (Exception exn) {
                    this.Log.LogErrorFromException(exn);
                    return false;
                }
            }
            return true;
        }
    }
}