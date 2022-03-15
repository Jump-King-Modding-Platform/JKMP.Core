using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework.Input;

namespace JKMP.Core.Windows
{
    internal static class WinNative
    {
        [DllImport("kernel32.dll",
            EntryPoint = "AllocConsole",
            SetLastError = true,
            CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        public static extern int AllocConsole();

        [DllImport("kernel32.dll",
            EntryPoint = "AllocConsole",
            SetLastError = true,
            CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        public static extern int FreeConsole();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern int GetKeyNameTextW(uint lParam, [MarshalAs(UnmanagedType.LPWStr), Out] StringBuilder lpString, int nSize);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint MapVirtualKeyW(uint uCode, uint uMapType);
        
        private static readonly StringBuilder KeyNameBuilder = new(capacity: 260);
        
        public static string GetKeyName(Keys key)
        {
            uint scanCode = MapVirtualKeyW((uint)key, 0) << 16;

            KeyNameBuilder.Clear();
            GetKeyNameTextW(scanCode, KeyNameBuilder, KeyNameBuilder.Capacity);

            return KeyNameBuilder.ToString();
        }
    }
}
