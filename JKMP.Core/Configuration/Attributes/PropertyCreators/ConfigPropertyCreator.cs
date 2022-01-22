using System;
using System.Collections.Generic;
using System.Reflection;
using JumpKing.PauseMenu;
using JumpKing.PauseMenu.BT;
using JumpKing.Util;

namespace JKMP.Core.Configuration.Attributes.PropertyCreators
{
    public abstract class ConfigPropertyCreator
    {
        public Action<object>? OnValueChanged { get; set; }
        
        public abstract ICollection<Type> SupportedTypes { get; }

        public abstract IMenuItem CreateField(object config, string fieldName, PropertyInfo propertyInfo, SettingsOptionAttribute attribute, MenuSelector menu, List<IDrawable> drawables);
    }

    public abstract class ConfigPropertyCreator<T> : ConfigPropertyCreator where T : SettingsOptionAttribute
    {
        public sealed override IMenuItem CreateField(object config, string fieldName, PropertyInfo propertyInfo, SettingsOptionAttribute attribute, MenuSelector menu, List<IDrawable> drawables)
        {
            return CreateField(config, fieldName, propertyInfo, (T)attribute, menu, drawables);
        }

        public abstract IMenuItem CreateField(object config, string fieldName, PropertyInfo propertyInfo, T attribute, MenuSelector menu, List<IDrawable> drawables);
    }
}