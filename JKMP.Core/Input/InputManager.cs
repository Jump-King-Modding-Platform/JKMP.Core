using System;
using System.Collections.Generic;
using System.Linq;
using JKMP.Core.Logging;
using JKMP.Core.Plugins;
using JKMP.Core.Windows;
using Microsoft.Xna.Framework.Input;
using Steamworks;

namespace JKMP.Core.Input
{
    internal static partial class InputManager
    {
        internal static readonly HashSet<string> ValidKeyNames = new();
        internal static readonly Dictionary<Keys, string> KeyMap = new();
        internal static readonly Dictionary<string, Keys> KeyMapReversed;

        internal static readonly HashSet<string> ModifierKeyNames = new()
        {
            "leftshift", "rightshift",
            "leftcontrol", "rightcontrol",
            "leftalt", "rightalt",
            "leftwin", "rightwin",
        };

        private static readonly Dictionary<Plugin, Bindings> PluginBindings = new();

        private static KeyboardState? lastKeyboardState;
        private static MouseState? lastMouseState;

        private static bool steamOverlayOpened;
        
        /// <summary>
        /// A list of all the keys that was just pressed down.
        /// </summary>
        private static readonly List<string> PressedKeys = new();
        
        /// <summary>
        /// A list of all the keys that was just released.
        /// </summary>
        private static readonly List<string> ReleasedKeys = new();

        /// <summary>
        /// A list of all key binds that are currently pressed down.
        /// They may or may not be bound to an action.
        /// </summary>
        private static readonly HashSet<KeyBind> PressedKeyBinds = new();

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
                "mouse1" => "LMB",
                "mouse2" => "RMB",
                "mouse3" => "MMB",
                "mouse4" => "MB4",
                "mouse5" => "MB5",
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

        public static bool RegisterAction(Plugin? plugin, string name, string uiName, KeyBind? defaultKey)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (uiName == null) throw new ArgumentNullException(nameof(uiName));

            Bindings bindings = GetOrCreateBindings(plugin);
            return bindings.RegisterAction(name, uiName, defaultKey);
        }

        public static void PressKey(KeyBind keyBind)
        {
            if (!PressedKeyBinds.Add(keyBind))
                return;
            
            InvokeActionCallbacksForInputKey(keyBind, true);
        }

        public static void ReleaseKey(KeyBind keyBind)
        {
            if (PressedKeyBinds.Remove(keyBind))
            {
                InvokeActionCallbacksForInputKey(keyBind, false);
            }

            // If there's no modifiers, release any other key binds that use the same key but also has modifiers
            if (keyBind.Modifiers == ModifierKeys.None)
            {
                List<KeyBind> toRemove = new();

                foreach (KeyBind key in PressedKeyBinds.Where(k => k.KeyName == keyBind.KeyName))
                {
                    toRemove.Add(key);
                    InvokeActionCallbacksForInputKey(key, false);
                }

                foreach (KeyBind key in toRemove)
                    PressedKeyBinds.Remove(key);
            }
        }

        private static void InvokeActionCallbacksForInputKey(KeyBind keyBind, bool pressed)
        {
            foreach (var bindings in PluginBindings.Values)
            {
                var actions = bindings.GetActionsForKey(keyBind);

                // If there's no actions bound to this key + modifiers check if there's any actions bound to this key without any modifiers
                if (actions.Count == 0)
                    actions = bindings.GetActionsForKey(new KeyBind(keyBind.KeyName, ModifierKeys.None));

                foreach (string actionName in actions)
                {
                    var callbacks = bindings.GetCallbacksForAction(actionName);

                    foreach (var callback in callbacks)
                        callback.Invoke(pressed);
                }
            }
        }

        /// <summary>
        /// Should be called after registering actions.
        /// This method loads the saved or default key bindings for the registered actions.
        /// </summary>
        public static void Initialize()
        {
            foreach (Bindings bindings in PluginBindings.Values)
            {
                foreach (ActionInfo actionInfo in bindings.GetActions())
                {
                    // todo: load saved key binding

                    if (actionInfo.DefaultKeyBind != null)
                        bindings.MapAction(actionInfo.DefaultKeyBind.Value, actionInfo.Name);
                }
            }
            
            SteamFriends.OnGameOverlayActivated += OnGameOverlayToggled;
        }

        private static void OnGameOverlayToggled(bool open)
        {
            steamOverlayOpened = open;
            PressedKeys.Clear();
            ReleasedKeys.Clear();
        }

