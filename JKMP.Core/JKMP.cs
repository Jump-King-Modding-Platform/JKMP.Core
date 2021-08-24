using System;
using System.Linq;
using JKMP.Core.Logging;

namespace JKMP.Core
{
    public static class JKMP
    {
#pragma warning disable 8618
        public static JKCore Core { get; private set; }
#pragma warning restore 8618

        public static void Initialize()
        {
            // todo: use a commandline library
            if (Environment.GetCommandLineArgs().Any(arg => arg.ToLowerInvariant() == "--console" || arg.ToLowerInvariant() == "-c"))
            {
                ConsoleManager.InitializeConsole();
            }
            
            // Consider carefully what is called before SetupAssemblyResolving.
            // If any outside assembly is referenced it will be loaded by the runtime.
            // If it's not found in the root game folder the game will crash due to the dll being located in JKMP/Dependencies.
            // If absolutely necessary, place those dlls in the game root instead.
            
            LogManager.InitializeLogging();
            AssemblyManager.SetupAssemblyResolving(AppDomain.CurrentDomain);
            Core = new();
        }
    }
}