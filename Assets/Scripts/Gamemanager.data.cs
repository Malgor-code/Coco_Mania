using System.Collections.Generic;
using UnityEngine;
using static MacheteData;

public partial class GameManager
{
    [Header("Costos del arbol (exponenciales por profundidad)")]
    [Tooltip("Si esta activo, el costo de cada mejora se calcula solo: treeCostBase * treeCostGrowth ^ profundidad. Desactivalo para usar costos manuales.")]
    public bool autoTreeCosts = true;
    [Tooltip("Costo de las mejoras raiz. Aprox. 35-40% del ingreso del primer dia.")]
    public float treeCostBase = 50f;
    [Tooltip("Crecimiento por nivel en el INICIO del arbol (hasta treeCostLateStartDepth). Bajo = inicio facil.")]
    public float treeCostGrowth = 1.5f;
    [Tooltip("Crecimiento por nivel despues de treeCostLateStartDepth. Alto = el late game no se vuelve barato.")]
    public float treeCostGrowthLate = 2.4f;
    [Tooltip("Profundidad donde el crecimiento cambia de treeCostGrowth a treeCostGrowthLate.")]
    public int treeCostLateStartDepth = 4;

    void EnsureDefaultData()
    {
        static SkillNode N(string id, string name, string desc, int cost, SkillEffect effect, float value, params string[] pre)
        {
            return new SkillNode { id = id, nodeName = name, description = desc, cost = cost, prerequisiteIds = pre, effect = effect, effectValue = value };
        }
        if (forceDefaultData || macheteOptions == null || macheteOptions.Count == 0)
        {
            macheteOptions = new List<MacheteData>
{
    new MacheteData { macheteName = "Machete Oxidado",      description = "El que ya tienes.",                     baseDamage = 5f,   baseSwingInterval = 1.2f,  baseHitRadius = .8f,  unlockCost = 0 },
    new MacheteData { macheteName = "Machete Normal",       description = "Mas daño, mas rapido, mas rango.",      baseDamage = 9f,   baseSwingInterval = 1.1f,  baseHitRadius = 1.2f,  unlockCost = 2500 },
    new MacheteData { macheteName = "Machete de Acero",     description = "Un salto grande de poder.",             baseDamage = 14f,  baseSwingInterval = 1.05f, baseHitRadius = 1.5f, unlockCost = 10000,
        passive = MachetePassive.StaminaMax,   passiveValue = 6f,    passiveDescription = "Acero Templado: +6 de resistencia maxima." },
    new MacheteData { macheteName = "Machete de Oro",       description = "Brilla y pega fuerte.",                 baseDamage = 22f,  baseSwingInterval = 1.0f,  baseHitRadius = 1.8f,  unlockCost = 30000,
        passive = MachetePassive.WaterBonus,   passiveValue = 0.15f, passiveDescription = "Toque Dorado: +15% de agua por coco." },
    new MacheteData { macheteName = "Machete de Obsidiana", description = "Corta como el vidrio. Pega muy duro.", baseDamage = 35f,  baseSwingInterval = 0.95f, baseHitRadius = 2f, unlockCost = 80000,
        passive = MachetePassive.CritChance,   passiveValue = 0.10f, passiveDescription = "Filo de Vidrio: +10% de probabilidad de critico." },
    new MacheteData { macheteName = "Machete de Maestro",   description = "La obra final del herrero.",            baseDamage = 50f,  baseSwingInterval = 0.9f,  baseHitRadius = 2.1f,  unlockCost = 200000,
        passive = MachetePassive.ChainSplash,  passiveValue = 0.25f, passiveDescription = "Obra Maestra: al matar un coco, los cercanos reciben 25% del daño." },
    new MacheteData { macheteName = "Machete Volcanico",    description = "Forjado en lava. Los criticos arden.",   baseDamage = 70f,  baseSwingInterval = 0.88f, baseHitRadius = 2.2f,  unlockCost = 450000,
        passive = MachetePassive.CritDamage,   passiveValue = 0.5f,  passiveDescription = "Brasas: los criticos pegan +0.5x mas." },
    new MacheteData { macheteName = "Machete Abisal",       description = "Viene de lo mas hondo del coco.",        baseDamage = 95f,  baseSwingInterval = 0.86f, baseHitRadius = 2.4f,  unlockCost = 1000000,
        passive = MachetePassive.JackpotChance, passiveValue = 0.08f, passiveDescription = "Sed del Abismo: +8% de probabilidad de Jackpot." },
    new MacheteData { macheteName = "Machete Ancestral",    description = "El primer machete. Resuena en todos.",   baseDamage = 130f, baseSwingInterval = 0.84f, baseHitRadius = 2.5f,  unlockCost = 2500000,
        passive = MachetePassive.MultiHit,     passiveValue = 0.25f, passiveDescription = "Eco Ancestral: +25% de daño al golpear 2 o mas cocos a la vez." },
};
        }

        if (forceDefaultData || skillTree == null || skillTree.Count == 0)
        {
            skillTree = new List<SkillNode>
        {
            N("fuerza_1", "Mas Fuerza I",   "+2 de daño. Abre el resto del arbol.", 50,    SkillEffect.DamageFlatBonus, 2f),
            N("fuerza_2", "Mas Fuerza II",  "+3 de daño",  900,   SkillEffect.DamageFlatBonus, 3f,  "fuerza_1"),
            N("fuerza_3", "Mas Fuerza III", "+5 de daño",  4000,  SkillEffect.DamageFlatBonus, 5f,  "fuerza_2"),
            N("fuerza_4", "Mas Fuerza IV",  "+8 de daño",  15000, SkillEffect.DamageFlatBonus, 8f,  "fuerza_3"),
            N("fuerza_5", "Mas Fuerza V",   "+12 de daño", 40000, SkillEffect.DamageFlatBonus, 12f, "fuerza_4"),
            N("fuerza_6", "Mas Fuerza VI",  "+15 de daño", 90000,  SkillEffect.DamageFlatBonus, 15f, "fuerza_5"),
            N("fuerza_7", "Mas Fuerza VII", "+25 de daño", 160000, SkillEffect.DamageFlatBonus, 25f, "fuerza_6"),

            N("velocidad_1", "Manos Rapidas I",   "Golpea 0.1s mas rapido", 150,   SkillEffect.SwingIntervalReduction, 0.10f, "fuerza_1"),
            N("velocidad_2", "Manos Rapidas II",  "Golpea 0.1s mas rapido", 1300,  SkillEffect.SwingIntervalReduction, 0.10f, "velocidad_1"),
            N("velocidad_3", "Manos Rapidas III", "Golpea 0.1s mas rapido", 25000, SkillEffect.SwingIntervalReduction, 0.10f, "velocidad_2"),
            N("velocidad_4", "Manos Rapidas IV",  "Golpea 0.1s mas rapido", 45000, SkillEffect.SwingIntervalReduction, 0.10f, "velocidad_3"),
            N("velocidad_5", "Manos Rapidas V",   "Golpea 0.1s mas rapido", 80000, SkillEffect.SwingIntervalReduction, 0.10f, "velocidad_4"),

            N("suerte_1", "Buen Ojo I",  "+10% de agua por coco", 500,  SkillEffect.MoneyMultiplierBonus, 0.10f, "fuerza_2"),
            N("suerte_2", "Buen Ojo II", "+15% de agua por coco", 5000, SkillEffect.MoneyMultiplierBonus, 0.15f,  "suerte_1"),
            N("suerte_3", "Buen Ojo III", "+20% de agua por coco", 15000, SkillEffect.WaterMultiplierBonus, 0.20f, "suerte_2"),
            N("suerte_4", "Buen Ojo IV", "+25% de agua por coco", 60000, SkillEffect.WaterMultiplierBonus, 0.25f, "suerte_3"),
            N("suerte_5", "Buen Ojo V", "+30% de agua por coco", 100000, SkillEffect.WaterMultiplierBonus, 0.3f, "suerte_4"),

            N("radio_1", "Golpe Amplio I",  "Mas radio de golpe",     1200, SkillEffect.HitRadiusBonus, 0.2f, "velocidad_1"),
            N("radio_2", "Golpe Amplio II", "Aun mas radio de golpe", 2200, SkillEffect.HitRadiusBonus, 0.3f, "radio_1"),
            N("radio_3", "Golpe Amplio III", "Mas radio de golpe", 10000,  SkillEffect.HitRadiusBonus, 0.20f, "protegida_2"),
            N("radio_4", "Golpe Amplio IV",  "Mas radio de golpe", 20000, SkillEffect.HitRadiusBonus, 0.25f, "radio_3"),
            N("radio_5", "Golpe Amplio V",   "Mas radio de golpe", 35000, SkillEffect.HitRadiusBonus, 0.30f, "radio_4"),
            N("radio_6", "Golpe Amplio VI",  "Mas radio de golpe", 80000, SkillEffect.HitRadiusBonus, 0.35f, "radio_5"),

            N("multi_1", "Golpe Multiple I",   "+5% de daño si golpeas 2 o mas cocos a la vez",     15000,  SkillEffect.MultiHitDamageBonus, 0.05f, "radio_3"),
            N("multi_2", "Golpe Multiple II",  "+10% mas (total +15%) al golpear 2 o mas cocos",     40000, SkillEffect.MultiHitDamageBonus, 0.10f, "multi_1"),
            N("multi_3", "Golpe Multiple III", "+15% mas (total +30%) al golpear 2 o mas cocos",     90000, SkillEffect.MultiHitDamageBonus, 0.15f, "multi_2"),
            N("multi_4", "Golpe Multiple IV", "+20% mas (total +50%) al golpear 2 o mas cocos", 150000, SkillEffect.MultiHitDamageBonus, 0.2f, "multi_3"),
            N("multi_5", "Golpe Multiple V", "+25% mas (total +75%) al golpear 2 o mas cocos", 300000, SkillEffect.MultiHitDamageBonus, 0.25f, "multi_4"),

            N("eficiencia_1", "Segundo Aliento I",   "9% de probabilidad de recuperar 1 de resistencia al destruir un coco", 750,   SkillEffect.StaminaRecoveryChanceBonus, 0.09f, "resistencia_2"),
            N("eficiencia_2", "Segundo Aliento II",  "+8% de probabilidad (total 17%)", 3500,  SkillEffect.StaminaRecoveryChanceBonus, 0.08f, "eficiencia_1"),
            N("eficiencia_3", "Segundo Aliento III", "+7% de probabilidad (total 24%)", 9000,  SkillEffect.StaminaRecoveryChanceBonus, 0.07f, "eficiencia_2"),
            N("eficiencia_4", "Segundo Aliento IV",  "+6% de probabilidad (total 30%)", 22000, SkillEffect.StaminaRecoveryChanceBonus, 0.06f, "eficiencia_3"),
            N("eficiencia_5", "Segundo Aliento V",   "+5% de probabilidad (total 35%, el maximo)", 50000, SkillEffect.StaminaRecoveryChanceBonus, 0.05f, "eficiencia_4"),

            N("resistencia_1", "Aguante I",   "+5 segundos de resistencia", 450,   SkillEffect.StaminaMaxFlatBonus, 5f, "fuerza_1"),
            N("resistencia_2", "Aguante II",  "+5 segundos de resistencia", 1800,  SkillEffect.StaminaMaxFlatBonus, 5f, "resistencia_1"),
            N("resistencia_3", "Aguante III", "+5 segundos de resistencia", 4500,  SkillEffect.StaminaMaxFlatBonus, 5f, "resistencia_2"),
            N("resistencia_4", "Aguante IV",  "+5 segundos de resistencia", 12000, SkillEffect.StaminaMaxFlatBonus, 5f, "resistencia_3"),
            N("resistencia_5", "Aguante V",   "+6 segundos de resistencia", 60000, SkillEffect.StaminaMaxFlatBonus, 6f, "resistencia_4"),
            N("resistencia_6", "Aguante VI", "+8 segundos de resistencia", 150000, SkillEffect.StaminaMaxFlatBonus, 8f, "resistencia_5"),

            N("recuperacion_1", "Aliento Profundo I",   "Cada recuperacion da +1.6 de resistencia (total 2.6)", 1500,  SkillEffect.StaminaRecoveryAmountBonus, 1.6f, "eficiencia_1"),
            N("recuperacion_2", "Aliento Profundo II",  "+1.5 por recuperacion (total 4.1)",  5000,  SkillEffect.StaminaRecoveryAmountBonus, 1.5f, "recuperacion_1"),
            N("recuperacion_3", "Aliento Profundo III", "+1.4 por recuperacion (total 5.5)",  14000, SkillEffect.StaminaRecoveryAmountBonus, 1.4f, "recuperacion_2"),
            N("recuperacion_4", "Aliento Profundo IV",  "+1.3 por recuperacion (total 6.8)",  32000, SkillEffect.StaminaRecoveryAmountBonus, 1.3f, "recuperacion_3"),
            N("recuperacion_5", "Aliento Profundo V",   "+1.2 por recuperacion (total 8, el maximo)", 65000, SkillEffect.StaminaRecoveryAmountBonus, 1.2f, "recuperacion_4"),

            N("cocos_1", "Cosecha Inicial",     "+1 coco al iniciar el dia", 400,  SkillEffect.ExtraStartingCoconut, 1f, "fuerza_1"),
            N("cocos_2", "Mejores vendedores",  "+2 cocos al iniciar el dia", 950,  SkillEffect.ExtraStartingCoconut, 2f, "aparicion_1"),
            N("cocos_3", "Cocos locos",         "+3 cocos al iniciar el dia", 2300, SkillEffect.ExtraStartingCoconut, 3f, "aparicion_2"),
            N("cocos_4", "Cocos locos II", "+2 cocos al iniciar el dia", 20000, SkillEffect.ExtraStartingCoconut, 2f, "cocos_3"),

            N("aparicion_1", "Cosecha Rapida",     "DESBLOQUEA la aparicion de cocos durante el dia: aparece uno nuevo cada 10 s (antes solo salen los del inicio)", 600,  SkillEffect.SpawnIntervalReduction, 0f, "cocos_1"),
            N("aparicion_2", "Cosecha Rapidisima", "Los cocos aparecen mas seguido (-1 s entre cocos)", 1300, SkillEffect.SpawnIntervalReduction, 1f, "cocos_2"),
            N("aparicion_3", "Cosecha Veloz",      "Los cocos aparecen mas seguido (-2 s entre cocos)", 4000, SkillEffect.SpawnIntervalReduction,2f, "cocos_3"),
            N("aparicion_4", "Cosecha muy veloz",      "Los cocos aparecen mas seguido (-2 s entre cocos)", 10000, SkillEffect.SpawnIntervalReduction,2f, "aparicion_3"),
            N("aparicion_5", "Cosecha final", "Los cocos aparecen mas seguido (-1 s entre cocos)", 25000, SkillEffect.SpawnIntervalReduction, 1f, "aparicion_4"),

            N("leche_1", "Coco Bendito I",   "+10% de agua de coco", 500,   SkillEffect.WaterMultiplierBonus, 0.1f, "velocidad_2"),
            N("leche_2", "Coco Bendito II",  "+15% de agua de coco", 1500,  SkillEffect.WaterMultiplierBonus, 0.15f,  "leche_1"),
            N("leche_3", "Coco Bendito III", "+20% de agua de coco", 5000,  SkillEffect.WaterMultiplierBonus, 0.20f,  "leche_2"),
            N("leche_4", "Coco Bendito IV",  "+25% de agua de coco", 30000, SkillEffect.WaterMultiplierBonus, 0.25f,  "leche_3"),
            N("leche_5", "Coco Bendito V", "+30% de agua de coco", 90000, SkillEffect.WaterMultiplierBonus, 0.3f, "leche_4"),
            N("leche_6", "Coco Bendito VI", "+35% de agua de coco", 180000, SkillEffect.WaterMultiplierBonus, 0.35f, "leche_5"),

            N("moneda_1", "Golpe de Suerte I",   "+7% de probabilidad de Jackpot de Agua al destruir un coco", 1000,   SkillEffect.JackpotChanceBonus, 0.07f, "velocidad_2", "radio_1"),
            N("moneda_2", "Golpe de Suerte II",  "+4% de probabilidad de Jackpot (total 11%)",  9000,   SkillEffect.JackpotChanceBonus, 0.04f, "moneda_1", "radio_2"),
            N("moneda_3", "Golpe de Suerte III", "+6% de probabilidad de Jackpot (total 17%)",  14000,  SkillEffect.JackpotChanceBonus, 0.06f, "moneda_2"),
            N("moneda_4", "Golpe de Suerte IV",  "+8% de probabilidad de Jackpot (total 25%)",  30000,  SkillEffect.JackpotChanceBonus, 0.08f, "moneda_3"),
            N("moneda_5", "Golpe de Suerte V",   "+10% de probabilidad de Jackpot (total 35%)", 60000, SkillEffect.JackpotChanceBonus, 0.1f,  "moneda_4"),

            N("jackpot_1", "Botin de Jackpot I",  "El Jackpot da +1x mas agua (total x2.5 del coco)",   6000,  SkillEffect.JackpotWaterBonus, 1f,   "moneda_2"),
            N("jackpot_2", "Botin de Jackpot II", "El Jackpot da +1.5x mas agua (total x4 del coco)",   40000, SkillEffect.JackpotWaterBonus, 1.5f, "jackpot_1"),
            N("jackpot_3", "Botin de Jackpot III", "El Jackpot da +2x mas agua (total x6 del coco)", 110000, SkillEffect.JackpotWaterBonus, 2f, "jackpot_2", "moneda_5"),

            N("critico_1", "Golpe Certero I",   "+5% de probabilidad de golpe crítico.",  2000,  SkillEffect.CritChanceBonus, 0.05f, "leche_1"),
            N("critico_2", "Golpe Certero II",  "+7% de probabilidad de golpe crítico(total 12%)",  8000,  SkillEffect.CritChanceBonus, 0.07f, "critico_1"),
            N("critico_3", "Golpe Certero III", "+8% de probabilidad de golpe crítico(total 20%)",  25000, SkillEffect.CritChanceBonus, 0.08f, "critico_2"),
            N("critico_4", "Golpe Certero IV",  "+10% de probabilidad de golpe crítico(total 30%)", 80000, SkillEffect.CritChanceBonus, 0.10f, "critico_3"),
            N("critico_5", "Golpe Certero V",   "+10% de probabilidad de golpe crítico(total 40%)", 150000, SkillEffect.CritChanceBonus, 0.1f, "critico_4"),
            N("critico_6", "Golpe Certero VI", "+15% de probabilidad de golpe crítico(total 55%)", 300000, SkillEffect.CritChanceBonus, 0.15f, "critico_5"),

            N("critico_dano_1", "Golpe Demoledor I",   "Los criticos pegan +0.1x mas (de x1.3 a x1.4)", 6000,   SkillEffect.CritDamageBonus, 0.10f, "critico_2"),
            N("critico_dano_2", "Golpe Demoledor II",  "+0.15x mas (total x1.55)", 14000,  SkillEffect.CritDamageBonus, 0.15f, "critico_dano_1", "critico_3"),
            N("critico_dano_3", "Golpe Demoledor III", "+0.2x mas (total x1.75)",  30000,  SkillEffect.CritDamageBonus, 0.20f, "critico_dano_2"),
            N("critico_dano_4", "Golpe Demoledor IV",  "+0.3x mas (total x2.05)",  55000,  SkillEffect.CritDamageBonus, 0.30f, "critico_dano_3"),
            N("critico_dano_5", "Golpe Demoledor V",   "+0.35x mas (total x2.4)",  100000, SkillEffect.CritDamageBonus, 0.35f, "critico_dano_4"),
            N("critico_dano_6", "Golpe Demoledor VI",  "+0.4x mas (total x2.8)",   170000, SkillEffect.CritDamageBonus, 0.40f, "critico_dano_5"),

            N("extra_1", "Cocos Gemelos I",   "5% de probabilidad de que aparezca otro coco al romper uno", 800,   SkillEffect.ExtraCoconutChanceBonus, 0.05f, "resistencia_3"),
            N("extra_2", "Cocos Gemelos II",  "+5% (total 10%)", 2000,  SkillEffect.ExtraCoconutChanceBonus, 0.05f, "extra_1"),
            N("extra_3", "Cocos Gemelos III", "+5% (total 15%)", 7000,  SkillEffect.ExtraCoconutChanceBonus, 0.05f, "extra_2"),
            N("extra_4", "Cocos Gemelos IV",  "+5% (total 20%)", 13500, SkillEffect.ExtraCoconutChanceBonus, 0.05f, "extra_3"),
            N("extra_5", "Cocos Gemelos V", "+5% (total 25%)", 30000, SkillEffect.ExtraCoconutChanceBonus, 0.05f, "extra_4"),
            N("extra_6", "Cocos Gemelos VI", "+5% (total 30%)", 60000, SkillEffect.ExtraCoconutChanceBonus, 0.05f, "extra_5"),
            N("extra_7", "Cocos Gemelos VII", "+5% (total 35%)", 120000, SkillEffect.ExtraCoconutChanceBonus, 0.05f, "extra_6"),

            N("protegida_1", "Racha Protegida I", "Un fallo por dia no rompe tu racha", 1800, SkillEffect.StreakForgiveness, 1f, "radio_2", "racha_1"),
            N("protegida_2", "Racha Protegida II", "Dos fallos por dia no rompen tu racha", 7500, SkillEffect.StreakForgiveness, 1f, "protegida_1"),
            N("protegida_3", "Racha Protegida III", "Tres fallos por dia no rompen tu racha", 20000, SkillEffect.StreakForgiveness, 1f, "protegida_2"),

            N("negociador_1", "Negociador I",   "-3% al pedido actual y a los siguientes", 2500,   SkillEffect.OrderDiscount, 0.03f, "suerte_1"),
            N("negociador_2", "Negociador II",  "-5% mas al pedido",  9000,   SkillEffect.OrderDiscount, 0.05f, "negociador_1", "suerte_1"),
            N("negociador_3", "Negociador III", "-7% mas al pedido", 24000,  SkillEffect.OrderDiscount, 0.07f,  "negociador_2"),
            N("negociador_4", "Negociador IV",  "-10% mas al pedido", 100000, SkillEffect.OrderDiscount, 0.1f, "negociador_3"),
            N("negociador_5", "Negociador V", "-5% mas al pedido", 220000, SkillEffect.OrderDiscount, 0.05f, "negociador_4"),

            N("aliento_1", "Aliento Final I",  "Con menos del 20% de resistencia: +20% de agua", 4500,  SkillEffect.LastBreathBonus, 0.2f, "suerte_2"),
            N("aliento_2", "Aliento Final II", "Con menos del 20% de resistencia: +40% de agua (total)", 12000, SkillEffect.LastBreathBonus, 0.2f, "aliento_1"),
            N("aliento_3",   "Aliento Final III", "Con menos del 20% de resistencia: +60% de agua (total)", 30000, SkillEffect.LastBreathBonus, 0.2f, "aliento_2"),

            N("cadena_1", "Golpe en Cadena I",  "Al matar un coco, los cercanos reciben 15% del daño", 7000,  SkillEffect.ChainSplash, 0.15f, "extra_1"),
            N("cadena_2", "Golpe en Cadena II", "+10% de daño en cadena (total 25%)", 22000, SkillEffect.ChainSplash, 0.10f, "cadena_1"),
            N("cadena_3", "Golpe en Cadena III", "+10% de daño en cadena (total 35%)", 60000, SkillEffect.ChainSplash, 0.10f, "cadena_2"),

            N("manofirme_1", "Mano Firme I",  "Con menos del 20% de resistencia: golpeas 0.15s más rápido", 5000,  SkillEffect.LastBreathSwingBonus, 0.15f, "aliento_1"),
            N("manofirme_2", "Mano Firme II", "Con menos del 20% de resistencia: 0.3s más rápido (total)",  13500, SkillEffect.LastBreathSwingBonus, 0.15f, "manofirme_1"),
            N("manofirme_3", "Mano Firme III",    "Con menos del 20% de resistencia: 0.4s mas rapido (total)", 30000, SkillEffect.LastBreathSwingBonus, 0.10f, "manofirme_2"),

            N("filo_1", "Filo Afilado I",   "+5% de daño a cocos con menos del 50% de vida", 20000,  SkillEffect.ExecuteDamageBonus, 0.05f, "fuerza_5"),
            N("filo_2", "Filo Afilado II",  "+5% más (total 10%)", 30000, SkillEffect.ExecuteDamageBonus, 0.05f,  "filo_1"),
            N("filo_3", "Filo Afilado III", "+10% más (total 20%)", 45000, SkillEffect.ExecuteDamageBonus, 0.1f, "filo_2"),
            N("filo_4", "Filo Afilado IV", "+15% mas (total 35%)", 80000, SkillEffect.ExecuteDamageBonus, 0.15f, "filo_3"),

            N("reembolso_1", "Pedido Flexible I",  "Al entregar un pedido, recuperas 5% de lo entregado", 20000,  SkillEffect.OrderRefundBonus, 0.05f, "negociador_3"),
            N("reembolso_2", "Pedido Flexible II", "+5% más, total 15%",                                 50000, SkillEffect.OrderRefundBonus, 0.05f, "reembolso_1"),
            N("reembolso_3", "Pedido Flexible III", "+5% mas, total 15%", 120000, SkillEffect.OrderRefundBonus, 0.05f, "reembolso_2"),

            N("calma_1", "Calma Antes de la Tormenta I",   "+10% de agua en los primeros 5 segundos del día", 3000,  SkillEffect.EarlyDayWaterBonus, 0.10f, "manofirme_1"),
            N("calma_2", "Calma Antes de la Tormenta II",  "+15% más (total +25%)", 8000,  SkillEffect.EarlyDayWaterBonus, 0.15f, "calma_1"),
            N("calma_3", "Calma Antes de la Tormenta III", "+20% más (total +45%)", 18000, SkillEffect.EarlyDayWaterBonus, 0.2f, "calma_2"),
            N("calma_4", "Calma Antes de la Tormenta IV", "+25% mas (total +70%)", 40000, SkillEffect.EarlyDayWaterBonus, 0.25f, "calma_3"),
            N("calma_5", "Calma Antes de la Tormenta V", "+30% mas (total +100%)", 120000, SkillEffect.EarlyDayWaterBonus, 0.3f, "calma_4"),

            N("racha_1", "Racha Ardiente I",   "DESBLOQUEA la racha: desde el 3er golpe seguido que conecte, cada golpe da +5% de daño y agua (hasta 4 veces)", 1500, SkillEffect.StreakMaxStacksBonus, 4f, "radio_1"),
            N("racha_2", "Racha Ardiente II",  "+2 acumulaciones maximas de racha", 8000,  SkillEffect.StreakMaxStacksBonus, 2f, "racha_1"),
            N("racha_3", "Racha Ardiente III", "+2 acumulaciones maximas de racha", 30000, SkillEffect.StreakMaxStacksBonus, 2f, "racha_2"),
            N("racha_4", "Racha Ardiente IV", "+2 acumulaciones maximas de racha", 60000, SkillEffect.StreakMaxStacksBonus, 2f, "racha_3"),

            N("rachapoder_1", "Racha Poderosa I",   "Cada acumulacion de racha da +1% mas de daño y agua",   6000,  SkillEffect.StreakPowerBonus, 0.01f,  "racha_1"),
            N("rachapoder_2", "Racha Poderosa II",  "+1% mas por acumulacion",                               25000, SkillEffect.StreakPowerBonus, 0.01f,  "rachapoder_1", "racha_2"),
            N("rachapoder_3", "Racha Poderosa III", "+1.5% mas por acumulacion",                             70000, SkillEffect.StreakPowerBonus, 0.015f, "rachapoder_2", "racha_3"),
            N("rachapoder_4", "Racha Poderosa IV", "+1.5% mas por acumulacion (llega al tope de 10%)", 110000, SkillEffect.StreakPowerBonus, 0.015f, "rachapoder_3"),

            N("tesoro_1", "Ojo de Tesorero I",   "+1% de probabilidad de encontrar monedas",   2000,  SkillEffect.CoinChanceBonus, 0.01f,  "suerte_1"),
            N("tesoro_2", "Ojo de Tesorero II",  "+1.5% de probabilidad de encontrar monedas", 8000,  SkillEffect.CoinChanceBonus, 0.015f, "tesoro_1"),
            N("tesoro_3", "Ojo de Tesorero III", "+2% de probabilidad de encontrar monedas",   25000, SkillEffect.CoinChanceBonus, 0.02f,  "tesoro_2"),
            N("tesoro_4", "Ojo de Tesorero IV", "+2.5% de probabilidad de encontrar monedas", 60000, SkillEffect.CoinChanceBonus, 0.025f, "tesoro_3"),

                };
        }

        if (autoTreeCosts) ApplyExponentialTreeCosts();

        if (forceDefaultData || perkPool == null || perkPool.Count == 0)
        {
            perkPool = new List<PerkOption>
        {
            new PerkOption { perkName = "Manos Firmes",    description = "+4 daño.",                          effect = PerkEffect.DamageBonus,           value = 4f },
            new PerkOption { perkName = "Segundo Aire",    description = "+8 stamina máxima.",                effect = PerkEffect.StaminaMaxBonus,       value = 8f },
            new PerkOption { perkName = "Buen Trato",      description = "+15% agua obtenida.",               effect = PerkEffect.WaterMultiplierBonus,  value = 0.15f },
            new PerkOption { perkName = "Reflejos",        description = "-0.08 segundos entre golpes.",      effect = PerkEffect.SwingIntervalReduction,value = 0.08f },
            new PerkOption { perkName = "Golpe Certero",   description = "+6% de probabilidad de crítico.",   effect = PerkEffect.CritChanceBonus,       value = 0.06f },
            new PerkOption { perkName = "Cosecha Extra",   description = "+2 cocos iniciales cada día (esta partida).", effect = PerkEffect.ExtraCoconutNextCycle, value = 2f },
            new PerkOption { perkName = "Golpe de Suerte", description = "+10% de probabilidad de crítico.",  effect = PerkEffect.CritChanceBonus,       value = 0.10f },
            new PerkOption { perkName = "Coco Generoso",   description = "+25% agua, pero los golpes son 0.12s mas lentos.", effect = PerkEffect.WaterForSwingPenalty, value = 0.25f },
            new PerkOption { perkName = "Cosecha Abundante", description = "+3 cocos iniciales cada día (esta partida).", effect = PerkEffect.ExtraCoconutNextCycle, value = 3f },
            new PerkOption { perkName = "Manos de Hierro",   description = "+10 daño.",              effect = PerkEffect.DamageBonus,          value = 10f },
            new PerkOption { perkName = "Pulmones de Acero", description = "+12 stamina máxima.",    effect = PerkEffect.StaminaMaxBonus,      value = 12f },
            new PerkOption { perkName = "Pacto del Cobrador",description = "+25% agua obtenida.",    effect = PerkEffect.WaterMultiplierBonus, value = 0.25f },
            new PerkOption { perkName = "Ojo de Halcón",     description = "+12% de probabilidad de crítico.", effect = PerkEffect.CritChanceBonus, value = 0.12f },
            new PerkOption { perkName = "Coco Dorado",   description = "+50% agua, pero los golpes son 0.24s mas lentos.", effect = PerkEffect.WaterForSwingPenalty, value = 0.50f },
            new PerkOption { perkName = "Manos de Titán",description = "+18 daño.",                   effect = PerkEffect.DamageBonus, value = 18f },
            new PerkOption { perkName = "Ojo de Águila",      description = "+0.25 de radio de golpe.",                          effect = PerkEffect.HitRadiusBonus,        value = 0.25f },
            new PerkOption { perkName = "Moneda de la Suerte",description = "+1% de probabilidad de encontrar monedas.",         effect = PerkEffect.CoinChanceBonus,       value = 0.01f },
            new PerkOption { perkName = "Madrugador",         description = "+20% de agua en los primeros segundos del día.",    effect = PerkEffect.EarlyDayWaterBonus,    value = 0.20f },
            new PerkOption { perkName = "Buen Regateo",       description = "-4% al pedido actual y a los siguientes.",         effect = PerkEffect.OrderDiscount,         value = 0.04f },
            new PerkOption { perkName = "Racha Viva",         description = "+1 acumulación máxima de racha.",                   effect = PerkEffect.StreakMaxStacksBonus,  value = 1f },
            new PerkOption { perkName = "Pulso Firme",        description = "+1.5% de daño y agua por acumulación de racha.",    effect = PerkEffect.StreakPowerBonus,      value = 0.015f },
            new PerkOption { perkName = "Doble Golpe",        description = "+12% de daño al golpear 2 o más cocos a la vez.",   effect = PerkEffect.MultiHitDamageBonus,   value = 0.12f },
            new PerkOption { perkName = "Corazón de Jackpot", description = "+5% de probabilidad de Jackpot de agua.",           effect = PerkEffect.JackpotChanceBonus,    value = 0.05f },
            new PerkOption { perkName = "Último Aliento",     description = "Con poca resistencia: +15% de agua.",               effect = PerkEffect.LastBreathBonus,       value = 0.15f },
            new PerkOption { perkName = "Machete Pesado",     description = "+8 daño, pero -4 de stamina máxima.",               effect = PerkEffect.DamageForStaminaPenalty, value = 8f },
            new PerkOption { perkName = "Instinto Cazador",   description = "+8% de daño a cocos con menos del 50% de vida.",    effect = PerkEffect.ExecuteDamageBonus,    value = 0.08f },
            new PerkOption { perkName = "Chispa en Cadena",   description = "Al matar un coco, los cercanos reciben +12% del daño.", effect = PerkEffect.ChainSplash,       value = 0.12f },
            new PerkOption { perkName = "Racha Imparable",    description = "+2 acumulaciones máximas de racha.",                effect = PerkEffect.StreakMaxStacksBonus,  value = 2f },
            new PerkOption { perkName = "Jackpot Mayor",      description = "+10% de probabilidad de Jackpot de agua.",          effect = PerkEffect.JackpotChanceBonus,    value = 0.10f },
            new PerkOption { perkName = "Cazador Maestro",    description = "+15% de daño a cocos con menos del 50% de vida.",   effect = PerkEffect.ExecuteDamageBonus,    value = 0.15f },
        };
        }

        if (forceDefaultData || legacyShop == null || legacyShop.Count == 0)
        {
            legacyShop = new List<LegacyItem>
        {
            new LegacyItem { itemName = "Anillo del Machetero",   description = "+1 daño permanente.",                      cost = 1, effect = LegacyEffect.PermanentDamageBonus,           value = 1f },
            new LegacyItem { itemName = "Pulsera de Aguante",     description = "+3 stamina máxima permanente.",            cost = 2, effect = LegacyEffect.PermanentStaminaMaxBonus,       value = 3f },
            new LegacyItem { itemName = "Amuleto del Cobrador",   description = "+5% agua obtenida permanentemente.",       cost = 3, effect = LegacyEffect.PermanentMoneyMultiplierBonus,  value = 0.05f },
            new LegacyItem { itemName = "Cesta Grande",           description = "+1 coco inicial cada día.",                cost = 4, effect = LegacyEffect.PermanentStartingCoconutBonus,  value = 1f },
            new LegacyItem { itemName = "Lente del Afortunado",   description = "+2% probabilidad de crítico permanente.",  cost = 2, effect = LegacyEffect.PermanentCritChanceBonus,       value = 0.02f },
            new LegacyItem { itemName = "Moneda Antigua",         description = "+0.5% probabilidad de encontrar monedas.", cost = 1, effect = LegacyEffect.PermanentCoinChanceBonus,       value = 0.005f },
            new LegacyItem { itemName = "Anillo del Machetero II",   description = "+1 daño permanente.",                    cost = 3, effect = LegacyEffect.PermanentDamageBonus,          value = 1f },
            new LegacyItem { itemName = "Pulsera de Aguante II",     description = "+4 stamina máxima permanente.",          cost = 4, effect = LegacyEffect.PermanentStaminaMaxBonus,      value = 4f },
            new LegacyItem { itemName = "Amuleto del Cobrador II",   description = "+8% agua obtenida permanentemente.",     cost = 6, effect = LegacyEffect.PermanentMoneyMultiplierBonus, value = 0.08f },
            new LegacyItem { itemName = "Cesta Grande II",           description = "+1 coco inicial cada día.",              cost = 6, effect = LegacyEffect.PermanentStartingCoconutBonus, value = 1f },
            new LegacyItem { itemName = "Lente del Afortunado II",   description = "+3% probabilidad de crítico permanente.",cost = 5, effect = LegacyEffect.PermanentCritChanceBonus,      value = 0.03f },
            new LegacyItem { itemName = "Moneda Antigua II",         description = "+1% probabilidad de encontrar monedas.", cost = 3, effect = LegacyEffect.PermanentCoinChanceBonus,      value = 0.01f },
            new LegacyItem { itemName = "Anillo del Machetero III",  description = "+2 daño permanente.",                    cost = 6,  effect = LegacyEffect.PermanentDamageBonus,          value = 2f },
            new LegacyItem { itemName = "Pulsera de Aguante III",    description = "+6 stamina máxima permanente.",          cost = 8,  effect = LegacyEffect.PermanentStaminaMaxBonus,      value = 6f },
            new LegacyItem { itemName = "Amuleto del Cobrador III",  description = "+12% agua obtenida permanentemente.",    cost = 10, effect = LegacyEffect.PermanentMoneyMultiplierBonus, value = 0.12f },
            new LegacyItem { itemName = "Cesta Grande III",          description = "+1 coco inicial cada día.",              cost = 10, effect = LegacyEffect.PermanentStartingCoconutBonus, value = 1f },
            new LegacyItem { itemName = "Lente del Afortunado III",  description = "+5% probabilidad de crítico permanente.",cost = 8,  effect = LegacyEffect.PermanentCritChanceBonus,      value = 0.05f },
            new LegacyItem { itemName = "Moneda Antigua III",        description = "+2% probabilidad de encontrar monedas.", cost = 6,  effect = LegacyEffect.PermanentCoinChanceBonus,      value = 0.02f },
            new LegacyItem { itemName = "Lente del Afortunado IV",   description = "+6% probabilidad de crítico permanente.",cost = 12, effect = LegacyEffect.PermanentCritChanceBonus,     value = 0.06f },
            new LegacyItem { itemName = "Sello Ancestral I",   description = "+10% al valor de cada punto de legado.",  cost = 8,  effect = LegacyEffect.PermanentLegacyMultiplierBonus, value = 0.10f },
            new LegacyItem { itemName = "Sello Ancestral II",  description = "+20% al valor de cada punto de legado.",  cost = 16, effect = LegacyEffect.PermanentLegacyMultiplierBonus, value = 0.20f },
            new LegacyItem { itemName = "Sello Ancestral III", description = "+40% al valor de cada punto de legado.",  cost = 30, effect = LegacyEffect.PermanentLegacyMultiplierBonus, value = 0.40f },
            new LegacyItem { itemName = "Reloj de Arena I",       description = "+1 día por ciclo.",                          cost = 14, effect = LegacyEffect.PermanentExtraDay,        value = 1f },
            new LegacyItem { itemName = "Reloj de Arena II",      description = "+1 día por ciclo (total +2).",               cost = 40, effect = LegacyEffect.PermanentExtraDay,        value = 1f },
            new LegacyItem { itemName = "Barril de Reserva I",    description = "Empiezas cada partida con 100 mL.",          cost = 3,  effect = LegacyEffect.PermanentStartingWater,   value = 100f },
            new LegacyItem { itemName = "Barril de Reserva II",   description = "Empiezas con 250 mL.",                       cost = 6,  effect = LegacyEffect.PermanentStartingWater,   value = 150f },
            new LegacyItem { itemName = "Barril de Reserva III",  description = "Empiezas con 500 mL.",                       cost = 10, effect = LegacyEffect.PermanentStartingWater,   value = 250f },
            new LegacyItem { itemName = "Trébol de Agua I",       description = "+2% de probabilidad de Jackpot permanente.", cost = 4,  effect = LegacyEffect.PermanentJackpotChance,   value = 0.02f },
            new LegacyItem { itemName = "Trébol de Agua II",      description = "+3% de probabilidad de Jackpot permanente.", cost = 8,  effect = LegacyEffect.PermanentJackpotChance,   value = 0.03f },
            new LegacyItem { itemName = "Ritmo del Machetero I",  description = "+1 acumulación máxima de racha permanente.", cost = 4,  effect = LegacyEffect.PermanentStreakStacks,    value = 1f },
            new LegacyItem { itemName = "Ritmo del Machetero II", description = "+1 acumulación máxima de racha permanente.", cost = 8,  effect = LegacyEffect.PermanentStreakStacks,    value = 1f },
            new LegacyItem { itemName = "Ojo Largo I",            description = "+0.1 de radio de golpe permanente.",         cost = 3,  effect = LegacyEffect.PermanentHitRadius,       value = 0.1f },
            new LegacyItem { itemName = "Ojo Largo II",           description = "+0.15 de radio de golpe permanente.",        cost = 6,  effect = LegacyEffect.PermanentHitRadius,       value = 0.15f },
            new LegacyItem { itemName = "Contrato Favorable I",   description = "-3% a todos los pedidos.",                   cost = 6,  effect = LegacyEffect.PermanentOrderDiscount,   value = 0.03f },
            new LegacyItem { itemName = "Contrato Favorable II",  description = "-3% más a todos los pedidos.",               cost = 12, effect = LegacyEffect.PermanentOrderDiscount,   value = 0.03f },
            new LegacyItem { itemName = "Herencia del Herrero I", description = "Empiezas con el Machete Normal.",            cost = 14, effect = LegacyEffect.PermanentStartingMachete, value = 1f },
            new LegacyItem { itemName = "Herencia del Herrero II",description = "Empiezas con el Machete de Acero.",          cost = 50, effect = LegacyEffect.PermanentStartingMachete, value = 1f },
        };
        }
        StampCoconutUnlocks();
    }

