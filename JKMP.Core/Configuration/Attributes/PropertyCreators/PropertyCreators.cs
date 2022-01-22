using System;
using System.Collections.Generic;
using JKMP.Core.Configuration.Attributes.PropertyCreators.Implementations;

namespace JKMP.Core.Configuration.Attributes.PropertyCreators
{
    public static class PropertyCreators
    {
        public static readonly Dictionary<Type, Type> Values = new()
        {
            { typeof(TextFieldAttribute), typeof(TextFieldCreator) }
        };
    }
}