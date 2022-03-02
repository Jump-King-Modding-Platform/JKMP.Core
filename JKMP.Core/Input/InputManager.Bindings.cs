using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Xna.Framework.Input;

namespace JKMP.Core.Input
{
    internal static partial class InputManager
    {
        public readonly struct ActionInfo
        {
            public readonly string Name;
            public readonly string UiName;
            public readonly KeyBind? DefaultKeyBind;

            public ActionInfo(string name, string uiName, KeyBind? defaultKeyBind)
            {
                Name = name;
                UiName = uiName;
                DefaultKeyBind = defaultKeyBind;
            }
        }
        
        [Flags]
        public enum ModifierKeys : short
        {
            None = 0,
            LeftShift = 1 << 0,
            RightShift = 1 << 1,
            LeftControl = 1 << 2,
            RightControl = 1 << 3,
            LeftAlt = 1 << 4,
            RightAlt = 1 << 5,
            LeftWin = 1 << 6,
            RightWin = 1 << 7,
        }

        public readonly struct KeyBind
        {
            public readonly ModifierKeys Modifiers;
            public readonly string KeyName;

            public KeyBind(string keyName, ModifierKeys modifiers)
            {
                KeyName = keyName?.ToLowerInvariant() ?? throw new ArgumentNullException(nameof(keyName));

                if (!ValidKeyNames.Contains(keyName))
                    throw new ArgumentException($"Invalid key name: {keyName}", nameof(keyName));
                
                Modifiers = modifiers;
            }
            
            public KeyBind(string bindName)
            {
                if (bindName == null) throw new ArgumentNullException(nameof(bindName));

                // Remove/ignore whitespace
                if (bindName.Contains(" "))
                    bindName = bindName.Replace(" ", "");
                
                if (!bindName.Contains("+"))
                {
                    Modifiers = ModifierKeys.None;
                    KeyName = bindName.ToLowerInvariant();
                    
                    if (!ValidKeyNames.Contains(KeyName))
                        throw new ArgumentException($"Invalid key name: {KeyName}", nameof(bindName));
                }
                else
                {
                    var parts = bindName.Split('+');
                    string[] modifiers = parts[0].Split(',');
                    KeyName = parts[1].ToLowerInvariant();

                    if (!ValidKeyNames.Contains(KeyName))
                        throw new ArgumentException($"Invalid key name: {KeyName}", nameof(bindName));

                    Modifiers = ModifierKeys.None;

                    foreach (string modifier in modifiers)
                    {
                        if (Enum.TryParse(modifier, ignoreCase: true, out ModifierKeys modifierKey))
                        {
                            Modifiers |= modifierKey;
                        }
                        else
                        {
                            throw new ArgumentException($"Unknown modifier key: {modifier}");
                        }
                    }
                }
            }

            public override string ToString()
            {
                return $"[{Modifiers} + {KeyName}]";
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((int)Modifiers * 397) ^ KeyName.GetHashCode();
                }
            }

            public static implicit operator KeyBind(string key) => new(key);
        }

        /// <summary>
        /// Holds bindings for a single plugin.
        /// </summary>
        public class Bindings
        {
            /// <summary>
            /// Maps input key names to one or several actions.
            /// </summary>
            private readonly Dictionary<KeyBind, HashSet<string>> mappings = new();
            
            /// <summary>
            /// Maps action names to one or several callbacks.
            /// </summary>
            private readonly Dictionary<string, List<PluginInput.BindActionCallback>> actionCallbacks = new();

            /// <summary>
            /// A set of all actions that have been registered.
            /// </summary>
            private readonly Dictionary<string, ActionInfo> registeredActions = new();

            public IReadOnlyCollection<string> GetActionsForKey(KeyBind keyBind)
            {
                if (!mappings.TryGetValue(keyBind, out var actions))
                    return Array.Empty<string>();

                return new ReadOnlyCollection<string>(actions.ToList());
            }

