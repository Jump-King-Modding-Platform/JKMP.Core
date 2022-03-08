using System;
using System.Collections.Generic;
using System.Linq;
using JumpKing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace JKMP.Core.Input.InputMappers
{
    internal partial class ControllerMapper : IInputMapper
    {
        private static readonly Dictionary<Buttons, string> KeyMap = new();
        private static readonly HashSet<Buttons> AllButtons;

        static ControllerMapper()
        {
            AllButtons = new HashSet<Buttons>(Enum.GetValues(typeof(Buttons)).Cast<Buttons>());

            foreach (Buttons button in AllButtons)
            {
                string? name = GetButtonName(button);

                if (name != null)
                {
                    KeyMap.Add(button, name);
                }
            }
        }

        public ISet<string> ModifierKeys { get; }

        public ISet<string> ValidKeyNames { get; }

        private GamePadState currentState;
        private GamePadState lastState;

        public ControllerMapper()
        {
            ValidKeyNames = new HashSet<string>();

            foreach (var kv in KeyMap)
                ValidKeyNames.Add(kv.Value);

            ModifierKeys = new HashSet<string>
            {
                KeyMap[Buttons.LeftTrigger],
                KeyMap[Buttons.LeftShoulder],
                KeyMap[Buttons.RightTrigger],
                KeyMap[Buttons.RightShoulder],
            };
        }
        
        public IEnumerable<string> GetPressedKeys()
        {
            if (!Game1.instance.IsActive)
                yield break;

            foreach (Buttons button in AllButtons)
            {
                if (currentState.IsButtonDown(button) && !lastState.IsButtonDown(button))
                {
                    yield return KeyMap[button];
                }
            }
        }
        
        public IEnumerable<string> GetReleasedKeys()
        {
            if (!Game1.instance.IsActive)
                yield break;
            
            foreach (Buttons button in AllButtons)
            {
                if (lastState.IsButtonDown(button) && !currentState.IsButtonDown(button))
                {
                    yield return KeyMap[button];
                }
            }
        }

        public void Update()
        {
            lastState = currentState;
            currentState = GamePad.GetState(PlayerIndex.One, GamePadDeadZone.Circular);
        }
    }
}