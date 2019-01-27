using System;
using System.IO;
using Mono.Cecil;

namespace Bolero.Build
{
    class AssemblyResolver : IAssemblyResolver
    {
        readonly string basePath;

        public AssemblyResolver(string basePath)
        {
            this.basePath = basePath;
        }

        public void Dispose()
        {
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            var path = Path.Combine(basePath, name.Name + ".dll");
            return AssemblyDefinition.ReadAssembly(path);
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            return Resolve(name);
        }
    }
}
