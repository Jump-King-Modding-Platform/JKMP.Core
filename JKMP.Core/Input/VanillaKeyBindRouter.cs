using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using JKMP.Core.Input.InputMappers;
using JKMP.Core.Logging;
using JumpKing.Controller;
using JumpKing.SaveThread;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace JKMP.Core.Input
{
    /// <summary>
    /// Forwards input from InputManager to the vanilla ControllerManager with the help of the patches in InputPatches.cs.
    /// </summary>
    internal static class VanillaKeyBindRouter
    {
        private static readonly Dictionary<string, bool> KeyStates = new();
        private static readonly Type PadStateType = typeof(PadState);
        private static PadState lastState;
        private static PadState currentState;
        
        // This field is only written to with reflection so we disable the warning about field never being assigned.
        #pragma warning disable CS0649
        private static PadState pressedState;
        #pragma warning restore CS0649

        private static readonly HashSet<string> GameOnlyKeys = new()
        {
            // JK+ keys
            "boots",
            "snake",
            "restart",
            "loadPosition",
            "savePosition"
        };

        public static void InitializeActions()
        {
            // Holds <fieldName, (uiName, keyName)> pairs
            Dictionary<string, (string uiName, string[] keyName, bool onlyGameInput)> vanillaBinds = new();
            
            // Get default binds
            PadBinding keyboard = new KeyboardPad().GetDefaultBind();
            PadBinding controller = new XboxPad(PlayerIndex.One).GetDefaultBind();

            // Have to use reflection to iterate the fields due to jump king+ potentially being installed which adds more fields.
            var fields = typeof(PadBinding).GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (FieldInfo field in fields)
            {
                if (field.FieldType != typeof(int[]))
                    continue;
                
                var keyboardValue = (int[])field.GetValue(keyboard);
                var controllerValue = (int[])field.GetValue(controller);

                List<string> keyNames = new();
                
                for (int i = 0; i < keyboardValue.Length; i++)
                {
                    Keys key = (Keys)keyboardValue[i];
                    string? keyName = KeyboardMapper.KeyMap.ContainsKey(key) ? KeyboardMapper.KeyMap[key] : null;

                    if (keyName != null)
                    {
                        keyNames.Add(keyName);
                    }
                }

                for (int i = 0; i < controllerValue.Length; ++i)
                {
                    Buttons button = (Buttons)controllerValue[i];
                    string? keyName = ControllerMapper.KeyMap.ContainsKey(button) ? ControllerMapper.KeyMap[button] : null;

                    if (keyName != null)
                    {
                        keyNames.Add(keyName);
                    }
                }

                vanillaBinds.Add(field.Name, (PrettifyFieldName(field.Name), keyNames.ToArray(), GameOnlyKeys.Contains(field.Name)));
                KeyStates[field.Name] = false;
            }

            foreach (var kv in vanillaBinds)
            {
                string fieldName = kv.Key;
                (string uiName, string[] keyNames, bool onlyGameInput) = kv.Value;
                
                InputManager.RegisterAction(plugin: null, fieldName, uiName, onlyGameInput, defaultKeys: keyNames.Select(k => (InputManager.KeyBind)k).ToArray());
                InputManager.BindAction(null, fieldName, pressed => OnKeyToggled(fieldName, pressed));
            }
        }

        private static void OnKeyToggled(string name, bool pressed)
        {
            KeyStates[name] = pressed;
        }

        /// <summary>
        /// Converts a string such as 'moveLeft' to 'Move left'.
        /// </summary>
        /// <param name="keyName"></param>
        /// <returns></returns>
        private static string PrettifyFieldName(string keyName)
        {
            switch (keyName.Length)
            {
                case 0: return string.Empty;
                case 1: return keyName.ToUpper();
            }

            var sb = new StringBuilder();

            sb.Append(keyName[0].ToString().ToUpper());
            bool lastWasUpper = false;
            
            for (int i = 1; i < keyName.Length; ++i)
            {
                if (char.IsUpper(keyName[i]))
                {
                    if (!lastWasUpper)
                    {
                        sb.Append(' ');
                    }

                    sb.Append(keyName[i].ToString().ToLowerInvariant());
                    lastWasUpper = true;
                }
                else
                {
                    lastWasUpper = false;
                    sb.Append(keyName[i]);
                }
            }

            return sb.ToString();
        }

        public static PadState GetState()
        {
            return currentState;
        }

        public static PadState GetPressedState()
        {
            return pressedState;
        }

        internal static void Update()
        {
            lastState = currentState;
            currentState = CreateState();
            TypedReference lastStateRef = __makeref(lastState);
            TypedReference pressedStateRef = __makeref(pressedState);

            // Update pressed state
            foreach (KeyValuePair<string, bool> kv in KeyStates)
            {
                FieldInfo fieldInfo = PadStateType.GetField(kv.Key);
                bool isPressed = KeyStates[kv.Key];
                bool wasPressed = (bool)fieldInfo.GetValueDirect(lastStateRef);

                fieldInfo.SetValueDirect(pressedStateRef, isPressed && !wasPressed);
            }
        }

        private static PadState CreateState()
        {
            PadState padState = new();
            TypedReference reference = __makeref(padState);

            foreach (var kv in KeyStates)
            {
                var field = PadStateType.GetField(kv.Key);
                field.SetValueDirect(reference, kv.Value);
            }

            return padState;
        }
    }
}