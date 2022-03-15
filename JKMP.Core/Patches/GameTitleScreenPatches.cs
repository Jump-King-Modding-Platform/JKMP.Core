using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BehaviorTree;
using HarmonyLib;
using JKMP.Core.Logging;
using JumpKing;
using JumpKing.GameManager.TitleScreen;
using JumpKing.Util;
using Microsoft.Xna.Framework;

namespace JKMP.Core.Patches
{
    [HarmonyPatch(typeof(GameTitleScreen), nameof(GameTitleScreen.Draw))]
    internal static class GameTitleScreenDrawPatch
    {
        private static readonly MethodInfo IBTnodeIsRunningMethod = AccessTools.Method(typeof(IBTnode), nameof(IBTnode.IsRunning));
        private static readonly FieldInfo GameTitleScreenMenuField = AccessTools.Field(typeof(GameTitleScreen), "m_menu");
        private static readonly MethodInfo DrawLogoMethod = AccessTools.Method(typeof(GameTitleScreenDrawPatch), nameof(DrawLogo));

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var list = instructions.ToList();

            for (int i = 0; i < list.Count; ++i)
            {
                var instruction = list[i];

                if (i > 1)
                {
                    var previousInstruction = list[i - 1];
                    bool previousInstructionIsField = previousInstruction.opcode == OpCodes.Ldfld && (FieldInfo)previousInstruction.operand == GameTitleScreenMenuField;
                    
                    if (instruction.Calls(IBTnodeIsRunningMethod) && previousInstructionIsField)
                    {
                        // Returns the next 2 instructions
                        yield return instruction;
                        yield return list[++i];

                        yield return new CodeInstruction(OpCodes.Call, DrawLogoMethod); // Call DrawLogo
                    }
                    else
                    {
                        yield return instruction;
                    }
                }
                else
                {
                    yield return instruction;
                }
            }
        }

        private static readonly string VersionName = Assembly.GetExecutingAssembly().GetName().Version.ToString(fieldCount: 3);
        private static string? logoText;

        private static void DrawLogo()
        {
            if (logoText == null)
            {
                int pluginsCount = JKCore.Instance.Plugins.Count;
                string numPluginsText = $"{pluginsCount} plugin" + (pluginsCount != 1 ? "s" : "");
                logoText = $"jkmp core v{VersionName} ({numPluginsText} loaded)";
            }
            
            Vector2 drawPosition = new Vector2(45, 160);
            TextHelper.DrawString(JKContentManager.Font.MenuFont, logoText, drawPosition, Color.Gold, Vector2.Zero);
        }
    }
}