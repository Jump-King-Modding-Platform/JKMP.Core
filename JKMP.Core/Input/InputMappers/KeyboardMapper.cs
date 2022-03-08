using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;

namespace JKMP.Core.Input.InputMappers
{
    internal partial class KeyboardMapper : IInputMapper
    {
        internal static readonly Dictionary<Keys, string> KeyMap = new();
        private static readonly Dictionary<string, Keys> KeyMapReversed;
        private static readonly HashSet<Keys> AllKeys;

        static KeyboardMapper()
        {
            AllKeys = new HashSet<Keys>(Enum.GetValues(typeof(Keys)).Cast<Keys>());
            
            foreach (Keys key in AllKeys)
            {
                string? name = GetKeyName(key);

                if (name != null)
                {
                    KeyMap.Add(key, name);
                }
            }

            KeyMapReversed = KeyMap.ToDictionary(kv => kv.Value, kv => kv.Key);
        }
        
        public ISet<string> ModifierKeys { get; }
        public ISet<string> ValidKeyNames { get; }

        private KeyboardState currentState;
        private KeyboardState lastState;

        public KeyboardMapper()
        {
            ValidKeyNames = new HashSet<string>();

            foreach (var kv in KeyMap)
                ValidKeyNames.Add(kv.Value);

            ModifierKeys = new HashSet<string>
            {
                KeyMap[Keys.LeftShift],
                KeyMap[Keys.RightShift],
                KeyMap[Keys.LeftControl],
                KeyMap[Keys.RightControl],
                KeyMap[Keys.LeftAlt],
                KeyMap[Keys.RightAlt],
                KeyMap[Keys.LeftWindows],
                KeyMap[Keys.RightWindows],
            };
        }

        public IEnumerable<string> GetPressedKeys()
        {
            foreach (Keys key in AllKeys)
            {
                if (currentState.IsKeyDown(key) && lastState.IsKeyUp(key))
                    yield return KeyMap[key];
            }
        }

        public IEnumerable<string> GetReleasedKeys()
        {
            foreach (Keys key in AllKeys)
            {
                if (currentState.IsKeyUp(key) && lastState.IsKeyDown(key))
                    yield return KeyMap[key];
            }
        }

        public void Update()
        {
            lastState = currentState;
            currentState = Keyboard.GetState();
        }
    }
}