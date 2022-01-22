using System;
using JKMP.Core.UI.MenuFields;

namespace JKMP.Core.Configuration.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SettingsMenuAttribute : Attribute
    {
        public string Name { get; }

        public SettingsMenuAttribute(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public abstract class SettingsOptionAttribute : Attribute
    {
        public string? Name { get; set; }
    }

    public class TextFieldAttribute : SettingsOptionAttribute
    {
        public TextVisibility Visibility { get; set; } = TextVisibility.Visible;
        public int MaxLength { get; set; }
        public bool TrimWhitespace { get; set; } = true;
    }
    
    public class SliderFieldAttribute : SettingsOptionAttribute
    {
        public float MinValue { get; set; } = 0;
        public float MaxValue { get; set; } = 1;
        public float StepSize { get; set; } = 0.1f;
    }

    public class CheckboxFieldAttribute : SettingsOptionAttribute
    {
    }
}