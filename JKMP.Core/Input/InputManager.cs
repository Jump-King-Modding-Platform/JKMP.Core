using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using JKMP.Core.Input.InputMappers;
using JKMP.Core.Logging;
using JKMP.Core.Plugins;
using Microsoft.Xna.Framework.Input;
using Steamworks;

namespace JKMP.Core.Input
{
    internal static partial class InputManager
    {
        internal static readonly HashSet<string> ValidKeyNames = new();
        internal static readonly HashSet<string> ModifierKeyNames = new();

        private static readonly Dictionary<Plugin, Bindings> PluginBindings = new();

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

        /// <summary>
        /// A list of all key binds that were pressed last frame.
        /// They may or may not be bound to an action.
        /// </summary>
        private static readonly HashSet<KeyBind> LastPressedKeyBinds = new();

        /// <summary>
        /// A list of all actions that were not bound to any keys when loaded.
        /// Used for not overwriting unbound actions with default key binds.
        /// </summary>
        private static readonly Dictionary<Plugin, HashSet<string>> UnboundActions = new();

        /// <summary>
        /// A list of all input mappers that translates arbitrary input into strings that can be used by the input system
        /// </summary>
        private static readonly List<IInputMapper> InputMappers = new()
        {
            new KeyboardMapper(),
            new MouseMapper(),
            new ControllerMapper()
        };

        static InputManager()
        {
            UpdateValidKeyNames();
        }

        private static void UpdateValidKeyNames()
        {
            foreach (var mapper in InputMappers)
            {
                foreach (var keyName in mapper.ValidKeyNames)
                {
                    if (!ValidKeyNames.Add(keyName))
                        throw new InvalidOperationException($"Duplicate key name: {keyName}");
                }

                foreach (var modifierName in mapper.ModifierKeys)
                {
                    if (!mapper.ValidKeyNames.Contains(modifierName))
                        throw new InvalidOperationException($"Modifier key name not found in list of valid keys: {modifierName}");

                    if (!ModifierKeyNames.Add(modifierName))
                        throw new InvalidOperationException($"Duplicate modifier key name: {modifierName}");
                }
            }
        }

        /// <summary>
        /// Returns the name of the given input key name. The key name should be a valid name as defined by the input mappers.
        /// If it's not a valid key name, the input value is returned prefixed with "UNNAMEDKEY_".
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetKeyDisplayName(in string name)
        {
            foreach (var mapper in InputMappers)
            {
                string? displayName = mapper.GetKeyDisplayName(name);

                if (displayName != null)
                    return displayName;
            }

            return $"UNNAMEDKEY_{name}";
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

        public static bool RegisterAction(Plugin? plugin, string name, string uiName, params KeyBind[] defaultKeys)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (uiName == null) throw new ArgumentNullException(nameof(uiName));

            Bindings bindings = GetOrCreateBindings(plugin);
            return bindings.RegisterAction(name, uiName, defaultKeys);
        }

        public static void PressKey(in KeyBind keyBind)
        {
            if (!PressedKeyBinds.Add(keyBind))
                return;

            InvokeActionCallbacksForInputKey(keyBind, true);
        }

        public static void ReleaseKey(in KeyBind keyBind)
        {
            if (PressedKeyBinds.Remove(keyBind))
            {
                InvokeActionCallbacksForInputKey(keyBind, false);
            }

            // If there's no modifiers, release any other key binds that use the same key but also has modifiers
            if (keyBind.Modifiers.Count == 0)
            {
                List<KeyBind> toRemove = new();

                var keyBindCopy = keyBind;
                foreach (KeyBind key in PressedKeyBinds.Where(k => k.KeyName == keyBindCopy.KeyName))
                {
                    toRemove.Add(key);
                    InvokeActionCallbacksForInputKey(key, false);
                }

                foreach (KeyBind key in toRemove)
                    PressedKeyBinds.Remove(key);
            }
        }

        public static bool IsKeyDown(in KeyBind keyBind)
        {
            return PressedKeyBinds.Contains(keyBind);
        }

        /// <summary>
        /// Returns all currently pressed keys, starting with the last pressed key.
        /// </summary>
        public static IEnumerable<KeyBind> GetDownedKeys()
        {
            // Return pressed keys in reverse order so that the first item will be the last pressed key.
            foreach (var key in PressedKeyBinds.Reverse())
            {
                yield return key;
            }
        }

        /// <summary>
        /// Returns all keys that were pressed this frame, starting with the last pressed key.
        /// </summary>
        public static IEnumerable<KeyBind> GetPressedKeys()
        {
            foreach (var key in PressedKeyBinds.Reverse())
            {
                if (!LastPressedKeyBinds.Contains(key))
                    yield return key;
            }
        }

        /// <summary>
        /// Returns all keys that were released this frame, starting with the last released key.
        /// </summary>
        public static IEnumerable<KeyBind> GetReleasedKeys()
        {
            foreach (var key in LastPressedKeyBinds.Reverse())
            {
                if (!PressedKeyBinds.Contains(key))
                    yield return key;
            }
        }

        private static void InvokeActionCallbacksForInputKey(in KeyBind keyBind, bool pressed)
        {
            foreach (var bindings in PluginBindings.Values)
            {
                var actions = bindings.GetActionsForKey(keyBind);

                // If there's no actions bound to this key + modifiers check if there's any actions bound to this key without any modifiers
                if (actions.Count == 0)
                    actions = bindings.GetActionsForKey(new KeyBind(keyBind.KeyName));

                foreach (var action in actions)
                {
                    var callbacks = bindings.GetCallbacksForAction(action.Name);

                    foreach (var callback in callbacks)
                        callback.Invoke(pressed);
                }
            }
        }

