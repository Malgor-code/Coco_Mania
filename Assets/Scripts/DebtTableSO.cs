using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DebtEntry
{
    [Tooltip("Dias que tienes para juntar esta deuda (sin contar los dias extra del Legado).")]
    public int days = 6;
    [Tooltip("Agua de coco (mL) que pide esta deuda antes de descuentos.")]
    public float waterTarget = 250f;
}

[CreateAssetMenu(fileName = "DebtTable", menuName = "CocoMania/Debt Table")]
public class DebtTableSO : ScriptableObject
{
    public List<DebtEntry> entries = new List<DebtEntry>();

    [Header("Modo infinito (despues de la ultima fila)")]
    [Tooltip("Cada deuda extra multiplica la anterior por este numero.")]
    public float endlessGrowth = 1.45f;
    public int endlessDays = 3;

    public DebtEntry Get(int cycle)
    {
        int index = Mathf.Max(1, cycle) - 1;
        if (entries.Count == 0) return new DebtEntry();
        if (index < entries.Count) return entries[index];

        DebtEntry last = entries[entries.Count - 1];
        return new DebtEntry
        {
            days = endlessDays,
            waterTarget = last.waterTarget * Mathf.Pow(endlessGrowth, index - (entries.Count - 1))
        };
    }

    [ContextMenu("Rellenar con la formula anterior (16 deudas)")]
    public void FillFromOldFormula()
    {
        const int count = 16;
        const float start = 250f, growth = 1.8f, lateGrowth = 1.45f;
        const int lateFrom = 8, baseDays = 6, minDays = 3;

        entries.Clear();
        float target = start;
        for (int n = 1; n <= count; n++)
        {
            if (n > 1) target *= (n > lateFrom) ? lateGrowth : growth;
            entries.Add(new DebtEntry
            {
                days = Mathf.Max(minDays, baseDays - (n - 1) / 2),
                waterTarget = Mathf.Round(target)
            });
        }
    }

    public static DebtTableSO CreateDefault()
    {
        DebtTableSO t = CreateInstance<DebtTableSO>();
        t.hideFlags = HideFlags.DontSave;
        t.FillFromOldFormula();
        return t;
    }
}