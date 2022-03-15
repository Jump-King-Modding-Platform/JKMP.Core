using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using JKMP.Core.Plugins;

namespace JKMP.Core.Input
{
    public static partial class InputManager
    {
        /// <summary>
        /// Represents an action that can be bound to a key.
        /// </summary>
        public readonly struct ActionInfo : IEquatable<ActionInfo>
        {
            /// <summary>
            /// The name (or id) of the action. Is unique per plugin but not necessarily globally.
            /// </summary>
            public readonly string Name;
            
            /// <summary>
            /// The display name of the action. For example one might want to display "Move Forward" instead of "MoveForward".
            /// </summary>
            public readonly string UiName;
            
            /// <summary>
            /// If true the action can only be triggered when the player is in-game and not paused.
            /// </summary>
            public readonly bool OnlyGameInput;
            
            /// <summary>
            /// The default key binds for the action. They're set when the action does not have any saved keybinds.
            /// </summary>
            public readonly KeyBind[] DefaultKeyBinds;

            /// <summary>
            /// Gets whether or not this action is currently being pressed down.
            /// </summary>
            [Pure]
            public bool IsPressed => IsActionPressed(this);

            internal readonly Plugin Owner;

            /// <summary>
            /// Instantiates a new <see cref="ActionInfo"/>.
            /// </summary>
            /// <param name="name">The name of the action.</param>
            /// <param name="uiName">The display name of the action.</param>
            /// <param name="onlyGameInput">Whether or not the action can only be triggered when in-game and not paused.</param>
            /// <param name="owner">The plugin that this action belongs to.</param>
            /// <param name="defaultKeyBinds">The default keybinds of this action.</param>
            internal ActionInfo(string name, string uiName, bool onlyGameInput, Plugin owner, params KeyBind[] defaultKeyBinds)
            {
                Name = name;
                UiName = uiName;
                OnlyGameInput = onlyGameInput;
                Owner = owner;
                DefaultKeyBinds = defaultKeyBinds;
            }

            /// <inheritdoc />
            public bool Equals(ActionInfo other)
            {
                if (Name != other.Name)
                    return false;

                if (UiName != other.UiName)
                    return false;

                if (OnlyGameInput != other.OnlyGameInput)
                    return false;
                
                if (Owner != other.Owner)
                    return false;

                if (DefaultKeyBinds != null && other.DefaultKeyBinds != null!)
                {
                    if (!DefaultKeyBinds.Equals(other.DefaultKeyBinds))
                        return false;
                }
                else if (DefaultKeyBinds != null || other.DefaultKeyBinds != null!)
                {
                    return false;
                }

                return true;
            }

            /// <inheritdoc />
            public override bool Equals(object? obj)
            {
                return obj is ActionInfo other && Equals(other);
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = Name.GetHashCode();
                    hashCode = (hashCode * 397) ^ UiName.GetHashCode();
                    hashCode = (hashCode * 397) ^ OnlyGameInput.GetHashCode();
                    hashCode = (hashCode * 397) ^ Owner.GetHashCode();
                    hashCode = (hashCode * 397) ^ DefaultKeyBinds.GetHashCode();
                    return hashCode;
                }
            }
            
            /// <summary>
            /// Compares two actions for equality.
            /// </summary>
            public static bool operator ==(ActionInfo a, ActionInfo b)
            {
                return a.Equals(b);
            }

            /// <summary>
            /// Compares two actions for inequality.
            /// </summary>
            public static bool operator !=(ActionInfo a, ActionInfo b)
            {
                return !(a == b);
            }
        }

