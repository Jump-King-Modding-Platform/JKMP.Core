using System;
using JKMP.Core.Configuration.Attributes.PropertyCreators.Implementations;
using JKMP.Core.UI.MenuFields;

namespace JKMP.Core.Configuration.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SettingsOptionCreatorAttribute : Attribute
    {
        public Type Type { get; }

        public SettingsOptionCreatorAttribute(Type type)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public abstract class SettingsOptionAttribute : Attribute
    {
        public string? Name { get; set; }
    }

    
    [SettingsOptionCreator(typeof(TextFieldCreator))]
    public class TextFieldAttribute : SettingsOptionAttribute
    {
        public TextVisibility Visibility { get; set; } = TextVisibility.Visible;
        public int MaxLength { get; set; }
        public bool TrimWhitespace { get; set; } = true;
    }
    
    [SettingsOptionCreator(typeof(SliderFieldCreator))]
    public class SliderFieldAttribute : SettingsOptionAttribute
    {
        public float MinValue { get; set; } = 0;
        public float MaxValue { get; set; } = 1;
        public float StepSize { get; set; } = 0.1f;
    }

    [SettingsOptionCreator(typeof(CheckboxFieldCreator))]
    public class CheckboxFieldAttribute : SettingsOptionAttribute
    {
    }
}