using System;
using System.Collections.Generic;
using System.Linq;
using JKMP.Core.Logging;
using JKMP.Core.Plugins;
using JKMP.Core.UI;
using JumpKing;
using JumpKing.PauseMenu;
using JumpKing.PauseMenu.BT;
using Microsoft.Xna.Framework;
using Serilog;
using IDrawable = JumpKing.Util.IDrawable;

namespace JKMP.Core.Configuration.UI
{
    internal static class SettingsMenuManager
    {
        private static readonly List<(Plugin, string, IConfigMenu)> Menus = new();
        private static readonly Dictionary<Plugin, AdvancedMenuSelector> ModMenus = new();

        private static bool menusCreated;
        private static PluginsMenu? modsMenu;

        private static readonly ILogger Logger = LogManager.CreateLogger(typeof(SettingsMenuManager));
        
        internal static void CreateMenus(GuiFormat guiFormat, MenuSelector menuSelector, List<IDrawable> drawables)
        {
            ModMenus.Clear(); // Clear old menus in case we're returning to the menu
            CreateModsMenu(menuSelector, drawables);
            
            foreach (var (plugin, name, configMenu) in Menus)
            {
                var modMenuSelector = GetOrAddModSettingsMenu(PluginsMenu.PluginOptionsGuiFormat, menuSelector, plugin, drawables);

                GuiFormat menuGuiFormat = PluginsMenu.PluginOptionsGuiFormat;

                var menu = configMenu.CreateMenu(menuGuiFormat, name, modMenuSelector, drawables);
                drawables.Add((IDrawable)menu);
            }

            menusCreated = true;
        }

        internal static void AddMenu(Plugin owner, string name, IConfigMenu menu)
        {
            if (menu == null) throw new ArgumentNullException(nameof(menu));

            if (menusCreated)
                throw new InvalidOperationException("Menus have already been created. This method should ideally be called from the plugin's Initialize or OnLoaded event.");

            if (Menus.Any(m => m.Item3 == menu))
                throw new ArgumentException("Menu is already added");

            Menus.Add((owner, name, menu));
        }

        private static AdvancedMenuSelector GetOrAddModSettingsMenu(GuiFormat guiFormat, MenuSelector parent, Plugin plugin, List<IDrawable> drawables)
        {
            if (ModMenus.TryGetValue(plugin, out AdvancedMenuSelector? value))
                return value;

            value = new AdvancedMenuSelector(guiFormat);
            modsMenu!.AddChild("Plugins", new TextButton(plugin.Info.Name, value, JKContentManager.Font.MenuFontSmall, Color.LightGray));
            
            drawables.Add(value);
            ModMenus.Add(plugin, value);
            return value;
        }

        private static void CreateModsMenu(MenuSelector optionsMenu, List<IDrawable> drawables)
        {
            modsMenu = new PluginsMenu(drawables);
            optionsMenu.AddChild(new TextButton("Configure plugins", modsMenu));
        }
    }
}