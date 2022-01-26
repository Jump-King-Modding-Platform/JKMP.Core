using System;
using System.Collections.Generic;
using System.Reflection;
using JKMP.Core.UI.MenuFields;
using JumpKing.PauseMenu;
using JumpKing.PauseMenu.BT;
using JumpKing.Util;

namespace JKMP.Core.Configuration.Attributes.PropertyCreators.Implementations
{
    internal class CheckboxFieldCreator : ConfigPropertyCreator<CheckboxFieldAttribute>
    {
        public override ICollection<Type> SupportedTypes => new List<Type>
        {
            typeof(bool)
        };
        
        public override IMenuItem CreateField(object config, string fieldName, PropertyInfo propertyInfo, CheckboxFieldAttribute attribute, List<IDrawable> drawables)
        {
            var result = new CheckboxField(fieldName, (bool)propertyInfo.GetValue(config));

            result.ValueChanged += val =>
            {
                ValueChanged?.Invoke(val);
            };

            return result;
        }
    }
}