        public static void Update()
        {
            if (steamOverlayOpened)
                return;
            
            PressedKeys.Clear();
            ReleasedKeys.Clear();
            var keyboardState = Keyboard.GetState();
            var mouseState = Mouse.GetState();
            
            if (lastKeyboardState != null && keyboardState != lastKeyboardState)
            {
                var oldKeysArray = lastKeyboardState.Value.GetPressedKeys();
                var keysArray = keyboardState.GetPressedKeys();

                var oldKeyNames = oldKeysArray.Where(k => KeyMap.ContainsKey(k)).Select(k => KeyMap[k]).ToList();
                var keyNames = keysArray.Where(k => KeyMap.ContainsKey(k)).Select(k => KeyMap[k]).ToList();

                PressedKeys.AddRange(keyNames.Except(oldKeyNames));
                ReleasedKeys.AddRange(oldKeyNames.Except(keyNames));
            }

            if (lastMouseState != null && mouseState != lastMouseState)
            {
                // Add pressed mouse buttons
                if (mouseState.LeftButton == ButtonState.Pressed && lastMouseState.Value.LeftButton == ButtonState.Released)
                    PressedKeys.Add("mouse1");

                if (mouseState.RightButton == ButtonState.Pressed && lastMouseState.Value.RightButton == ButtonState.Released)
                    PressedKeys.Add("mouse2");
                
                if (mouseState.MiddleButton == ButtonState.Pressed && lastMouseState.Value.MiddleButton == ButtonState.Released)
                    PressedKeys.Add("mouse3");
                
                if (mouseState.XButton1 == ButtonState.Pressed && lastMouseState.Value.XButton1 == ButtonState.Released)
                    PressedKeys.Add("mouse4");
                
                if (mouseState.XButton2 == ButtonState.Pressed && lastMouseState.Value.XButton2 == ButtonState.Released)
                    PressedKeys.Add("mouse5");
                
                // Add released mouse buttons
                if (mouseState.LeftButton == ButtonState.Released && lastMouseState.Value.LeftButton == ButtonState.Pressed)
                    ReleasedKeys.Add("mouse1");
                
                if (mouseState.RightButton == ButtonState.Released && lastMouseState.Value.RightButton == ButtonState.Pressed)
                    ReleasedKeys.Add("mouse2");
                
                if (mouseState.MiddleButton == ButtonState.Released && lastMouseState.Value.MiddleButton == ButtonState.Pressed)
                    ReleasedKeys.Add("mouse3");
                
                if (mouseState.XButton1 == ButtonState.Released && lastMouseState.Value.XButton1 == ButtonState.Pressed)
                    ReleasedKeys.Add("mouse4");
                
                if (mouseState.XButton2 == ButtonState.Released && lastMouseState.Value.XButton2 == ButtonState.Pressed)
                    ReleasedKeys.Add("mouse5");
            }

            {
                ModifierKeys modifiers = ModifierKeys.None;

                if (keyboardState.IsKeyDown(Keys.LeftControl))
                    modifiers |= ModifierKeys.LeftControl;
                
                if (keyboardState.IsKeyDown(Keys.RightControl))
                    modifiers |= ModifierKeys.RightControl;
                
                if (keyboardState.IsKeyDown(Keys.LeftShift))
                    modifiers |= ModifierKeys.LeftShift;
                
                if (keyboardState.IsKeyDown(Keys.RightShift))
                    modifiers |= ModifierKeys.RightShift;
                
                if (keyboardState.IsKeyDown(Keys.LeftAlt))
                    modifiers |= ModifierKeys.LeftAlt;
                
                if (keyboardState.IsKeyDown(Keys.RightAlt))
                    modifiers |= ModifierKeys.RightAlt;

                if (keyboardState.IsKeyDown(Keys.LeftWindows))
                    modifiers |= ModifierKeys.LeftWin;

                if (keyboardState.IsKeyDown(Keys.RightWindows))
                    modifiers |= ModifierKeys.RightWin;
                
                FireEvents(modifiers);
            }

            lastKeyboardState = keyboardState;
            lastMouseState = mouseState;
        }

        private static void FireEvents(ModifierKeys modifierKeys)
        {
            foreach (string key in PressedKeys)
            {
                if (!ModifierKeyNames.Contains(key))
                    PressKey(new KeyBind(key, modifierKeys));
                else
                    PressKey(new KeyBind(key, ModifierKeys.None));
            }

            foreach (string key in ReleasedKeys)
            {
                if (!ModifierKeyNames.Contains(key))
                    ReleaseKey(new KeyBind(key, modifierKeys));
                else
                    ReleaseKey(new KeyBind(key, ModifierKeys.None));
            }
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