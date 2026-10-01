#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class RewardMachineGenerator
{
    // ---- Rutas configurables ----
    private const string RewardsFolder = "Assets/RewardMachine/Rewards";
    private const string IconsFolder = "Assets/RewardMachine/Icons";
    private const string DatabasePath = "Assets/RewardMachine/RewardDatabase.asset";

    // Objetos fisicos eliminados del juego: se borran del proyecto y de la base de datos al generar.
    private static readonly string[] ObsoleteIds =
    {
        "cubeta_especial", "mini_machete", "iman_monedas", "canasta_automatica",
        "reloj_cobrador", "reloj_detenido", "tormenta_cocos"
    };

    [MenuItem("Tools/Reward Machine/Generar 35 Recompensas (balanceo)")]
    public static void GenerateAll()
    {
        EnsureFolder(RewardsFolder);
        EnsureFolder(IconsFolder);

        RewardDatabase db = LoadOrCreateDatabase();
        RemoveObsolete(db);

        var defs = BuildDefinitions();
        var created = new List<RewardData>();
        var missingIcons = new List<string>();

        foreach (var def in defs)
        {
            RewardData asset = CreateOrUpdate(def, out bool iconFound);
            created.Add(asset);
            if (!iconFound) missingIcons.Add(def.id);
        }

        MergeIntoDatabase(db, created);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[RewardMachineGenerator] Listo: {created.Count} RewardData generadas/actualizadas en '{RewardsFolder}', " +
                  $"RewardDatabase actualizada en '{DatabasePath}'.");

        if (missingIcons.Count > 0)
        {
            Debug.LogWarning($"[RewardMachineGenerator] No se encontro sprite para {missingIcons.Count} recompensas " +
                              $"(se dejaron sin icono, o con el que ya tenian antes). Falta poner en '{IconsFolder}' un " +
                              $"archivo de imagen con alguno de estos nombres:\n- " + string.Join("\n- ", missingIcons));
        }

        Selection.activeObject = db;
        EditorGUIUtility.PingObject(db);
    }

    static void RemoveObsolete(RewardDatabase db)
    {
        foreach (var id in ObsoleteIds)
        {
            string path = $"{RewardsFolder}/{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<RewardData>(path) != null)
                AssetDatabase.DeleteAsset(path);
        }

        if (db.allRewards != null)
        {
            db.allRewards.RemoveAll(r => r == null);
            EditorUtility.SetDirty(db);
        }
    }

    static RewardData CreateOrUpdate(RewardDef def, out bool iconFound)
    {
        string assetPath = $"{RewardsFolder}/{def.id}.asset";
        RewardData asset = AssetDatabase.LoadAssetAtPath<RewardData>(assetPath);

        bool isNew = asset == null;
        if (isNew)
        {
            asset = ScriptableObject.CreateInstance<RewardData>();
            AssetDatabase.CreateAsset(asset, assetPath);
        }

        asset.id = def.id;
        asset.displayName = def.displayName;
        asset.description = def.description;
        asset.rarity = def.rarity;
        asset.category = def.category;
        asset.effect = def.effect;
        asset.value = def.value;
        asset.durationDays = def.durationDays;

        Sprite found = FindIconForId(def.id);
        iconFound = found != null;
        if (found != null) asset.icon = found;

        EditorUtility.SetDirty(asset);
        return asset;
    }

    static Sprite FindIconForId(string id)
    {
        string[] guids = AssetDatabase.FindAssets($"t:Sprite {id}", new[] { IconsFolder });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (fileName == id)
            {
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
        }
        return null;
    }

    static RewardDatabase LoadOrCreateDatabase()
    {
        RewardDatabase db = AssetDatabase.LoadAssetAtPath<RewardDatabase>(DatabasePath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<RewardDatabase>();
            AssetDatabase.CreateAsset(db, DatabasePath);
        }
        return db;
    }

    static void MergeIntoDatabase(RewardDatabase db, List<RewardData> generated)
    {
        if (db.allRewards == null) db.allRewards = new List<RewardData>();

        foreach (var reward in generated)
        {
            bool already = db.allRewards.Exists(r => r != null && r.id == reward.id);
            if (!already) db.allRewards.Add(reward);
        }

        EditorUtility.SetDirty(db);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }

    private struct RewardDef
    {
        public string id, displayName, description;
        public RewardRarity rarity;
        public RewardCategory category;
        public RewardEffect effect;
        public float value;
        public int durationDays;
    }

    static RewardDef Temp(string id, string name, string desc, RewardRarity rarity, RewardEffect effect, float value, int days) => new RewardDef
    {
        id = id,
        displayName = name,
        description = desc,
        rarity = rarity,
        category = RewardCategory.Temporary,
        effect = effect,
        value = value,
        durationDays = days
    };

    static RewardDef Perm(string id, string name, string desc, RewardRarity rarity, RewardEffect effect, float value) => new RewardDef
    {
        id = id,
        displayName = name,
        description = desc,
        rarity = rarity,
        category = RewardCategory.PermanentRelic,
        effect = effect,
        value = value
    };

    static List<RewardDef> BuildDefinitions()
    {
        var list = new List<RewardDef>();
        list.Add(Temp("filo_improvisado", "Filo Improvisado", "+1 daño durante 2 días.",
            RewardRarity.Common, RewardEffect.DamageBonus, 1f, 2));
        list.Add(Temp("agua_bendecida", "Agua Bendecida", "+10% agua durante 1 día.",
            RewardRarity.Common, RewardEffect.WaterMultiplierBonus, 0.10f, 1));
        list.Add(Temp("guantes_secos", "Guantes Secos", "+3 stamina máxima durante 2 días.",
            RewardRarity.Common, RewardEffect.StaminaMaxBonus, 3f, 2));
        list.Add(Temp("moneda_suelta", "Moneda Suelta", "+3% prob. de moneda durante 2 días.",
            RewardRarity.Common, RewardEffect.CoinDropChanceBonus, 0.03f, 2));
        list.Add(Temp("filo_improvisado_2", "Filo Improvisado II", "+2 daño durante 2 días.",
            RewardRarity.Uncommon, RewardEffect.DamageBonus, 2f, 2));
        list.Add(Temp("golpe_preciso", "Golpe Preciso", "+5% crítico durante 2 días.",
            RewardRarity.Uncommon, RewardEffect.CritChanceBonus, 0.05f, 2));
        list.Add(Temp("agua_bendecida_2", "Agua Bendecida II", "+15% agua durante 2 días.",
            RewardRarity.Uncommon, RewardEffect.WaterMultiplierBonus, 0.15f, 2));
        list.Add(Temp("trebol_suerte", "Trébol de la Suerte", "+5% prob. de moneda durante 2 días.",
            RewardRarity.Uncommon, RewardEffect.CoinDropChanceBonus, 0.05f, 2));
        list.Add(Temp("muneca_agil", "Muñeca Ágil", "Golpea ~5% más seguido durante 2 días.",
            RewardRarity.Uncommon, RewardEffect.SwingIntervalReduction, 0.05f, 2));
        list.Add(Temp("filo_improvisado_3", "Filo Improvisado III", "+3 daño durante 3 días.",
            RewardRarity.Rare, RewardEffect.DamageBonus, 3f, 3));
        list.Add(Temp("golpe_preciso_2", "Golpe Preciso II", "+8% crítico durante 3 días.",
            RewardRarity.Rare, RewardEffect.CritChanceBonus, 0.08f, 3));
        list.Add(Temp("agua_bendecida_3", "Agua Bendecida III", "+25% agua durante 2 días.",
            RewardRarity.Rare, RewardEffect.WaterMultiplierBonus, 0.25f, 2));
        list.Add(Temp("aliento_profundo", "Aliento Profundo", "+6 stamina máxima durante 3 días.",
            RewardRarity.Rare, RewardEffect.StaminaMaxBonus, 6f, 3));
        list.Add(Temp("pulso_firme", "Pulso Firme", "Golpea ~10% más seguido durante 2 días.",
            RewardRarity.Rare, RewardEffect.SwingIntervalReduction, 0.10f, 2));
        list.Add(Perm("piedra_afilado", "Piedra de Afilado", "+2 daño mientras esté equipada.",
            RewardRarity.Rare, RewardEffect.DamageBonus, 2f));
        list.Add(Perm("ojo_cazador", "Ojo de Cazador", "+3% crítico mientras esté equipado.",
            RewardRarity.Rare, RewardEffect.CritChanceBonus, 0.03f));
        list.Add(Perm("cantimplora_vieja", "Cantimplora Vieja", "+8% agua mientras esté equipada.",
            RewardRarity.Rare, RewardEffect.WaterMultiplierBonus, 0.08f));
        list.Add(Perm("correa_reforzada", "Correa Reforzada", "+4 stamina máxima mientras esté equipada.",
            RewardRarity.Rare, RewardEffect.StaminaMaxBonus, 4f));
        list.Add(Perm("mango_reforzado", "Mango Reforzado", "+0.1 radio de golpe mientras esté equipado.",
            RewardRarity.Rare, RewardEffect.HitRadiusBonus, 0.1f));
        list.Add(Perm("cuerda_tensa", "Cuerda Tensa", "Golpea ~8% más seguido mientras esté equipada.",
            RewardRarity.Rare, RewardEffect.SwingIntervalReduction, 0.08f));
        list.Add(Perm("amuleto_moneda", "Amuleto de Moneda", "+4% prob. de moneda mientras esté equipado.",
            RewardRarity.Rare, RewardEffect.CoinDropChanceBonus, 0.04f));
        list.Add(Perm("filo_templado", "Filo Templado", "+3 daño mientras esté equipado.",
            RewardRarity.Epic, RewardEffect.DamageBonus, 3f));
        list.Add(Perm("ojo_cazador_2", "Ojo de Cazador II", "+5% crítico mientras esté equipado.",
            RewardRarity.Epic, RewardEffect.CritChanceBonus, 0.05f));
        list.Add(Perm("cantimplora_grande", "Cantimplora Grande", "+15% agua mientras esté equipada.",
            RewardRarity.Epic, RewardEffect.WaterMultiplierBonus, 0.15f));
        list.Add(Perm("cinturon_titan", "Cinturón del Titán", "+7 stamina máxima mientras esté equipado.",
            RewardRarity.Epic, RewardEffect.StaminaMaxBonus, 7f));
        list.Add(Perm("mango_reforzado_2", "Mango Reforzado II", "+0.2 radio de golpe mientras esté equipado.",
            RewardRarity.Epic, RewardEffect.HitRadiusBonus, 0.2f));
        list.Add(Perm("reflejos_felinos", "Reflejos Felinos", "Golpea ~15% más seguido mientras esté equipado.",
            RewardRarity.Epic, RewardEffect.SwingIntervalReduction, 0.15f));
        list.Add(Perm("moneda_dorada", "Moneda Dorada", "+8% prob. de moneda mientras esté equipada.",
            RewardRarity.Epic, RewardEffect.CoinDropChanceBonus, 0.08f));
        list.Add(Perm("filo_perfecto", "Filo Perfecto", "+5 daño mientras esté equipado.",
            RewardRarity.Legendary, RewardEffect.DamageBonus, 5f));
        list.Add(Perm("vista_halcon", "Vista de Halcón", "+10% crítico mientras esté equipada.",
            RewardRarity.Legendary, RewardEffect.CritChanceBonus, 0.10f));
        list.Add(Perm("manantial_eterno", "Manantial Eterno", "+30% agua mientras esté equipado.",
            RewardRarity.Legendary, RewardEffect.WaterMultiplierBonus, 0.30f));
        list.Add(Perm("corazon_coco", "Corazón de Coco", "+12 stamina máxima mientras esté equipado.",
            RewardRarity.Legendary, RewardEffect.StaminaMaxBonus, 12f));
        list.Add(Perm("mango_colosal", "Mango Colosal", "+0.4 radio de golpe mientras esté equipado.",
            RewardRarity.Legendary, RewardEffect.HitRadiusBonus, 0.4f));
        list.Add(Perm("danza_machete", "Danza del Machete", "Golpea ~25% más seguido mientras esté equipada.",
            RewardRarity.Legendary, RewardEffect.SwingIntervalReduction, 0.25f));
        list.Add(Perm("rey_midas", "Rey Midas", "+15% prob. de moneda mientras esté equipado.",
            RewardRarity.Legendary, RewardEffect.CoinDropChanceBonus, 0.15f));

        return list;
    }
}
#endif