    void ApplyExponentialTreeCosts()
    {
        if (skillTree == null || skillTree.Count == 0) return;

        var byId = new Dictionary<string, SkillNode>();
        foreach (var n in skillTree) byId[n.id] = n;
        var depth = new Dictionary<string, int>();

        int Depth(SkillNode n)
        {
            if (depth.TryGetValue(n.id, out int d)) return d;
            depth[n.id] = 0; 
            int best = 0;
            if (n.prerequisiteIds != null)
            {
                foreach (var p in n.prerequisiteIds)
                {
                    if (byId.TryGetValue(p, out var pn))
                        best = Mathf.Max(best, Depth(pn) + 1);
                }
            }
            depth[n.id] = best;
            return best;
        }

        foreach (var n in skillTree)
        {
            int dep = Depth(n);
            int early = Mathf.Min(dep, treeCostLateStartDepth);
            int late = Mathf.Max(0, dep - treeCostLateStartDepth);
            float raw = treeCostBase * Mathf.Pow(treeCostGrowth, early) * Mathf.Pow(treeCostGrowthLate, late);
            float mag = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(Mathf.Max(raw, 1f))) - 1f);
            n.cost = Mathf.Max(1, Mathf.RoundToInt(Mathf.Round(raw / mag) * mag));
        }
    }

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