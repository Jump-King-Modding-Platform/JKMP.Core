using System;
using System.Collections.Generic;
using System.Reflection;
using JKMP.Core.UI.MenuFields;
using JumpKing;
using JumpKing.PauseMenu;
using JumpKing.PauseMenu.BT;
using JumpKing.Util;

namespace JKMP.Core.Configuration.Attributes.PropertyCreators.Implementations
{
    internal class TextFieldCreator : ConfigPropertyCreator<TextFieldAttribute>
    {
        public override ICollection<Type> SupportedTypes { get; } = new List<Type>
        {
            typeof(string)
        };

        public override IMenuItem CreateField(object config, string fieldName, PropertyInfo propertyInfo, TextFieldAttribute attribute, MenuSelector menu, List<IDrawable> drawables)
        {
            var result = new TextInputField(fieldName, (string)propertyInfo.GetValue(config), attribute.MaxLength, JKContentManager.Font.MenuFont)
            {
                Visibility = attribute.Visibility,
                TrimWhitespace = attribute.TrimWhitespace,
            };

            result.OnValueChanged += val =>
            {
                OnValueChanged?.Invoke(val);
            };

            return result;
        }
    }
}