        public static void CreateVanillaKeyBinds()
        {
            VanillaKeyBindRouter.InitializeActions();
        }

        /// <summary>
        /// Should be called after registering actions.
        /// This method loads the saved or default key bindings for the registered actions.
        /// </summary>
        public static void Initialize()
        {
            var loadedMappings = Persistence.LoadMappings();
            
            foreach (var kv in PluginBindings)
            {
                var plugin = kv.Key;
                var bindings = kv.Value;

                foreach (ActionInfo actionInfo in bindings.GetActions())
                {
                    // todo: add a unique name property for plugins that isn't normally renamed.
                    if (!loadedMappings.TryGetValue(plugin.Container.Info.Name!, out Dictionary<string, List<string>>? mappings))
                        mappings = new Dictionary<string, List<string>>();

                    if (mappings.TryGetValue(actionInfo.Name, out var keyNames))
                    {
                        if (keyNames.Count > 0)
                        {
                            foreach (string keyName in keyNames)
                            {
                                bindings.MapAction(keyName, actionInfo.Name);
                            }
                        }
                        else if (actionInfo.DefaultKeyBinds.Length > 0)
                        {
                            // Prevent the saved unbound action from being reset to default after the next restart
                            AddUnboundAction(plugin, actionInfo.Name);
                        }
                    }
                    else
                    {
                        foreach (var defaultKeyBind in actionInfo.DefaultKeyBinds)
                        {
                            bindings.MapAction(defaultKeyBind, actionInfo.Name);
                        }
                    }
                }
            }

            Persistence.SaveMappings(PluginBindings, UnboundActions);

            SteamFriends.OnGameOverlayActivated += OnGameOverlayToggled;
        }

        internal static void Save()
        {
            Persistence.SaveMappings(PluginBindings, UnboundActions);
        }

        private static void AddUnboundAction(Plugin plugin, string actionName)
        {
            if (!UnboundActions.TryGetValue(plugin, out var actions))
            {
                actions = new();
                UnboundActions[plugin] = actions;
            }

            if (!actions.Add(actionName))
                throw new InvalidOperationException("This action is already marked as unbound");
        }

        private static void RemoveUnboundAction(Plugin plugin, string actionName)
        {
            if (!UnboundActions.TryGetValue(plugin, out var actions))
                return;

            actions.Remove(actionName);

            if (actions.Count <= 0)
                UnboundActions.Remove(plugin);
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

            // Clear last pressed key binds and all items from PressedKeyBinds to it
            LastPressedKeyBinds.Clear();

            foreach (var key in PressedKeyBinds)
                LastPressedKeyBinds.Add(key);
            
            PressedKeys.Clear();
            ReleasedKeys.Clear();

            foreach (IInputMapper inputMapper in InputMappers)
            {
                inputMapper.Update();
                PressedKeys.AddRange(inputMapper.GetPressedKeys());
                ReleasedKeys.AddRange(inputMapper.GetReleasedKeys());
            }

            {
                List<string> modifiers = new();
                
                foreach (var keyBind in PressedKeyBinds)
                {
                    if (keyBind.Modifiers.Count == 0 && ModifierKeyNames.Contains(keyBind.KeyName))
                    {
                        modifiers.Add(keyBind.KeyName);
                    }
                }
                
                FireEvents(modifiers);
            }

            VanillaKeyBindRouter.Update();
        }

        private static void FireEvents(IReadOnlyList<string> modifierKeys)
        {
            foreach (string key in PressedKeys)
            {
                if (!ModifierKeyNames.Contains(key))
                    PressKey(new KeyBind(key, modifierKeys));
                else
                    PressKey(new KeyBind(key, ImmutableArray<string>.Empty));
            }

            foreach (string key in ReleasedKeys)
            {
                if (!ModifierKeyNames.Contains(key))
                    ReleaseKey(new KeyBind(key, modifierKeys));
                else
                    ReleaseKey(new KeyBind(key, ImmutableArray<string>.Empty));
            }
        }

        private static Bindings GetOrCreateBindings(Plugin? plugin)
        {
            plugin ??= Plugin.InternalPlugin;

            if (!PluginBindings.TryGetValue(plugin, out var result))
            {
                result = new(plugin);
                PluginBindings.Add(plugin, result);
            }
            
            return result;
        }

        public static Bindings GetBindings(Plugin? plugin)
        {
            return GetOrCreateBindings(plugin);
        }

        public static IReadOnlyDictionary<Plugin, Bindings> GetAllBindings()
        {
            return new ReadOnlyDictionary<Plugin, Bindings>(PluginBindings);
        }

        public static Dictionary<Plugin, IReadOnlyList<ActionInfo>> GetActionsForKeyBind(in KeyBind keyBind)
        {
            var result = new Dictionary<Plugin, IReadOnlyList<ActionInfo>>();

            foreach (var kv in PluginBindings)
            {
                var keyBinds = kv.Value.GetActionsForKey(keyBind);
                
                if (keyBinds.Count > 0)
                {
                    result.Add(kv.Key, keyBinds);
                }
            }
            
            return result;
        }
    }
}