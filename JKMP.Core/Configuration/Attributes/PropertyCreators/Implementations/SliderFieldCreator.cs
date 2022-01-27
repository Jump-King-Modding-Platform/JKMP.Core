using System;
using System.Collections.Generic;
using System.Reflection;
using JKMP.Core.UI.MenuFields;
using JumpKing.PauseMenu;
using JumpKing.PauseMenu.BT;
using JumpKing.Util;

namespace JKMP.Core.Configuration.Attributes.PropertyCreators.Implementations
{
    internal class SliderFieldCreator : ConfigPropertyCreator<SliderFieldAttribute>
    {
        public override ICollection<Type> SupportedTypes => new List<Type>
        {
            typeof(float),
            typeof(int)
        };
        
        public override IMenuItem CreateField(object config, string fieldName, PropertyInfo propertyInfo, SliderFieldAttribute attribute, List<IDrawable> drawables)
        {
            var result = new SliderField(fieldName, (float)Convert.ChangeType(propertyInfo.GetValue(config), TypeCode.Single), attribute.MinValue, attribute.MaxValue, attribute.StepSize);

            result.ValueChanged += val =>
            {
                ValueChanged?.Invoke(val);
            };

            return result;
        }
    }
}