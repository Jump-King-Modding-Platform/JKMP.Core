using System;
using System.Collections.Generic;
using System.Linq;
using JKMP.Core.Logging;
using JKMP.Core.Plugins;
using JumpKing.PauseMenu;
using JumpKing.PauseMenu.BT;
using JumpKing.Util;
using Serilog;

namespace JKMP.Core.Configuration.UI
{
    internal static class SettingsMenuManager
    {
        private static readonly List<(Plugin, string, IConfigMenu)> Menus = new();
        private static readonly Dictionary<Plugin, MenuSelector> ModMenus = new();

        private static bool menusCreated;
        private static MenuSelector? modsMenu;

        private static readonly ILogger Logger = LogManager.CreateLogger(typeof(SettingsMenuManager));
        
        internal static void CreateMenus(GuiFormat guiFormat, MenuSelector menuSelector, List<IDrawable> drawables)
        {
            CreateModsMenu(guiFormat, menuSelector, drawables);
            
            foreach (var (plugin, name, configMenu) in Menus)
            {
                var modMenuSelector = GetOrAddModSettingsMenu(guiFormat, menuSelector, plugin, drawables);

                GuiFormat menuGuiFormat = guiFormat;

                var menu = configMenu.CreateMenu(menuGuiFormat, name, modMenuSelector, drawables);
                drawables.Add(menu);
            }

            foreach (var modMenu in ModMenus.Values)
            {
                modMenu.Initialize();
            }

            modsMenu!.Initialize();
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

        private static MenuSelector GetOrAddModSettingsMenu(GuiFormat guiFormat, MenuSelector parent, Plugin plugin, List<IDrawable> drawables)
        {
            if (ModMenus.TryGetValue(plugin, out MenuSelector? value))
                return value;

            value = new MenuSelector(guiFormat);
            modsMenu!.AddChild(new TextButton(plugin.Info.Name, value));
            
            drawables.Add(value);
            ModMenus.Add(plugin, value);
            return value;
        }

        private static void CreateModsMenu(GuiFormat guiFormat, MenuSelector optionsMenu, List<IDrawable> drawables)
        {
            modsMenu = new MenuSelector(guiFormat);
            optionsMenu.AddChild(new TextButton("Mods", modsMenu));
            drawables.Add(modsMenu);
        }
    }
}