using System.Collections.Generic;
using System.Linq;
using DerailValleyUniversalJato;
using UnityModManagerNet;

public static class BindingsAPI
{
    public static Dictionary<UnityModManager.ModEntry, List<BindingInfo>> AllBindings = [];

    public static List<BindingInfo> RegisterBindings(UnityModManager.ModEntry modEntry, List<BindingInfo> bindings)
    {
        // TODO: allow replace/addition
        AllBindings[modEntry] = bindings;

        RebuildBindingsByAction();

        return bindings;
    }

    public static Dictionary<int, List<BindingInfo>> BindingsByAction = new();

    public static void RebuildBindingsByAction()
    {
        BindingsByAction.Clear();

        foreach (var kv in AllBindings)
        {
            var list = kv.Value;
            for (int i = 0; i < list.Count; i++)
            {
                var b = list[i];
                if (!BindingsByAction.TryGetValue(b.ActionId, out var actionList))
                {
                    actionList = new List<BindingInfo>();
                    BindingsByAction[b.ActionId] = actionList;
                }
                actionList.Add(b);
            }
        }
    }

    public static bool GetIsPressed(int actionId)
    {
        if (!BindingsByAction.TryGetValue(actionId, out var bindings))
            return false;

        for (int i = 0; i < bindings.Count; i++)
            if (BindingsHelper.GetIsPressed(bindings[i]))
                return true;

        return false;
    }
}