using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyUniversalJato;

public class Settings : UnityModManager.ModSettings
{
    public JatoSettings? LastJatoSettings;
    public bool ShowDebugStuff = false;

    public override void Save(UnityModManager.ModEntry modEntry)
    {
        Save(this, modEntry);
    }
}
