using HarmonyLib;

namespace DerailValleyUniversalJato;

[HarmonyPatch(typeof(LocomotionInputWrapper), "get_CrouchRequested")]
static class Patch_CrouchRequested
{
    static void Postfix(ref bool __result)
    {
        if (PlayerManager.Car != null && Main.settings != null && Main.settings.DisableCrouchWhenInTrainCar)
            __result = false;
    }
}