        /// <summary>
        /// Represents a keybind. A keybind consists of a key name and zero or more modifiers.
        /// </summary>
        public readonly struct KeyBind : IEquatable<KeyBind>
        {
            /// <summary>
            /// Whether or not the keybind was created with a valid constructor.
            /// Will be false when the value is the default value.
            /// </summary>
            public readonly bool IsValid;
            
            /// <summary>
            /// The modifier key names that must also be pressed to trigger the bound action.
            /// </summary>
            public readonly ImmutableSortedSet<string>? Modifiers;
            
            /// <summary>
            /// The name of the key that must be pressed to trigger the bound action.
            /// </summary>
            public readonly string KeyName;

            /// <summary>
            /// Instantiates a new <see cref="KeyBind"/>.
            /// </summary>
            /// <param name="keyName">The name of the key.</param>
            /// <param name="modifiers">The names of the modifier keys.</param>
            /// <exception cref="ArgumentNullException">Thrown if KeyName is null</exception>
            /// <exception cref="ArgumentException">Thrown if the key name or any modifier keys are unknown.</exception>
            public KeyBind(string keyName, IEnumerable<string> modifiers)
            {
                KeyName = keyName?.ToLowerInvariant() ?? throw new ArgumentNullException(nameof(keyName));

                if (!ValidKeyNames.Contains(keyName))
                    throw new ArgumentException($"Invalid key name: {keyName}", nameof(keyName));

                Modifiers = modifiers.ToImmutableSortedSet();

                foreach (string modifier in Modifiers)
                {
                    if (!ModifierKeyNames.Contains(modifier))
                        throw new ArgumentException($"Unknown modifier key: {modifier}");
                }

                IsValid = true;
            }
            
            /// <summary>
            /// Instantiates a new <see cref="KeyBind"/> by parsing a string.
            /// </summary>
            /// <param name="bindName">
            /// The serialized bind, formatted like this: "leftshift,leftcontrol+w" or just "w".
            /// Whitespace is ignored.
            /// </param>
            /// <exception cref="ArgumentNullException">Thrown is bindName is null.</exception>
            /// <exception cref="ArgumentException">Thrown if the keyname or any of the modifier key names are unknown.</exception>
            public KeyBind(string bindName)
            {
                if (bindName == null) throw new ArgumentNullException(nameof(bindName));

                // Remove/ignore whitespace
                if (bindName.Contains(" "))
                    bindName = bindName.Replace(" ", "");
                
                if (!bindName.Contains("+"))
                {
                    Modifiers = ImmutableSortedSet<string>.Empty;
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

                    Modifiers = modifiers.ToImmutableSortedSet();

                    foreach (string modifier in modifiers)
                    {
                        if (!ModifierKeyNames.Contains(modifier))
                            throw new ArgumentException($"Unknown modifier key: {modifier}");
                    }
                }

                IsValid = true;
            }

            /// <inheritdoc />
            public override string ToString() => ToDisplayString();

            /// <summary>
            /// Returns the display string for this keybind.
            /// For example: "LShift + LCtrl + W" or just "W".
            /// </summary>
            public string ToDisplayString()
            {
                if (!IsValid)
                    return "Invalid Key";

                if (Modifiers!.Count == 0)
                    return GetKeyDisplayName(KeyName);
                
                StringBuilder builder = new();

                for (int i = 0; i < Modifiers.Count; ++i)
                {
                    builder.Append(GetKeyDisplayName(Modifiers[i]));

                    if (i < Modifiers.Count - 1)
                        builder.Append(", ");
                }

                builder.Append(" + ");
                builder.Append(GetKeyDisplayName(KeyName));

                return builder.ToString();
            }

            /// <summary>
            /// Returns a string representation of the key bind.
            /// For example "leftshift+a" or "leftcontrol,leftshift+a" or just "a" if there's no modifiers.
            /// </summary>
            /// <returns></returns>
            public string ToSerializedString()
            {
                if (!IsValid)
                    throw new InvalidOperationException("Can not serialize invalid KeyBind");

                if (Modifiers!.Count == 0)
                    return KeyName;

                StringBuilder builder = new();

                for (var i = 0; i < Modifiers.Count; ++i)
                {
                    string modifier = Modifiers[i];
                    builder.Append(modifier.ToLowerInvariant());

                    if (i < Modifiers.Count - 1)
                        builder.Append(",");
                }

                builder.Append("+");
                builder.Append(KeyName);

                return builder.ToString();
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = IsValid.GetHashCode();
                    hashCode = (hashCode * 397) ^ KeyName.GetHashCode();

                    if (Modifiers != null)
                    {
                        hashCode = (hashCode * 397) ^ Modifiers.Count;

                        // Use a for loop instead of foreach to get the most performance out of the loop
                        for (var i = 0; i < Modifiers.Count; ++i)
                        {
                            hashCode = (hashCode * 397) ^ Modifiers[i].GetHashCode();
                        }
                    }
                    else
                    {
                        hashCode = (hashCode * 397) ^ 0;
                    }

                    return hashCode;
                }
            }

