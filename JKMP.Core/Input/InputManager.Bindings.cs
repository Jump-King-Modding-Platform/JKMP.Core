using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using JKMP.Core.Plugins;
using Microsoft.Xna.Framework.Input;

namespace JKMP.Core.Input
{
    internal static partial class InputManager
    {
        public readonly struct ActionInfo : IEquatable<ActionInfo>
        {
            public readonly string Name;
            public readonly string UiName;
            public readonly KeyBind[] DefaultKeyBinds;

            public ActionInfo(string name, string uiName, params KeyBind[] defaultKeyBinds)
            {
                Name = name;
                UiName = uiName;
                DefaultKeyBinds = defaultKeyBinds;
            }

            public bool Equals(ActionInfo other)
            {
                return Name == other.Name && UiName == other.UiName && DefaultKeyBinds.Equals(other.DefaultKeyBinds);
            }

            public override bool Equals(object? obj)
            {
                return obj is ActionInfo other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = Name.GetHashCode();
                    hashCode = (hashCode * 397) ^ UiName.GetHashCode();
                    hashCode = (hashCode * 397) ^ DefaultKeyBinds.GetHashCode();
                    return hashCode;
                }
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

        public readonly struct KeyBind : IEquatable<KeyBind>
        {
            public readonly bool IsValid;
            public readonly ModifierKeys Modifiers;
            public readonly string KeyName;

            public KeyBind(string keyName, ModifierKeys modifiers)
            {
                KeyName = keyName?.ToLowerInvariant() ?? throw new ArgumentNullException(nameof(keyName));

                if (!ValidKeyNames.Contains(keyName))
                    throw new ArgumentException($"Invalid key name: {keyName}", nameof(keyName));
                
                Modifiers = modifiers;
                IsValid = true;
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

                IsValid = true;
            }

            public override string ToString()
            {
                if (Modifiers != ModifierKeys.None)
                    return $"{Modifiers} + {KeyName}";

                return KeyName;
            }

            public string ToDisplayString()
            {
                if (Modifiers == ModifierKeys.None)
                    return GetKeyBindingName(KeyName);

                ModifierKeys modifiers = Modifiers;
                StringBuilder builder = new();

                foreach (ModifierKeys modifier in Enum.GetValues(typeof(ModifierKeys)).Cast<ModifierKeys>().Where(modifier => (modifiers & modifier) != 0))
                {
                    Keys modifierKey = modifier switch
                    {
                        ModifierKeys.LeftShift => Keys.LeftShift,
                        ModifierKeys.RightShift => Keys.RightShift,
                        ModifierKeys.LeftControl => Keys.LeftControl,
                        ModifierKeys.RightControl => Keys.RightControl,
                        ModifierKeys.LeftAlt => Keys.LeftAlt,
                        ModifierKeys.RightAlt => Keys.RightAlt,
                        ModifierKeys.LeftWin => Keys.LeftWindows,
                        ModifierKeys.RightWin => Keys.RightWindows,
                        _ => throw new ArgumentOutOfRangeException(nameof(modifier), "Unexpected modifier key")
                    };

                    builder.Append(GetKeyDisplayName(modifierKey));
                    modifiers &= ~modifier;

                    if (modifiers != ModifierKeys.None)
                        builder.Append(", ");
                }

                builder.Append(" + ");
                builder.Append(GetKeyBindingName(KeyName));

                return builder.ToString();
            }

            /// <summary>
            /// Returns a string representation of the key bind.
            /// For example "leftshift+a" or "leftcontrol,leftshift+a" or just "a" if there's no modifiers.
            /// </summary>
            /// <returns></returns>
            public string ToSerializedString()
            {
                if (Modifiers == ModifierKeys.None)
                    return KeyName;

                ModifierKeys modifiers = Modifiers;
                StringBuilder builder = new();
                
                // Serialize modifiers to a string array and add them to the builder separated by a comma
                foreach (var modifier in Enum.GetValues(typeof(ModifierKeys)).Cast<ModifierKeys>().Where(modifier => (modifiers & modifier) != 0))
                {
                    builder.Append(modifier.ToString().ToLowerInvariant());
                    modifiers &= ~modifier;

                    if (modifiers != ModifierKeys.None)
                        builder.Append(",");
                }

                builder.Append("+");
                builder.Append(KeyName);

                return builder.ToString();
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((int)Modifiers * 397) ^ KeyName.GetHashCode();
                }
            }

            public bool Equals(KeyBind other)
            {
                return Modifiers == other.Modifiers && KeyName == other.KeyName;
            }

            public override bool Equals(object? obj)
            {
                return obj is KeyBind other && Equals(other);
            }

            public static implicit operator KeyBind(string key) => new(key);
        }

