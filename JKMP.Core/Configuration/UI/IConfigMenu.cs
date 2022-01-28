using System;
using System.Collections.Generic;
using BehaviorTree;
using JKMP.Core.UI;
using JumpKing.PauseMenu;
using JumpKing.Util;

namespace JKMP.Core.Configuration.UI
{
    /// <summary>
    /// A class that represents a configuration menu for a plugin's configuration. There can be multiple of these for a single plugin.
    /// Note: Implement the generic interface when implementing your own menu. This is not generally needed for the built-in system.
    /// </summary>
    public interface IConfigMenu
    {
        /// <summary>
        /// Contains the property name and value of a property that changed.
        /// </summary>
        public class PropertyChangedEventArgs : EventArgs
        {
            /// <summary>
            /// The name of the property that changed.
            /// </summary>
            public string PropertyName { get; set; }
            
            /// <summary>
            /// The new value of the property.
            /// </summary>
            public object? Value { get; set; }
            
            /// <summary>
            /// Instantiates a new instance of the <see cref="PropertyChangedEventArgs"/> class.
            /// </summary>
            /// <param name="propertyName">The name of the property that changed</param>
            /// <param name="value">The property's new value</param>
            /// <exception cref="ArgumentNullException">Thrown if propertyName is null</exception>
            public PropertyChangedEventArgs(string propertyName, object? value)
            {
                PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
                Value = value;
            }
        }

        /// <inheritdoc />
        public delegate void PropertyChangedEventHandler(object sender, PropertyChangedEventArgs args);

        /// <summary>
        /// Invoked when a property changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        
        /// <summary>
        /// Called when the menu should be created.
        /// </summary>
        /// <param name="format">The <see cref="GuiFormat"/> of the menu</param>
        /// <param name="name">The name of the menu</param>
        /// <param name="parent"></param>
        /// <param name="drawables">The list of drawables. New menus will need to be added to this. The return value is added by the caller.</param>
        public IBTnode CreateMenu(GuiFormat format, string name, AdvancedMenuSelector parent, List<IDrawable> drawables);
    }

    /// <summary>
    /// A class that represents a configuration menu for a plugin's configuration. There can be multiple of these for a single plugin.
    /// Implement this interface when implementing your own menu. This is not generally needed for the built-in system.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IConfigMenu<T> : IConfigMenu where T : class, new()
    {
        /// <summary>
        /// The configuration object that this menu is for.
        /// </summary>
        public T Values { get; }
    }
}