            /// <inheritdoc />
            public bool Equals(KeyBind other)
            {
                if (IsValid != other.IsValid)
                    return false;
                
                if (KeyName != other.KeyName)
                    return false;

                if (Modifiers?.Count != other.Modifiers?.Count)
                    return false;

                if (Modifiers != null && other.Modifiers != null)
                {
                    for (int i = 0; i < Modifiers.Count; ++i)
                    {
                        if (!Modifiers[i].Equals(other.Modifiers[i]))
                            return false;
                    }
                }

                return true;
            }

            /// <inheritdoc />
            public override bool Equals(object? obj)
            {
                return obj is KeyBind other && Equals(other);
            }

            /// <summary>
            /// Implicitly converts a string to a <see cref="KeyBind"/>.
            /// Will throw an exception if the string is null or contains unknown key names.
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            public static implicit operator KeyBind(string key) => new(key);
            
            /// <summary>
            /// Compares two keybinds for equality.
            /// </summary>
            public static bool operator ==(KeyBind a, KeyBind b)
            {
                return a.Equals(b);
            }

            /// <summary>
            /// Compares two keybinds for inequality.
            /// </summary>
            public static bool operator !=(KeyBind a, KeyBind b)
            {
                return !(a == b);
            }
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

            internal Bindings(Plugin owner)
            {
                this.owner = owner;
            }

