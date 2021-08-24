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
            
            LogManager.InitializeLogging();
            AssemblyManager.SetupAssemblyResolving(AppDomain.CurrentDomain);
            Core = new();
        }
    }
}