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
        public Action<object>? ValueChanged { get; set; }
        
        public abstract ICollection<Type> SupportedTypes { get; }

        public abstract IMenuItem CreateField(object config, string fieldName, PropertyInfo propertyInfo, SettingsOptionAttribute attribute, List<IDrawable> drawables);
    }

    public abstract class ConfigPropertyCreator<T> : ConfigPropertyCreator where T : SettingsOptionAttribute
    {
        public sealed override IMenuItem CreateField(object config, string fieldName, PropertyInfo propertyInfo, SettingsOptionAttribute attribute, List<IDrawable> drawables)
        {
            return CreateField(config, fieldName, propertyInfo, (T)attribute, drawables);
        }

        public abstract IMenuItem CreateField(object config, string fieldName, PropertyInfo propertyInfo, T attribute, List<IDrawable> drawables);
    }
}