            /// <summary>
            /// Returns all actions that are bound to the given key.
            /// </summary>
            /// <param name="keyBind">The keybind that is bound to the actions that will be returned.</param>
            /// <exception cref="ArgumentException">Thrown is keyBind is invalid.</exception>
            public IReadOnlyList<ActionInfo> GetActionsForKey(in KeyBind keyBind)
            {
                if (!keyBind.IsValid)
                    throw new ArgumentException("KeyBind is invalid", nameof(keyBind));
                
                if (!mappings.TryGetValue(keyBind, out var actions))
                    return Array.Empty<ActionInfo>();

                return new ReadOnlyCollection<ActionInfo>(actions.Select(name => registeredActions[name]).ToList());
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

            /// <summary>
            /// Returns all callbacks that have been registered for the given action.
            /// </summary>
            /// <param name="actionName">The name of the action.</param>
            /// <returns></returns>
            public IReadOnlyList<PluginInput.BindActionCallback> GetCallbacksForAction(in string actionName)
            {
                if (!actionCallbacks.TryGetValue(actionName, out var callbacks))
                    return Array.Empty<PluginInput.BindActionCallback>();

                return new ReadOnlyCollection<PluginInput.BindActionCallback>(callbacks);
            }

            /// <summary>
            /// Returns all keys bound to the specified action.
            /// </summary>
            public IReadOnlyList<KeyBind> GetKeyBindsForAction(in string actionName)
            {
                var result = new List<KeyBind>();

                foreach (KeyValuePair<KeyBind, HashSet<string>> mapping in mappings)
                {
                    if (mapping.Value.Contains(actionName))
                        result.Add(mapping.Key);
                }

                return result.AsReadOnly();
            }
            
            /// <summary>
            /// Returns the action defined with the given name, if it exists. Otherwise, returns null.
            /// </summary>
            /// <param name="actionName">The name of the action</param>
            public ActionInfo? GetActionByName(string actionName)
            {
                return registeredActions.TryGetValue(actionName, out ActionInfo result) ? result : null;
            }

            internal ActionInfo RegisterAction(string name, string uiName, bool onlyGameInput, params KeyBind[] defaultKeys)
            {
                if (name == null) throw new ArgumentNullException(nameof(name));
                if (uiName == null) throw new ArgumentNullException(nameof(uiName));

                if (registeredActions.ContainsKey(name))
                    throw new ArgumentException($"An action with the name '{name}' already exists", nameof(name));

                var actionInfo = new ActionInfo(name, uiName, onlyGameInput, owner, defaultKeys);
                registeredActions.Add(name, actionInfo);
                return actionInfo;
            }

            internal void AddActionCallback(string actionName, PluginInput.BindActionCallback callback)
            {
                if (actionName == null) throw new ArgumentNullException(nameof(actionName));

                if (!registeredActions.TryGetValue(actionName, out ActionInfo actionInfo))
                {
                    throw new ArgumentException($"An action with the name '{actionName}' could not be found", nameof(actionName));
                }

                AddActionCallback(actionInfo, callback);
            }

            internal void AddActionCallback(ActionInfo action, PluginInput.BindActionCallback callback)
            {
                if (action == default) throw new ArgumentNullException(nameof(action), "Action cannot be the default struct value");
                if (callback == null) throw new ArgumentNullException(nameof(callback));

                if (!registeredActions.ContainsValue(action))
                    throw new ArgumentException($"Unknown action: {action.Name}");

                var callbacks = GetActionCallbacks(action.Name);
                callbacks.Add(callback);
            }

            internal bool RemoveActionCallback(string actionName, PluginInput.BindActionCallback callback)
            {
                if (actionName == null) throw new ArgumentNullException(nameof(actionName));

                if (!registeredActions.TryGetValue(actionName, out ActionInfo actionInfo))
                {
                    throw new ArgumentException($"An action with the name '{actionName}' could not be found", nameof(actionName));
                }

                return RemoveActionCallback(actionInfo, callback);
            }

            internal bool RemoveActionCallback(ActionInfo action, PluginInput.BindActionCallback callback)
            {
                if (action == default) throw new ArgumentNullException(nameof(action), "Action cannot be the default struct value");
                if (callback == null) throw new ArgumentNullException(nameof(callback));

                if (!registeredActions.ContainsValue(action))
                    throw new ArgumentException($"Unknown action: {action.Name}");

                var callbacks = GetActionCallbacks(action.Name);
                return callbacks.Remove(callback);
            }

            internal void MapAction(KeyBind keyBind, string actionName)
            {
                if (!keyBind.IsValid)
                    throw new ArgumentException($"KeyBind is invalid", nameof(keyBind));
                
                if (!registeredActions.ContainsKey(actionName))
                    throw new ArgumentException($"Unknown action: {actionName}");

                var keyActions = GetOrCreateActionsForKey(keyBind);
                keyActions.Add(actionName);

                RemoveUnboundAction(owner, actionName);
            }

            internal void UnmapAction(KeyBind keyBind, string actionName)
            {
                if (!keyBind.IsValid)
                    throw new ArgumentException($"KeyBind is invalid", nameof(keyBind));
                
                if (!registeredActions.ContainsKey(actionName))
                    throw new ArgumentException($"Unknown action: {actionName}");

                var keyActions = GetOrCreateActionsForKey(keyBind);
                keyActions.Remove(actionName);

                if (registeredActions[actionName].DefaultKeyBinds.Length > 0 && GetKeyBindsForAction(actionName).Count <= 0)
                {
                    AddUnboundAction(owner, actionName);
                }
            }

            internal void ClearMappings()
            {
                mappings.Clear();
            }

            private HashSet<string> GetOrCreateActionsForKey(in KeyBind keyBind)
            {
                if (!mappings.TryGetValue(keyBind, out var result))
                {
                    result = new();
                    mappings[keyBind] = result;
                }

                return result;
            }

            private List<PluginInput.BindActionCallback> GetActionCallbacks(in string actionName)
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