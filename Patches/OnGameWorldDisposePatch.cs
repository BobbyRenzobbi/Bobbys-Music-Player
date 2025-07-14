using System.Reflection;
using EFT;
using SPT.Reflection.Patching;

#if DEBUG
using BobbysMusicPlayer.Utils;
#endif

namespace BobbysMusicPlayer.Patches
{
    public class OnGameWorldDisposePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(GameWorld).GetMethod(
                "Dispose",
                BindingFlags.Instance | BindingFlags.Public);

        [PatchPrefix]
        static void Prefix()
        {
#if DEBUG
            OverlayDebug.Instance.Disable();
#endif
            
            Plugin.InRaid = false;
        }
    }
}