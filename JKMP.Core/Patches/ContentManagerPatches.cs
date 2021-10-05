using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using JKMP.Core.Content;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace JKMP.Core.Patches
{
    [HarmonyPatch]
    internal static class HookContentManagerLoadPatch
    {
        // ReSharper disable once InconsistentNaming
        private static bool Prefix(ref string assetName)
        {
            assetName = ContentRouter.GetContentPath(assetName);
            return true;
        }

        private static IEnumerable<MethodBase> TargetMethods()
        {
            MethodInfo loadMethod = typeof(ContentManager).GetMethod(nameof(ContentManager.Load)) ?? throw new NotSupportedException("ContentManager.Load method not found");
            yield return loadMethod.MakeGenericMethod(typeof(Texture2D));
            yield return loadMethod.MakeGenericMethod(typeof(SpriteFont));
            yield return loadMethod.MakeGenericMethod(typeof(SoundEffect));
            yield return loadMethod.MakeGenericMethod(typeof(Effect));
            yield return loadMethod.MakeGenericMethod(typeof(Model));
        }
    }
}