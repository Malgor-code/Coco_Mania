using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Dialogo", menuName = "CocoMania/Dialogo")]
public class DialogueSO : ScriptableObject
{
    [Tooltip("Cada linea es una clave del .csv (por ejemplo story_intro_1) o texto directo.")]
    public List<string> lines = new List<string>();
    public string[] GetLines()
    {
        LocalizationManager loc = LocalizationManager.Instance;
        string[] result = new string[lines.Count];
        for (int i = 0; i < lines.Count; i++)
            result[i] = loc != null ? loc.Get(lines[i]) : lines[i];
        return result;
    }
}