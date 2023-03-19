using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Bolero.Build
{
    public class BoleroApplyCssScopes : Task
    {
        [Required]
        public ITaskItem[] ScopedCss { get; set; }

        [Required]
        public string ScopedCssSourceFile { get; set; }

        public override bool Execute()
        {
            using var file = new StreamWriter(ScopedCssSourceFile, false);
            file.WriteLine("[<System.Runtime.CompilerServices.CompilerGenerated>]");
            file.WriteLine("module internal CssScopes");
            foreach (var item in ScopedCss)
            {
                var scopeName = item.GetMetadata("ScopeName");
                var scope = item.GetMetadata("CssScope");
                file.WriteLine($"""let [<Literal>] ``{scopeName}`` = "{scope}";""");
            }
            return true;
        }
    }
}
