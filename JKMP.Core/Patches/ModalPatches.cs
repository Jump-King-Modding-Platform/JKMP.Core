using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BehaviorTree;
using HarmonyLib;
using JKMP.Core.Logging;
using JKMP.Core.UI;
using JumpKing.GameManager.TitleScreen;
using JumpKing.PauseMenu;
using JumpKing.PauseMenu.BT;
using JumpKing.Util;

namespace JKMP.Core.Patches
{
    // What we're modifying:
    // ldfld        class JumpKing.PauseMenu.BT.MenuSelector JumpKing.PauseMenu.PauseManager::m_main_menu
    // callvirt     instance void BehaviorTree.IBTcomposite::AddChild(class BehaviorTree.IBTnode)
    //
    // What we're replacing it with:
    // ldfld        class JumpKing.PauseMenu.BT.MenuSelector JumpKing.PauseMenu.PauseManager::m_main_menu
    // load this
    // load field m_factory
    // load m_factory.m_drawables
    // call MakeNode(MenuSelector, IEnumerable<IDrawable>)
    // callvirt     instance void BehaviorTree.IBTcomposite::AddChild(class BehaviorTree.IBTnode)
    [HarmonyPatch(typeof(PauseManager), "MakeBT")]
    internal static class PauseManagerModalPatch
    {
        private static readonly MethodInfo CompositeAddChildMethod =
            AccessTools.Method(typeof(IBTcomposite), nameof(IBTcomposite.AddChild)) ?? throw new NotSupportedException("IBTcomposite.AddChild not found");

        private static readonly MethodInfo CreateNodeMethod = AccessTools.Method(typeof(PauseManagerModalPatch), nameof(MakeNode));
        private static readonly FieldInfo MenuFactoryField = AccessTools.Field(typeof(PauseManager), "m_factory");
        private static readonly FieldInfo DrawablesField = AccessTools.Field(typeof(MenuFactory), "m_drawables");

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var list = instructions.ToList();

            for (int i = 0; i < list.Count; ++i)
            {
                var inst1 = list[i];

                if (i < list.Count - 2)
                {
                    var inst2 = list[i + 1];

                    if (
                        inst1.opcode == OpCodes.Ldfld && ((FieldInfo)inst1.operand).Name == "m_main_menu" &&
                        inst2.opcode == OpCodes.Callvirt && (MethodInfo)inst2.operand == CompositeAddChildMethod
                    )
                    {
                        yield return inst1;
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldfld, MenuFactoryField);
                        yield return new CodeInstruction(OpCodes.Ldfld, DrawablesField);
                        yield return new CodeInstruction(OpCodes.Call, CreateNodeMethod);
                        yield return inst2;

                        i += 1;

                        continue;
                    }
                }

                yield return inst1;
            }
        }

        private static IBTnode MakeNode(MenuSelector mainMenu, List<IDrawable> drawables)
        {
            return ModalDialog.CreateMainMenuNode(mainMenu, drawables);
        }
    }

    [HarmonyPatch(typeof(GameTitleScreen), "CreateMenu")]
    internal static class GameTitleScreenModalPatch
    {
        // ReSharper disable InconsistentNaming
        private static void Postfix(MenuFactory ___m_menu_factory, ref IBTnode __result)
            // ReSharper restore InconsistentNaming
        {
            var drawables = (List<IDrawable>)AccessTools.Field(typeof(MenuFactory), "m_drawables").GetValue(___m_menu_factory);
            __result = ModalDialog.CreateMainMenuNode(__result, drawables);
        }
    }
}