using System.Collections.Generic;
using UnityEngine;
using static MacheteData;

public static class GameData
{
    static bool built;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => built = false;

    public static void Ensure()
    {
        if (built) return;
        DefaultGameData.Build();
        built = true;
    }

    public static List<MacheteData> Machetes { get { Ensure(); return DefaultGameData.macheteOptions; } }
    public static List<SkillNode> Skills { get { Ensure(); return DefaultGameData.skillTree; } }
    public static List<PerkOption> Perks { get { Ensure(); return DefaultGameData.perkPool; } }
    public static List<LegacyItem> LegacyItems { get { Ensure(); return DefaultGameData.legacyShop; } }
}