        /// <summary>
        /// Holds bindings for a single plugin.
        /// </summary>
        public class Bindings
        {
            private readonly Plugin owner;

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

            public Bindings(Plugin owner)
            {
                this.owner = owner;
            }

            public IReadOnlyList<string> GetActionsForKey(KeyBind keyBind)
            {
                if (!mappings.TryGetValue(keyBind, out var actions))
                    return Array.Empty<string>();

                return new ReadOnlyCollection<string>(actions.ToList());
            }

            /// <summary>
            /// Gets the mapped actions for a key. Note that this copies the list so it should not be called often due to GC pressure.
            /// </summary>
            /// <returns></returns>
            public IReadOnlyDictionary<KeyBind, IReadOnlyList<ActionInfo>> GetMappings()
            {
                return new ReadOnlyDictionary<KeyBind, IReadOnlyList<ActionInfo>>(
                    mappings.ToDictionary(
                        kv => kv.Key,
                        kv => (IReadOnlyList<ActionInfo>)new ReadOnlyCollection<ActionInfo>(kv.Value.Select(action => registeredActions[action]).ToList())
                    )
                );
            }
            
            /// <summary>
            /// Gets all registered actions.
            /// </summary>
            /// <returns></returns>
            public IReadOnlyList<ActionInfo> GetActions()
            {
                if (registeredActions.Count == 0)
                    return Array.Empty<ActionInfo>();
                
                return new ReadOnlyCollection<ActionInfo>(registeredActions.Values.ToList());
            }

            public IReadOnlyList<PluginInput.BindActionCallback> GetCallbacksForAction(string actionName)
            {
                if (!actionCallbacks.TryGetValue(actionName, out var callbacks))
                    return Array.Empty<PluginInput.BindActionCallback>();

                return new ReadOnlyCollection<PluginInput.BindActionCallback>(callbacks);
            }

            /// <summary>
            /// Returns all keys bound to the specified action.
            /// </summary>
            public IReadOnlyList<KeyBind> GetKeyBindsForAction(string actionName)
            {
                var result = new List<KeyBind>();

                foreach (KeyValuePair<KeyBind, HashSet<string>> mapping in mappings)
                {
                    if (mapping.Value.Contains(actionName))
                        result.Add(mapping.Key);
                }

                return result.AsReadOnly();
            }

            public bool RegisterAction(string name, string uiName, params KeyBind[] defaultKeys)
            {
                if (name == null) throw new ArgumentNullException(nameof(name));
                if (uiName == null) throw new ArgumentNullException(nameof(uiName));
                
                if (registeredActions.ContainsKey(name))
                    return false;

                registeredActions.Add(name, new ActionInfo(name, uiName, defaultKeys));
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

                RemoveUnboundAction(owner, actionName);
            }

            public void UnmapAction(KeyBind keyBind, string actionName)
            {
                if (!registeredActions.ContainsKey(actionName))
                    throw new ArgumentException($"Unknown action: {actionName}");

                var keyActions = GetOrCreateActionsForKey(keyBind);
                keyActions.Remove(actionName);

                if (keyActions.Count <= 0 && registeredActions[actionName].DefaultKeyBinds.Length > 0)
                {
                    AddUnboundAction(owner, actionName);
                }
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