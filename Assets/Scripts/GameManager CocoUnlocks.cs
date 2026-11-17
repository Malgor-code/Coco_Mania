using System.Collections.Generic;
using UnityEngine;

public partial class GameManager
{
    private static readonly CoconutUnlockThreshold[] CanonicalUnlocks = new CoconutUnlockThreshold[]
    {
        new CoconutUnlockThreshold { coconutName = "Coco Verde",       cycleRequired = 1,  ability = CoconutAbility.Ninguna },
        new CoconutUnlockThreshold { coconutName = "Coco Maduro",      cycleRequired = 2,  ability = CoconutAbility.Generoso },
        new CoconutUnlockThreshold { coconutName = "Coco Correoso",    cycleRequired = 3,  ability = CoconutAbility.Resistente },
        new CoconutUnlockThreshold { coconutName = "Coco Fibroso",     cycleRequired = 4,  ability = CoconutAbility.FibraDura },
        new CoconutUnlockThreshold { coconutName = "Coco Petreo",      cycleRequired = 5,  ability = CoconutAbility.Pesado },
        new CoconutUnlockThreshold { coconutName = "Coco Curtido",     cycleRequired = 6,  ability = CoconutAbility.Tenaz },
        new CoconutUnlockThreshold { coconutName = "Coco Blindado",    cycleRequired = 7,  ability = CoconutAbility.Armadura },
        new CoconutUnlockThreshold { coconutName = "Coco de Hierro",   cycleRequired = 8,  ability = CoconutAbility.Rebote },
        new CoconutUnlockThreshold { coconutName = "Coco de Acero",    cycleRequired = 9,  ability = CoconutAbility.Fortificado },
        new CoconutUnlockThreshold { coconutName = "Coco de Titanio",  cycleRequired = 10, ability = CoconutAbility.Implacable },
        new CoconutUnlockThreshold { coconutName = "Coco de Diamante", cycleRequired = 11, ability = CoconutAbility.FragilValioso },
        new CoconutUnlockThreshold { coconutName = "Coco Legendario",  cycleRequired = 12, ability = CoconutAbility.Regeneracion },
        new CoconutUnlockThreshold { coconutName = "Coco Mitico",      cycleRequired = 13, ability = CoconutAbility.Camuflaje },
        new CoconutUnlockThreshold { coconutName = "Coco Ancestral",   cycleRequired = 14, ability = CoconutAbility.Maldicion },
        new CoconutUnlockThreshold { coconutName = "Coco Supremo",     cycleRequired = 16, ability = CoconutAbility.Supremo },
    };

    void StampCoconutUnlocks()
    {
        if (coconutUnlocks == null) coconutUnlocks = new List<CoconutUnlockThreshold>();

        while (coconutUnlocks.Count < CanonicalUnlocks.Length)
            coconutUnlocks.Add(new CoconutUnlockThreshold());

        for (int i = 0; i < CanonicalUnlocks.Length; i++)
        {
            Sprite keepIcon = coconutUnlocks[i].icon;
            GameObject keepPrefab = coconutUnlocks[i].prefab;
            coconutUnlocks[i].coconutName = CanonicalUnlocks[i].coconutName;
            coconutUnlocks[i].cycleRequired = CanonicalUnlocks[i].cycleRequired;
            coconutUnlocks[i].ability = CanonicalUnlocks[i].ability;
            coconutUnlocks[i].icon = keepIcon;
            coconutUnlocks[i].prefab = keepPrefab;
        }
    }
}