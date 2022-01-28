using System;
using JKMP.Core.Configuration.Attributes.PropertyCreators;
using JKMP.Core.Configuration.Attributes.PropertyCreators.Implementations;
using JKMP.Core.UI.MenuFields;

namespace JKMP.Core.Configuration.Attributes
{
    /// <summary>
    /// This attribute is used to specify a <see cref="ConfigPropertyCreator"/> that is used to create a menu field for a config property.
    /// For an example look at the <see cref="TextFieldAttribute"/>. It has a <see cref="TextFieldCreator"/> as creator specified in an attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SettingsOptionCreatorAttribute : Attribute
    {
        /// <summary>
        /// The type of the creator.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Instantiates a new instance of <see cref="SettingsOptionCreatorAttribute"/>.
        /// </summary>
        /// <param name="type">The type of the creator. Note that it needs to inherit from <see cref="ConfigPropertyCreator"/> or an exception will be raised.</param>
        /// <exception cref="ArgumentNullException">Thrown if type is null.</exception>
        public SettingsOptionCreatorAttribute(Type type)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }
    }

    /// <summary>
    /// An attribute that is used to specify a property should be visible in a settings menu.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public abstract class SettingsOptionAttribute : Attribute
    {
        /// <summary>
        /// The name of the property. If null the property name will be used.
        /// </summary>
        public string? Name { get; set; }
    }

    /// <summary>
    /// A menu field that is used to display a text field. Supported property types are <see cref="string"/>.
    /// </summary>
    [SettingsOptionCreator(typeof(TextFieldCreator))]
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class TextFieldAttribute : SettingsOptionAttribute
    {
        /// <summary>
        /// The visibility of the text. Visible by default.
        /// Hidden means the text will be masked.
        /// </summary>
        public TextVisibility Visibility { get; set; } = TextVisibility.Visible;
        
        /// <summary>
        /// The maximum length of the text. Can not be 0.
        /// </summary>
        public int MaxLength { get; set; }
        
        /// <summary>
        /// If true the text will be trimmed from whitespace when changed. True by default.
        /// </summary>
        public bool TrimWhitespace { get; set; } = true;
    }
    
    /// <summary>
    /// A menu field that is used to display a slider. Supported property types are <see cref="float"/> and <see cref="int"/>
    /// Note that the underlying slider value is always a <see cref="float"/>.
    /// </summary>
    [SettingsOptionCreator(typeof(SliderFieldCreator))]
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SliderFieldAttribute : SettingsOptionAttribute
    {
        /// <summary>
        /// The minimum value of the slider. 0 by default.
        /// </summary>
        public float MinValue { get; set; } = 0;
        
        /// <summary>
        /// The maximum value of the slider. 1 by default.
        /// </summary>
        public float MaxValue { get; set; } = 1;
        
        /// <summary>
        /// The amount to increase or decrease the slider value by when the user presses the left or right arrow keys. 0.1 by default.
        /// </summary>
        public float StepSize { get; set; } = 0.1f;
    }

    /// <summary>
    /// A menu field that is used to display a checkbox. Supported property types are <see cref="bool"/>.
    /// </summary>
    [SettingsOptionCreator(typeof(CheckboxFieldCreator))]
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CheckboxFieldAttribute : SettingsOptionAttribute
    {
    }
}