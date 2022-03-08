using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace JKMP.Core.Input.InputMappers
{
    internal class MouseMapper : IInputMapper
    {
        public ISet<string> ModifierKeys => new HashSet<string>();

        public ISet<string> ValidKeyNames { get; } = new HashSet<string>
        {
            "mouse1",
            "mouse2",
            "mouse3",
            "mouse4",
            "mouse5"
        };

        private MouseState currentState;
        private MouseState lastState;

        public string? GetKeyDisplayName(in string keyName)
        {
            return keyName switch
            {
                "mouse1" => "LMB",
                "mouse2" => "RMB",
                "mouse3" => "MMB",
                "mouse4" => "Mouse 4",
                "mouse5" => "Mouse 5",
                _ => null
            };
        }

        public IEnumerable<string> GetPressedKeys()
        {
            if (currentState == default || lastState == default)
                yield break;

            if (currentState.LeftButton == ButtonState.Pressed && lastState.LeftButton == ButtonState.Released)
                yield return "mouse1";

            if (currentState.RightButton == ButtonState.Pressed && lastState.RightButton == ButtonState.Released)
                yield return "mouse2";

            if (currentState.MiddleButton == ButtonState.Pressed && lastState.MiddleButton == ButtonState.Released)
                yield return "mouse3";

            if (currentState.XButton1 == ButtonState.Pressed && lastState.XButton1 == ButtonState.Released)
                yield return "mouse4";

            if (currentState.XButton2 == ButtonState.Pressed && lastState.XButton2 == ButtonState.Released)
                yield return "mouse5";
        }

        public IEnumerable<string> GetReleasedKeys()
        {
            if (currentState == default || lastState == default)
                yield break;

            if (currentState.LeftButton == ButtonState.Released && lastState.LeftButton == ButtonState.Pressed)
                yield return "mouse1";

            if (currentState.RightButton == ButtonState.Released && lastState.RightButton == ButtonState.Pressed)
                yield return "mouse2";

            if (currentState.MiddleButton == ButtonState.Released && lastState.MiddleButton == ButtonState.Pressed)
                yield return "mouse3";

            if (currentState.XButton1 == ButtonState.Released && lastState.XButton1 == ButtonState.Pressed)
                yield return "mouse4";

            if (currentState.XButton2 == ButtonState.Released && lastState.XButton2 == ButtonState.Pressed)
                yield return "mouse5";
        }

        public void Update()
        {
            lastState = currentState;
            currentState = Mouse.GetState();
        }
    }
}