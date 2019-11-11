using System.Reflection;
using System.Runtime.Loader;

namespace RazorComponentManager
{
    public class UnloadableAssemblyContext : AssemblyLoadContext
    {
        public UnloadableAssemblyContext() : base(isCollectible: true)
        { }

        protected override Assembly Load(AssemblyName assemblyName)
            => null;
    }
}
