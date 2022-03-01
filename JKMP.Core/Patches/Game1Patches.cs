using HarmonyLib;
using JumpKing;
using Microsoft.Xna.Framework;

namespace JKMP.Core.Patches
{
    [HarmonyPatch(typeof(Game1), "MyUpdate")]
    internal static class Game1UpdatePatch
    {
        private static void Prefix(GameTime gameTime)
        {
            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Events.OnPreGameUpdate(delta);
        }

        private static void Postfix(GameTime gameTime)
        {
            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Events.OnPostGameUpdate(delta);
        }
    }
}