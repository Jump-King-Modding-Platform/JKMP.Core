using HarmonyLib;
using JKMP.Core.Input;
using JumpKing.Controller;

// ReSharper disable InconsistentNaming
// ReSharper disable RedundantAssignment

namespace JKMP.Core.Patches
{
    [HarmonyPatch(typeof(PadInstance), nameof(PadInstance.GetState))]
    internal static class PadInstanceGetStatePatch
    {
        private static bool Prefix(ref PadState __result)
        {
            __result = VanillaKeyBindRouter.GetState();
            return false;
        }
    }

    [HarmonyPatch(typeof(PadInstance), nameof(PadInstance.GetPressed))]
    internal static class PadInstanceGetPressedPatch
    {
        private static bool Prefix(ref PadState __result)
        {
            __result = VanillaKeyBindRouter.GetPressedState();
            return false;
        }
    }
}