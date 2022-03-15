using System;
using System.IO;
using System.Text;
using JKMP.Core.Windows;

namespace JKMP.Core
{
    internal static class ConsoleManager
    {
        private static StreamWriter? consoleWriter;
        
        public static void InitializeConsole()
        {
            if (Environment.OSVersion.Platform != PlatformID.MacOSX && Environment.OSVersion.Platform != PlatformID.Unix)
            {
                WinNative.AllocConsole();
            }

            consoleWriter = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
            Console.OutputEncoding = Encoding.UTF8;
            Console.SetOut(consoleWriter);
        }
    }
}