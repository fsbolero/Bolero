using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace Bolero.Build
{
    internal class AssemblyResolver : IAssemblyResolver
    {
        private readonly IEnumerable<string> basePaths;

        public AssemblyResolver(IEnumerable<string> basePaths)
        {
            this.basePaths = basePaths;
        }

        public void Dispose()
        {
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            var path = basePaths
                .Select(p => Path.Combine(p, name.Name + ".dll"))
                .FirstOrDefault(File.Exists)
                ?? throw new Exception($"Could not resolve assembly {name}");
            return AssemblyDefinition.ReadAssembly(path);
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            return Resolve(name);
        }
    }
}
