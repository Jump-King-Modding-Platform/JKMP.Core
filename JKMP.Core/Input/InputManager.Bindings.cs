using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace JKMP.Core.Input
{
    internal static partial class InputManager
    {
        public readonly struct ActionInfo
        {
            public readonly string Name;
            public readonly string UiName;
            public readonly string? DefaultKey;

            public ActionInfo(string name, string uiName, string? defaultKey)
            {
                Name = name;
                UiName = uiName;
                DefaultKey = defaultKey;
            }
        }

        /// <summary>
        /// Holds bindings for a single plugin.
        /// </summary>
        private class Bindings
        {
            /// <summary>
            /// Maps input key names to one or several actions.
            /// </summary>
            private readonly Dictionary<string, HashSet<string>> mappings = new();
            
            /// <summary>
            /// Maps action names to one or several callbacks.
            /// </summary>
            private readonly Dictionary<string, List<PluginInput.BindActionCallback>> actionCallbacks = new();

            /// <summary>
            /// A set of all actions that have been registered.
            /// </summary>
            private readonly Dictionary<string, ActionInfo> registeredActions = new();

            public IReadOnlyCollection<string> GetActionsForKey(string keyName)
            {
                if (!ValidKeyNames.Contains(keyName))
                    throw new ArgumentException($"Invalid key name: {keyName}");
                
                if (!mappings.TryGetValue(keyName, out var actions))
                    return Array.Empty<string>();

                return new ReadOnlyCollection<string>(actions.ToList());
            }

            /// <summary>
            /// Gets the mapped actions for a key. Note that this copies the list so it should not be called often due to GC pressure.
            /// </summary>
            /// <returns></returns>
            public IReadOnlyDictionary<string, IReadOnlyCollection<ActionInfo>> GetMappings()
            {
                return new ReadOnlyDictionary<string, IReadOnlyCollection<ActionInfo>>(
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

            public bool RegisterAction(string name, string uiName, string? defaultKey)
            {
                if (name == null) throw new ArgumentNullException(nameof(name));
                if (uiName == null) throw new ArgumentNullException(nameof(uiName));

                if (defaultKey != null && !InputManager.ValidKeyNames.Contains(defaultKey))
                    throw new ArgumentException($"Invalid default key name: {defaultKey}");
                
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

            public void MapAction(string keyName, string actionName)
            {
                if (!ValidKeyNames.Contains(keyName))
                    throw new ArgumentException($"Invalid key name: {keyName}");

                if (!registeredActions.ContainsKey(actionName))
                    throw new ArgumentException($"Invalid action name: {actionName}");

                var keyActions = GetOrCreateActionsForKey(keyName);
                keyActions.Add(actionName);
            }

            public void UnmapAction(string keyName, string actionName)
            {
                if (!ValidKeyNames.Contains(keyName))
                    throw new ArgumentException($"Invalid key name: {keyName}");

                if (!registeredActions.ContainsKey(actionName))
                    throw new ArgumentException($"Invalid action name: {actionName}");

                var keyActions = GetOrCreateActionsForKey(keyName);
                keyActions.Remove(keyName);
            }

            private HashSet<string> GetOrCreateActionsForKey(string keyName)
            {
                if (!mappings.TryGetValue(keyName, out var result))
                {
                    result = new();
                    mappings[keyName] = result;
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