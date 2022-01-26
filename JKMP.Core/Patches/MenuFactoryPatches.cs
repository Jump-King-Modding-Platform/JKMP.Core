using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JKMP.Core.Configuration.UI;
using JumpKing.PauseMenu;
using JumpKing.PauseMenu.BT;

namespace JKMP.Core.Patches
{
    [HarmonyPatch(typeof(MenuFactory), nameof(MenuFactory.CreateOptionsMenu))]
    internal static class OptionsMenuPatch
    {
        private static readonly MethodInfo CreateAudioOptionsMethod = AccessTools.Method(typeof(MenuFactory), "CreateAudioOptions");
        private static readonly FieldInfo DrawablesField = AccessTools.Field(typeof(MenuFactory), "m_drawables");
        private static readonly MethodInfo CreateOptionsMenuMethod = AccessTools.Method(typeof(OptionsMenuPatch), nameof(CreateOptionsMenu));

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var list = instructions.ToList();

            for (int i = 0; i < list.Count; ++i)
            {
                var instruction = list[i];

                if (instruction.Calls(CreateAudioOptionsMethod))
                {
                    // Return the next 3 instructions
                    yield return instruction;
                    yield return list[++i];
                    yield return list[++i];

                    yield return new CodeInstruction(OpCodes.Ldarg_0); // Load this onto stack
                    yield return new CodeInstruction(OpCodes.Ldarg_3); // Load gui sub sub format (3rd parameter) onto stack
                    yield return new CodeInstruction(OpCodes.Ldloc_0); // Load first local variable onto stack
                    yield return new CodeInstruction(OpCodes.Ldarg_0); // Load this onto stack
                    yield return new CodeInstruction(OpCodes.Ldfld, DrawablesField); // Load m_drawables from this onto stack
                    yield return new CodeInstruction(OpCodes.Call, CreateOptionsMenuMethod); // Call CreateMultiplayerOptionsMenu
                }
                else
                {
                    yield return instruction;
                }
            }
        }

        private static void CreateOptionsMenu(MenuFactory menuFactory, GuiFormat guiFormat, MenuSelector menuSelector, List<JumpKing.Util.IDrawable> drawables)
        {
            SettingsMenuManager.CreateMenus(guiFormat, menuSelector, drawables);
        }
    }
}