using System;
using System.Collections.Generic;
using System.Linq;
using JKMP.Core.Logging;
using JKMP.Core.Plugins;
using JKMP.Core.Windows;
using Microsoft.Xna.Framework.Input;

namespace JKMP.Core.Input
{
    internal static partial class InputManager
    {
        internal static readonly HashSet<string> ValidKeyNames = new();
        internal static readonly Dictionary<Keys, string> KeyMap = new();
        internal static readonly Dictionary<string, Keys> KeyMapReversed;

        private static readonly Dictionary<Plugin, Bindings> PluginBindings = new();

        private static KeyboardState? lastKeyboardState;
        private static MouseState? lastMouseState;
        
        /// <summary>
        /// A list of all the keys that was just pressed down.
        /// </summary>
        private static readonly List<string> pressedKeys = new();
        
        /// <summary>
        /// A list of all the keys that was just released.
        /// </summary>
        private static readonly List<string> releasedKeys = new();

        static InputManager()
        {
            foreach (Keys key in Enum.GetValues(typeof(Keys)))
            {
                string? name = GetKeyName(key);

                if (name != null)
                {
                    if (!ValidKeyNames.Add(name))
                        throw new InvalidOperationException($"Duplicate key name: {name}");

                    KeyMap.Add(key, name);
                }
            }

            // add mouse1-5
            for (int i = 1; i <= 5; ++i)
                ValidKeyNames.Add($"mouse{i}");

            KeyMapReversed = KeyMap.ToDictionary(kv => kv.Value, kv => kv.Key);
        }

        /// <summary>
        /// Returns the name of the given input key name. The key name should be mouse1-5 or one of the names specified in <see cref="GetKeyName"/>.
        /// If it's not a valid key name, the input value is returned.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetKeyBindingName(string name)
        {
            return name switch
            {
                "mouse1" => "Mouse Left",
                "mouse2" => "Mouse Right",
                "mouse3" => "Mouse Middle",
                "mouse4" => "Mouse Btn 4",
                "mouse5" => "Mouse Btn 5",
                _ => ValidKeyNames.Contains(name) ? WinNative.GetKeyName(KeyMapReversed[name]) : name
            };
        }
        
        public static void BindAction(Plugin? plugin, string actionName, PluginInput.BindActionCallback callback)
        {
            if (actionName == null) throw new ArgumentNullException(nameof(actionName));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            Bindings bindings = GetOrCreateBindings(plugin);
            bindings.AddActionCallback(actionName, callback);
        }
        
        public static bool UnbindAction(Plugin? plugin, string actionName, PluginInput.BindActionCallback callback)
        {
            if (actionName == null) throw new ArgumentNullException(nameof(actionName));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            Bindings bindings = GetOrCreateBindings(plugin);
            return bindings.RemoveActionCallback(actionName, callback);
        }

        public static bool RegisterAction(Plugin? plugin, string name, string uiName, string? defaultKey)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (uiName == null) throw new ArgumentNullException(nameof(uiName));

            if (defaultKey != null && !ValidKeyNames.Contains(defaultKey))
                throw new ArgumentException($"Invalid default key: {defaultKey}");

            Bindings bindings = GetOrCreateBindings(plugin);
            return bindings.RegisterAction(name, uiName, defaultKey);
        }

        /// <summary>
        /// Should be called after registering actions.
        /// This method loads the saved or default key bindings for the registered actions.
        /// </summary>
        public static void Initialize()
        {
            
        }
        
        public static void Update()
        {
            pressedKeys.Clear();
            releasedKeys.Clear();
            var keyboardState = Keyboard.GetState();
            var mouseState = Mouse.GetState();
            
            if (lastKeyboardState != null && keyboardState != lastKeyboardState)
            {
                var oldKeysArray = lastKeyboardState.Value.GetPressedKeys();
                var keysArray = keyboardState.GetPressedKeys();

                var oldKeyNames = oldKeysArray.Where(k => KeyMap.ContainsKey(k)).Select(k => KeyMap[k]).ToList();
                var keyNames = keysArray.Where(k => KeyMap.ContainsKey(k)).Select(k => KeyMap[k]).ToList();

                pressedKeys.AddRange(keyNames.Except(oldKeyNames));
                releasedKeys.AddRange(oldKeyNames.Except(keyNames));
            }

            if (lastMouseState != null && mouseState != lastMouseState)
            {
                // Add pressed mouse buttons
                if (mouseState.LeftButton == ButtonState.Pressed && lastMouseState.Value.LeftButton == ButtonState.Released)
                    pressedKeys.Add("mouse1");

                if (mouseState.RightButton == ButtonState.Pressed && lastMouseState.Value.RightButton == ButtonState.Released)
                    pressedKeys.Add("mouse2");
                
                if (mouseState.MiddleButton == ButtonState.Pressed && lastMouseState.Value.MiddleButton == ButtonState.Released)
                    pressedKeys.Add("mouse3");
                
                if (mouseState.XButton1 == ButtonState.Pressed && lastMouseState.Value.XButton1 == ButtonState.Released)
                    pressedKeys.Add("mouse4");
                
                if (mouseState.XButton2 == ButtonState.Pressed && lastMouseState.Value.XButton2 == ButtonState.Released)
                    pressedKeys.Add("mouse5");
                
                // Add released mouse buttons
                if (mouseState.LeftButton == ButtonState.Released && lastMouseState.Value.LeftButton == ButtonState.Pressed)
                    releasedKeys.Add("mouse1");
                
                if (mouseState.RightButton == ButtonState.Released && lastMouseState.Value.RightButton == ButtonState.Pressed)
                    releasedKeys.Add("mouse2");
                
                if (mouseState.MiddleButton == ButtonState.Released && lastMouseState.Value.MiddleButton == ButtonState.Pressed)
                    releasedKeys.Add("mouse3");
                
                if (mouseState.XButton1 == ButtonState.Released && lastMouseState.Value.XButton1 == ButtonState.Pressed)
                    releasedKeys.Add("mouse4");
                
                if (mouseState.XButton2 == ButtonState.Released && lastMouseState.Value.XButton2 == ButtonState.Pressed)
                    releasedKeys.Add("mouse5");
            }

            FireEvents();

            lastKeyboardState = keyboardState;
            lastMouseState = mouseState;
        }

        private static void FireEvents()
        {
            
        }

        private static Bindings GetOrCreateBindings(Plugin? plugin)
        {
            plugin ??= Plugin.InternalPlugin;

            if (!PluginBindings.TryGetValue(plugin, out var result))
            {
                result = new();
                PluginBindings.Add(plugin, result);
            }
            
            return result;
        }
    }
}