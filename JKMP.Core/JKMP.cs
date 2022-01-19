using System;
using System.Linq;
using JKMP.Core.Logging;

namespace JKMP.Core
{
    /// <summary>
    /// The entry point for the JKMP mod loader.
    /// </summary>
    public static class JKMP
    {
        /// <summary>
        /// Gets the core manager of JKMP which holds all plugins.
        /// </summary>
        public static JKCore Core { get; private set; } = null!;

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