using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using System;

namespace ResoniteGameStreamerMod
{
    // All Harmony patches are free-standing classes now.
    // They call into CanvasBuilder, MmfIO, PixelApplier, and read StreamerConfig/RuntimeState.
    [HarmonyPatch]
    internal static class CanvasFinishUpdatePatch
    {
        [HarmonyPatch(typeof(Canvas), "FinishCanvasUpdate")]
        [HarmonyPostfix]
        private static void Postfix(Canvas __instance)
        {
            if (!StreamerConfig.Enabled) return;
            if (__instance.Slot.Name != StreamerConfig.CanvasName) return;

            if (__instance != RuntimeState.Canvas)
            {
                ResoniteGameStreamerMod.Msg($"[Canvas] Found target canvas '{__instance.Slot.Name}' (new instance).");
                RuntimeState.Canvas = __instance;
                RuntimeState.Initialized = false;
            }

            if (!RuntimeState.Initialized || StreamerConfig.ReInitializeNeeded)
            {
                if ((DateTime.UtcNow - RuntimeState.LastInitAttempt).TotalSeconds < 5) return;
                RuntimeState.LastInitAttempt = DateTime.UtcNow;

                ResoniteGameStreamerMod.Msg($"[Canvas] Initializing (rgb_mode={StreamerConfig.RgbMode}, size={StreamerConfig.CanvasW}x{StreamerConfig.CanvasH}) ...");
                try
                {
                    CanvasBuilder.InitializeCanvas(__instance);
                    RuntimeState.Initialized = true;
                    StreamerConfig.ReInitializeNeeded = false;
                    ResoniteGameStreamerMod.Msg("[Canvas] Initialization complete.");
                }
                catch (Exception ex)
                {
                    ResoniteGameStreamerMod.Error($"[Canvas] Initialization failed: {ex}");
                    RuntimeState.Initialized = false;
                }
            }
        }
    }

    [HarmonyPatch]
    internal static class CanvasOnDestroyPatch
    {
        [HarmonyPatch(typeof(Canvas), "OnDestroy")]
        [HarmonyPrefix]
        private static void Prefix(Canvas __instance)
        {
            if (!StreamerConfig.Enabled) return;
            if (__instance.Slot.Name != StreamerConfig.CanvasName) return;

            ResoniteGameStreamerMod.Msg("[Canvas] OnDestroy detected; cleaning up resources.");
            RuntimeState.DisposeAll();
        }
    }

    [HarmonyPatch]
    internal static class AnimatorOnCommonUpdatePatch
    {
        [HarmonyPatch(typeof(FrooxEngine.Animator), "OnCommonUpdate")]
        [HarmonyPrefix]
        private static void Prefix()
        {
            if (!StreamerConfig.Enabled) return;
            if (!RuntimeState.Initialized || RuntimeState.Canvas == null) return;

            if (RuntimeState.PxDataLen == -1)
            {
                MmfIO.ReadFrameIfAvailable();
                if (RuntimeState.PxDataLen == -1) return;
            }

            try
            {
                PixelApplier.ApplyCurrentFrame();
            }
            catch (Exception ex)
            {
                ResoniteGameStreamerMod.Error($"[Frame] Failed to apply frame: {ex}");
                RuntimeState.Initialized = false;
            }
            finally
            {
                RuntimeState.ResetFrameLatch();
            }
        }
    }

    [HarmonyPatch]
    internal static class ConfigChangedPatch
    {
        [HarmonyPatch(typeof(ResoniteModLoader.ModConfiguration), "FireConfigurationChangedEvent")]
        [HarmonyPostfix]
        private static void Postfix()
        {
            if (ResoniteGameStreamerMod.Config != null)
                StreamerConfig.UpdateFrom(ResoniteGameStreamerMod.Config);
        }
    }
}
