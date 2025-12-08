using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyUniversalJato;

public class Settings : UnityModManager.ModSettings, IDrawable
{
    public JatoSettings? LastJatoSettings;
    [Draw(Label = "Completely disable derailing")] public bool PreventDerail = false;
    [Draw(Label = "Disable crouching when in train car")] public bool DisableCrouchWhenInTrainCar = true;

    public override void Save(UnityModManager.ModEntry modEntry)
    {
        Save(this, modEntry);

        if (PreventDerail)
            TrainCarHelper.EnableNoDerail();
        else
            TrainCarHelper.DisableNoDerail();
    }

    public void OnChange()
    {
    }
}
