using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StoryCatalog", menuName = "CocoMania/Catalogo de Historia")]
public class StoryCatalogSO : ScriptableObject
{
    [Header("Inicio y tutorial")]
    public DialogueSO intro;
    public DialogueSO tutorialOrder;
    public DialogueSO tutorialDeuda;
    public DialogueSO tutorialUpgradeIntro;
    public DialogueSO tutorialTree;

    [Header("Capitulos por pedido")]
    [Tooltip("El indice es el ciclo del pedido (la posicion 0 queda vacia).")]
    public List<DialogueSO> chapters = new List<DialogueSO>();

    [Header("Maquina gacha")]
    public DialogueSO gachaIntro;
    public DialogueSO gachaExplain;

    static string[] Lines(DialogueSO d) => d != null ? d.GetLines() : null;

    public string[] Intro => Lines(intro);
    public string[] TutorialOrder => Lines(tutorialOrder);
    public string[] TutorialDeuda => Lines(tutorialDeuda);
    public string[] TutorialUpgradeIntro => Lines(tutorialUpgradeIntro);
    public string[] TutorialTree => Lines(tutorialTree);
    public string[] GachaIntro => Lines(gachaIntro);
    public string[] GachaExplain => Lines(gachaExplain);

    public string[] Chapter(int cycle)
    {
        return (cycle >= 0 && cycle < chapters.Count) ? Lines(chapters[cycle]) : null;
    }
}