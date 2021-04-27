using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace Bolero.Build
{
    internal class AssemblyResolver : IAssemblyResolver
    {
        private readonly IDictionary<string, string> _referencePaths;

        private readonly IDictionary<string, AssemblyDefinition> _cache;

        public AssemblyResolver(IEnumerable<string> referencePaths)
        {
            _referencePaths = referencePaths.ToDictionary(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase);
            _cache = new Dictionary<string, AssemblyDefinition>(StringComparer.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            foreach (var item in _cache)
            {
                item.Value.Dispose();
            }
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            if (_cache.TryGetValue(name.Name, out var assembly))
            {
                return assembly;
            }
            else if (_referencePaths.TryGetValue(name.Name, out var path))
            {
                assembly = AssemblyDefinition.ReadAssembly(path);
                _cache.Add(name.Name, assembly);
                return assembly;
            }
            else
            {
                throw new Exception($"Could not resolve assembly {name}");
            }
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            return Resolve(name);
        }
    }
}
