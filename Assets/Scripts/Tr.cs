
public static class Tr
{
    public static Language Lang => LocalizationManager.Current;
    public static int Index => (int)Lang;
    public static bool EN => Lang == Language.English;

    public static string T(string es, string en)
    {
        return Lang == Language.Spanish ? es : en;
    }

    public static string T(string es, string en, string pt, string fr, string de, string ja)
    {
        switch (Lang)
        {
            case Language.English: return en;
            case Language.Portuguese: return pt;
            case Language.French: return fr;
            case Language.German: return de;
            case Language.Japanese: return ja;
            default: return es;
        }
    }
}