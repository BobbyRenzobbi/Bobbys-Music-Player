using System.Reflection;
using EFT;
using SPT.Reflection.Patching;
#if DEBUG
using BobbysMusicPlayer.Utils;
#endif

namespace BobbysMusicPlayer.Patches
{
    public class OnGameWorldStartPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(GameWorld).GetMethod(
                "OnGameStarted",
                BindingFlags.Instance | BindingFlags.Public);

        [PatchPostfix]
        static void PostFix()
        {
#if DEBUG
            OverlayDebug.Instance.Enable();
#endif
            Plugin.InRaid = true;
        }
    }
}