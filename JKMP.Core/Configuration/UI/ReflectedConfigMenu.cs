using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BehaviorTree;
using JKMP.Core.Configuration.Attributes;
using JKMP.Core.Configuration.Attributes.PropertyCreators;
using JKMP.Core.Logging;
using JKMP.Core.Plugins;
using JumpKing.PauseMenu;
using JumpKing.PauseMenu.BT;
using JumpKing.Util;
using Serilog;

namespace JKMP.Core.Configuration.UI
{
    internal class ReflectedConfigMenu<T> : IConfigMenu<T> where T : class, new()
    {
        public T Values { get; }

        private readonly Plugin owner;
        private readonly string sourceName;

        private static readonly ILogger Logger = LogManager.CreateLogger<ReflectedConfigMenu<T>>();

        public ReflectedConfigMenu(Plugin owner, string sourceName)
        {
            this.owner = owner;
            this.sourceName = sourceName;
            
            Values = owner.Configs.LoadConfig<T>(sourceName);
        }

        public MenuSelector CreateMenu(GuiFormat format, string name, MenuSelector parent, List<IDrawable> drawables)
        {
            var menu = new MenuSelector(format);
            CreateFields(menu, drawables);
            menu.Initialize();
            drawables.Add(menu);

            parent.AddChild(new TextButton(name, menu));

            return menu;
        }

        private void CreateFields(MenuSelector menu, List<IDrawable> drawables)
        {            
            // Get all properties that are not static
            var properties = Values.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var property in properties)
            {
                var settingsOptionAttributes = property.GetCustomAttributes<SettingsOptionAttribute>(inherit: true).ToList();

                // If there is no SettingsOptionAttribute, skip this property
                if (settingsOptionAttributes.Count == 0)
                    continue;

                // If there is more than one SettingsOptionAttribute, throw an exception
                if (settingsOptionAttributes.Count > 1)
                    throw new ConfigAttributeException("Multiple SettingsOptionAttribute found on property");

                SettingsOptionAttribute optionAttribute = settingsOptionAttributes.Single();

                var propertyCreatorType = optionAttribute.GetType().GetCustomAttribute<SettingsOptionCreatorAttribute>()?.Type;

                if (propertyCreatorType == null)
                    throw new ConfigAttributeException($"No SettingsOptionCreatorAttribute found on type {optionAttribute.GetType().FullName}");

                if (propertyCreatorType.GetConstructor(Type.EmptyTypes) == null)
                    throw new ConfigAttributeException($"Property reader {propertyCreatorType} must have a default constructor");
                
                var propertyCreator = Activator.CreateInstance(propertyCreatorType) as ConfigPropertyCreator;
                
                // Check that the property creator inherits from ConfigPropertyCreator
                if (propertyCreator == null)
                    throw new ConfigAttributeException($"Property reader for type {optionAttribute.GetType()} is not of type ConfigPropertyCreator");

                if (!propertyCreator.SupportedTypes.Contains(property.PropertyType))
                    throw new ConfigAttributeException($"Property type {property.PropertyType} is not supported by {propertyCreatorType}");

                propertyCreator.ValueChanged += newValue =>
                {
                    if (newValue.GetType() == property.PropertyType)
                    {
                        property.SetValue(Values, newValue);
                    }
                    else
                    {
                        property.SetValue(Values, Convert.ChangeType(newValue, property.PropertyType));
                    }

                    try
                    {
                        owner.Configs.SaveConfig(Values, sourceName);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Failed to save config");
                    }
                };

                string fieldName = optionAttribute.Name ?? property.Name;
                
                // Create the field
                IMenuItem field = propertyCreator.CreateField(Values, fieldName, property, optionAttribute, menu, drawables);

                // Add the field to the menu. We need to cast the menu to IBTcomposite and the field to IBTnode
                // because the MenuSelector.AddChild method has a constrained generic that we can't use at compile time
                ((IBTcomposite)menu).AddChild((IBTnode)field);
            }
        }
    }
}