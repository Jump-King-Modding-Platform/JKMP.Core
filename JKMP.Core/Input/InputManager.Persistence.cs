using System.Collections.Generic;
using System.IO;
using System.Linq;
using JKMP.Core.Plugins;
using Newtonsoft.Json;

namespace JKMP.Core.Input
{
    internal static partial class InputManager
    {
        /// <summary>
        /// Handles loading and saving of custom keybinds
        /// </summary>
        private static class Persistence
        {
            const string FilePath ="./JKMP/Keybinds.json";
            
            public static void SaveMappings(Dictionary<Plugin, Bindings> pluginBindings, Dictionary<Plugin, HashSet<string>> unboundActions)
            {
                Dictionary<string, Dictionary<string, List<string>>> pluginMappings = new();

                foreach (var kv in pluginBindings)
                {
                    var plugin = kv.Key;
                    var bindings = kv.Value;
                    var mappings = new Dictionary<string, List<string>>(); // key is action name, value is array of bound keys

                    // Right now we have dictionary of <keyName, actions[]> so we have to convert it to <action, keyName[]>
                    foreach (var mapping in bindings.GetMappings())
                    {
                        var keyBind = mapping.Key;
                        var actions = mapping.Value;

                        foreach (var action in actions)
                        {
                            if (!mappings.TryGetValue(action.Name, out var keys))
                            {
                                keys = new();
                                mappings[action.Name] = keys;
                            }

                            keys.Add(keyBind.ToSerializedString());
                        }
                    }

                    pluginMappings[plugin.Info.Name!] = mappings;
                }

                foreach (var kv in unboundActions)
                {
                    var plugin = kv.Key;
                    var actions = kv.Value;

                    var mappings = pluginMappings[plugin.Info.Name!];

                    foreach (string actionName in actions)
                    {
                        mappings[actionName] = new List<string>();
                    }
                }

                var json = JsonConvert.SerializeObject(pluginMappings, Formatting.Indented);
                File.WriteAllText(FilePath, json);
            }

            /// <summary>
            /// Returns a dictionary where the key is the action and the value is a list of all the keys bound to that action
            /// </summary>
            public static Dictionary<string, Dictionary<string, List<string>>> LoadMappings()
            {
                Dictionary<string, Dictionary<string, List<string>>>? mappings = null;

                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);

                    try
                    {
                        mappings = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<string>>>>(json);
                    }
                    catch (JsonSerializationException)
                    {
                        mappings = null;
                    }
                }

                return mappings ?? new();
            }
        }
    }
}