            /// <summary>
            /// Gets the mapped actions for a key. Note that this copies the list so it should not be called often due to GC pressure.
            /// </summary>
            /// <returns></returns>
            public IReadOnlyDictionary<KeyBind, IReadOnlyCollection<ActionInfo>> GetMappings()
            {
                return new ReadOnlyDictionary<KeyBind, IReadOnlyCollection<ActionInfo>>(
                    mappings.ToDictionary(
                        kv => kv.Key,
                        kv => (IReadOnlyCollection<ActionInfo>)new ReadOnlyCollection<ActionInfo>(kv.Value.Select(action => registeredActions[action]).ToList())
                    )
                );
            }
            
            /// <summary>
            /// Gets all registered actions.
            /// </summary>
            /// <returns></returns>
            public IReadOnlyCollection<ActionInfo> GetActions()
            {
                if (registeredActions.Count == 0)
                    return Array.Empty<ActionInfo>();
                
                return new ReadOnlyCollection<ActionInfo>(registeredActions.Values.ToList());
            }

            public IReadOnlyCollection<PluginInput.BindActionCallback> GetCallbacksForAction(string actionName)
            {
                if (!actionCallbacks.TryGetValue(actionName, out var callbacks))
                    return Array.Empty<PluginInput.BindActionCallback>();

                return new ReadOnlyCollection<PluginInput.BindActionCallback>(callbacks);
            }

            public bool RegisterAction(string name, string uiName, KeyBind? defaultKey)
            {
                if (name == null) throw new ArgumentNullException(nameof(name));
                if (uiName == null) throw new ArgumentNullException(nameof(uiName));
                
                if (registeredActions.ContainsKey(name))
                    return false;

                registeredActions.Add(name, new ActionInfo(name, uiName, defaultKey));
                return true;
            }

            public void AddActionCallback(string actionName, PluginInput.BindActionCallback callback)
            {
                if (actionName == null) throw new ArgumentNullException(nameof(actionName));
                if (callback == null) throw new ArgumentNullException(nameof(callback));

                if (!registeredActions.ContainsKey(actionName))
                    throw new ArgumentException($"Unknown action: {actionName}");

                var callbacks = GetActionCallbacks(actionName);
                callbacks.Add(callback);
            }

            public bool RemoveActionCallback(string actionName, PluginInput.BindActionCallback callback)
            {
                if (actionName == null) throw new ArgumentNullException(nameof(actionName));
                if (callback == null) throw new ArgumentNullException(nameof(callback));

                if (!registeredActions.ContainsKey(actionName))
                    throw new ArgumentException($"Unknown action: {actionName}");

                var callbacks = GetActionCallbacks(actionName);
                return callbacks.Remove(callback);
            }

            public void MapAction(KeyBind keyBind, string actionName)
            {
                if (!registeredActions.ContainsKey(actionName))
                    throw new ArgumentException($"Unknown action: {actionName}");

                var keyActions = GetOrCreateActionsForKey(keyBind);
                keyActions.Add(actionName);
            }

            public void UnmapAction(KeyBind keyBind, string actionName)
            {
                if (!registeredActions.ContainsKey(actionName))
                    throw new ArgumentException($"Unknown action: {actionName}");

                var keyActions = GetOrCreateActionsForKey(keyBind);
                keyActions.Remove(actionName);
            }

            private HashSet<string> GetOrCreateActionsForKey(KeyBind keyBind)
            {
                if (!mappings.TryGetValue(keyBind, out var result))
                {
                    result = new();
                    mappings[keyBind] = result;
                }

                return result;
            }

            private List<PluginInput.BindActionCallback> GetActionCallbacks(string actionName)
            {
                if (!actionCallbacks.TryGetValue(actionName, out var result))
                {
                    result = new();
                    actionCallbacks[actionName] = result;
                }

                return result;
            }
        }
    }
}