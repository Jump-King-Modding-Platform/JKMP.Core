using System.Reflection;
using HarmonyLib;
using JKMP.Core.Input;
using JumpKing.Controller;

// ReSharper disable InconsistentNaming
// ReSharper disable RedundantAssignment

namespace JKMP.Core.Patches
{
    [HarmonyPatch(typeof(PadInstance), nameof(PadInstance.Update))]
    internal static class PadInstanceUpdatePatch
    {
        private static readonly FieldInfo LastStateField = AccessTools.Field(typeof(PadInstance), "last_state");
        private static readonly FieldInfo CurrentStateField = AccessTools.Field(typeof(PadInstance), "current_state");

        private static bool Prefix(PadInstance __instance)
        {
            LastStateField.SetValue(__instance, CurrentStateField.GetValue(__instance));
            CurrentStateField.SetValue(__instance, VanillaKeyBindRouter.GetState());
            
            return false;
        }
    }
}