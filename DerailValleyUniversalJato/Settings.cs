using System.Collections.Generic;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyUniversalJato;

public static class Actions
{
    public static int RearJatoActivate = 150;
    public static int FrontJatoActivate = 151;
}

public class Settings : UnityModManager.ModSettings, IDrawable
{
    [Draw(Label = "Completely disable derailing")] public bool PreventDerail = false;
    [Draw(Label = "Disable crouching when in train car")] public bool DisableCrouchWhenInTrainCar = true;
    [Draw(Label = "Game must be in focus to activate JATO")] public bool RequireGameFocus = true;
    public List<BindingInfo> Bindings;

    public void AddDefaultBindings()
    {
        var defaultBindings = new List<BindingInfo>()
        {
            new BindingInfo("Rear JATO", Actions.RearJatoActivate, KeyCode.LeftShift),
            new BindingInfo("Front JATO", Actions.FrontJatoActivate, KeyCode.LeftControl),
        };

        if (Bindings.Count == 0)
            Bindings = defaultBindings;

        BindingsAPI.RegisterBindings(Main.ModEntry, Bindings);
    }

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
