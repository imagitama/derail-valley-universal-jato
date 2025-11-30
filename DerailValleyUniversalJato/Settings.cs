using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyUniversalJato;

public class Settings : UnityModManager.ModSettings, IDrawable
{
    public JatoSettings? LastJatoSettings;
    [Draw] public bool PreventDerail = false;

    public override void Save(UnityModManager.ModEntry modEntry)
    {
        Save(this, modEntry);
    }

    public void OnChange()
    {
    }
}
