using System;
using System.Collections.Generic;
using System.Reflection;
using JumpKing.PauseMenu;
using JumpKing.PauseMenu.BT;
using JumpKing.Util;

namespace JKMP.Core.Configuration.Attributes.PropertyCreators
{
    /// <summary>
    /// A property creator that creates a property field for a config property.
    /// Note that you should inherit <see cref="ConfigPropertyCreator{T}"/> and not this class if you don't want to convert the attribute to the correct type.
    /// </summary>
    public abstract class ConfigPropertyCreator
    {
        /// <summary>
        /// Invoked when the value changes.
        /// </summary>
        public Action<object>? ValueChanged { get; set; }

        /// <summary>
        /// The supported types that the creator can read/write to the property.
        /// </summary>
        public abstract ICollection<Type> SupportedTypes { get; }

        /// <summary>
        /// Creates the property field for the given config property.
        /// </summary>
        /// <param name="config">The target config this field belongs to.</param>
        /// <param name="fieldName">The name of this field.</param>
        /// <param name="propertyInfo">The <see cref="PropertyInfo"/> of the property that holds the value of this field.</param>
        /// <param name="attribute">The values of the <see cref="SettingsOptionAttribute"/> on the field's property.</param>
        /// <param name="drawables">The list of drawables. New menus have to be added to this list. <see cref="IMenuItem"/>'s does not.</param>
        public abstract IMenuItem CreateField(object config, string fieldName, PropertyInfo propertyInfo, SettingsOptionAttribute attribute, List<IDrawable> drawables);
    }

    /// <summary>
    /// A property creator that creates a property field for a config property.
    /// </summary>
    /// <typeparam name="T">The type of the property's <see cref="SettingsOptionAttribute"/> that we're creating a field for.</typeparam>
    public abstract class ConfigPropertyCreator<T> : ConfigPropertyCreator where T : SettingsOptionAttribute
    {
        /// <inheritdoc />
        public sealed override IMenuItem CreateField(object config, string fieldName, PropertyInfo propertyInfo, SettingsOptionAttribute attribute, List<IDrawable> drawables)
        {
            return CreateField(config, fieldName, propertyInfo, (T)attribute, drawables);
        }

        /// <summary>
        /// Creates a property field for the given config property.
        /// </summary>
        /// <param name="config">The target config this field belongs to.</param>
        /// <param name="fieldName">The name of this field.</param>
        /// <param name="propertyInfo">The <see cref="PropertyInfo"/> of the property that holds the value of this field.</param>
        /// <param name="attribute">The values of the <see cref="SettingsOptionAttribute"/> on the field's property.</param>
        /// <param name="drawables">The list of drawables. New menus have to added to this list. <see cref="IMenuItem"/>'s does not.</param>
        public abstract IMenuItem CreateField(object config, string fieldName, PropertyInfo propertyInfo, T attribute, List<IDrawable> drawables);
    }
}