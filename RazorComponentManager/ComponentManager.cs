using Microsoft.AspNetCore.Components;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace RazorComponentManager
{
    public class ComponentManager : IDisposable
    {
        // Static factory

        private ComponentManager()
        { }

        public static ComponentManager GetComponent(string assemblyPathname)
        {
            string pathname = assemblyPathname.Contains(@"^") ? DecodedPathname(assemblyPathname) : assemblyPathname;
            var cm = new ComponentManager();
            cm.CreateInstance(pathname);
            return cm;
        }

        public static string EncodedPathname(string normalPathname)
            => normalPathname.Replace(@"/", @"^").Replace(@"\", @"^");

        public static string DecodedPathname(string normalPathname)
            => normalPathname.Replace(@"^", @"\");

        // Instance members 

        public RenderFragment Instance = null;
        private WeakReference WeakALC = null;
        private string Pathname = string.Empty;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void CreateInstance(string assemblyPathname)
        {
            Pathname = assemblyPathname;
            var alc = new UnloadableAssemblyContext();
            WeakALC = new WeakReference(alc, trackResurrection: true);
            
            using var fs = new FileStream(Pathname, FileMode.Open, FileAccess.Read);
            var assembly = alc.LoadFromStream(fs);
            
            var type = assembly.GetType("RazorComponent.Component");
            if (type == null)
            {
                Dispose();
                throw new Exception($"GetType failed for 'RazorComponent.Component' in assembly '{assemblyPathname}'.");
            }

            var component = Activator.CreateInstance(type);
            if (component == null)
            {
                Dispose();
                throw new Exception($"Activation failed for 'RazorComponent.Component' in assembly '{assemblyPathname}'.");
            }

            Instance = typeof(ComponentBase)
                .GetField("_renderFragment", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(component) as RenderFragment;
        }

        public void Dispose()
        {
            Instance = null;
            Pathname = null;
            (WeakALC.Target as UnloadableAssemblyContext).Unload();
            for (int i = 0; i < 10 && WeakALC.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            WeakALC = null;
        }
    }
}