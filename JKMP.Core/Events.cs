using System;
using JumpKing;

namespace JKMP.Core
{
    internal static class Events
    {
        private static object DefaultEventSender => Game1.instance;

        public static event EventHandler<float>? PreGameUpdate;

        public static void OnPreGameUpdate(float delta)
        {
            PreGameUpdate?.Invoke(DefaultEventSender, delta);
        }

        public static event EventHandler<float>? PostGameUpdate;
        
        public static void OnPostGameUpdate(float delta)
        {
            PostGameUpdate?.Invoke(DefaultEventSender, delta);
        }

        public static event EventHandler? PostGameInitialized;

        public static void OnPostGameInitialize()
        {
            PostGameInitialized?.Invoke(DefaultEventSender, EventArgs.Empty);
        }
    }
}