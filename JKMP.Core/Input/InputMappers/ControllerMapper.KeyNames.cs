using Microsoft.Xna.Framework.Input;

namespace JKMP.Core.Input.InputMappers
{
    internal partial class ControllerMapper
    {
        private static string? GetButtonName(Buttons button)
        {
            return button switch
            {
                Buttons.DPadUp => "xpad-dpad-up",
                Buttons.DPadDown => "xpad-dpad-down",
                Buttons.DPadLeft => "xpad-dpad-left",
                Buttons.DPadRight => "xpad-dpad-right",
                Buttons.Start => "xpad-start",
                Buttons.Back => "xpad-back",
                Buttons.LeftStick => "xpad-lsb",
                Buttons.RightStick => "xpad-rsb",
                Buttons.LeftShoulder => "xpad-lb",
                Buttons.RightShoulder => "xpad-rb",
                Buttons.A => "xpad-a",
                Buttons.B => "xpad-b",
                Buttons.X => "xpad-x",
                Buttons.Y => "xpad-y",
                Buttons.RightTrigger => "xpad-rt",
                Buttons.LeftTrigger => "xpad-lt",
                Buttons.RightThumbstickLeft => "xpad-rs-left",
                Buttons.RightThumbstickUp => "xpad-rs-up",
                Buttons.RightThumbstickRight => "xpad-rs-right",
                Buttons.RightThumbstickDown => "xpad-rs-down",
                Buttons.LeftThumbstickLeft => "xpad-ls-left",
                Buttons.LeftThumbstickUp => "xpad-ls-up",
                Buttons.LeftThumbstickRight => "xpad-ls-right",
                Buttons.LeftThumbstickDown => "xpad-ls-down",
                _ => null,
            };
        }
        
        public string? GetKeyDisplayName(in string keyName)
        {
            return keyName switch
            {
                "xpad-rsb" => "Right stick button",
                "xpad-lsb" => "Left stick button",
                "xpad-rb" => "RB",
                "xpad-rt" => "RT",
                "xpad-lb" => "LB",
                "xpad-lt" => "LT",
                "xpad-y" => "XPad Y",
                "xpad-a" => "XPad A",
                "xpad-x" => "XPad X",
                "xpad-b" => "XPad B",
                "xpad-dpad-left" => "D-Pad left",
                "xpad-dpad-up" => "D-Pad up",
                "xpad-dpad-right" => "D-Pad right",
                "xpad-dpad-down" => "D-Pad down",
                "xpad-ls-left" => "LS left",
                "xpad-ls-up" => "LS up",
                "xpad-ls-right" => "LS right",
                "xpad-ls-down" => "LSdown",
                "xpad-rs-left" => "RS left",
                "xpad-rs-up" => "RS up",
                "xpad-rs-right" => "RS right",
                "xpad-rs-down" => "RS down",
                "xpad-back" => "XPad Back",
                "xpad-start" => "XPad Start",
                _ => null
            };
        }
    }
}