using System.Collections.Generic;
using UnityEngine;

public enum Language { Spanish, English, Portuguese, French, German, Japanese }

[DefaultExecutionOrder(-1000)]
public class LocalizationManager : MonoBehaviour
{
    const string LangKey = "game_language";
    const int LangCount = 6;

    public static readonly string[] NativeNames =
    {
        "Español", "English", "Português", "Français", "Deutsch", "日本語"
    };

    private static LocalizationManager _instance;
    public static LocalizationManager Instance
    {
        get
        {
            if (_instance == null) _instance = FindObjectOfType<LocalizationManager>();
            return _instance;
        }
    }

    public static Language Current =>
        Instance != null ? Instance.currentLanguage : Language.Spanish;

    public static bool IsEnglish => Current == Language.English;

    public Language currentLanguage = Language.Spanish;

    public System.Action OnLanguageChanged;

    private Dictionary<string, string[]> table = new Dictionary<string, string[]>();

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        if (PlayerPrefs.HasKey(LangKey))
        {
            int saved = PlayerPrefs.GetInt(LangKey, (int)currentLanguage);
            if (saved >= 0 && saved < LangCount) currentLanguage = (Language)saved;
        }

        BuildTable();
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    public string Get(string key)
    {
        if (table.TryGetValue(key, out var entry))
        {
            string s = entry[(int)currentLanguage];
            if (string.IsNullOrEmpty(s)) s = entry[(int)Language.English];
            if (string.IsNullOrEmpty(s)) s = entry[(int)Language.Spanish];
            return s;
        }
        return key;
    }

    public string Get(string key, params object[] args)
    {
        string raw = Get(key);
        try { return string.Format(raw, args); }
        catch (System.FormatException) { return raw; }
    }

    public void SetLanguage(Language lang)
    {
        if (currentLanguage == lang) return;
        currentLanguage = lang;
        PlayerPrefs.SetInt(LangKey, (int)lang);
        PlayerPrefs.Save();
        OnLanguageChanged?.Invoke();
    }

    public void SetLanguageIndex(int index)
    {
        if (index >= 0 && index < LangCount) SetLanguage((Language)index);
    }
    public void SetSpanish() { SetLanguage(Language.Spanish); }
    public void SetEnglish() { SetLanguage(Language.English); }
    public void SetPortuguese() { SetLanguage(Language.Portuguese); }
    public void SetFrench() { SetLanguage(Language.French); }
    public void SetGerman() { SetLanguage(Language.German); }
    public void SetJapanese() { SetLanguage(Language.Japanese); }
    public void NextLanguage() { SetLanguage((Language)(((int)currentLanguage + 1) % LangCount)); }


    void Add(string key, string es, string en, string pt, string fr, string de, string ja)
    {
        table[key] = new[] { es, en, pt, fr, de, ja };
    }

    void Same(string key, string value)
    {
        table[key] = new[] { value, value, value, value, value, value };
    }

    void BuildTable()
    {
        // ---------- Menu principal ----------
        Add("menu_play", "Jugar", "Play", "Jogar", "Jouer", "Spielen", "プレイ");
        Add("menu_settings", "Configuracion", "Settings", "Configurações", "Paramètres", "Einstellungen", "設定");
        Add("menu_quit", "Salir", "Quit", "Sair", "Quitter", "Beenden", "終了");
        Add("menu_title", "Deudas de Coco", "Coconut Debts", "Dívidas de Coco", "Dettes de Coco", "Kokosnuss-Schulden", "ココナッツの借金");

        // ---------- Configuracion ----------
        Add("settings_title", "Configuracion", "Settings", "Configurações", "Paramètres", "Einstellungen", "設定");
        Add("settings_resolution", "Resolucion", "Resolution", "Resolução", "Résolution", "Auflösung", "解像度");
        Add("settings_fullscreen", "Pantalla completa", "Fullscreen", "Tela cheia", "Plein écran", "Vollbild", "フルスクリーン");
        Add("settings_Reiniciar", "Reiniciar progreso", "Reset progress", "Reiniciar progresso", "Réinitialiser la progression", "Fortschritt zurücksetzen", "進行状況をリセット");
        Add("settings_language", "Idioma", "Language", "Idioma", "Langue", "Sprache", "言語");
        Add("settings_back", "Volver", "Back", "Voltar", "Retour", "Zurück", "戻る");

        // ---------- Pantalla de Recaudacion (fin de energia) ----------
        Add("collection_title", "Recaudacion", "Collection", "Arrecadação", "Bilan", "Tagesbilanz", "集計");
        Add("collection_coconuts_killed", "Cocos destruidos: {0}", "Coconuts destroyed: {0}", "Cocos destruídos: {0}", "Noix de coco détruites : {0}", "Zerstörte Kokosnüsse: {0}", "壊したココナッツ：{0}");
        Add("collection_money_earned", "Dinero conseguido: ${0}", "Money earned: ${0}", "Dinheiro ganho: ${0}", "Argent gagné : ${0}", "Verdientes Geld: ${0}", "獲得金額：${0}");
        Add("collection_lechita_earned", "Agua conseguida: {0}", "Water earned: {0}", "Água obtida: {0}", "Eau récoltée : {0}", "Gewonnenes Wasser: {0}", "獲得した水：{0}");
        Same("collection_current_money", "{0}");
        Same("collection_coins", "{0}");
        Same("collection_money", "{0}");
        Add("collection_cycle", "Ciclo de deuda: {0}", "Debt cycle: {0}", "Ciclo de dívida: {0}", "Cycle de dette : {0}", "Schuldenzyklus: {0}", "借金サイクル：{0}");
        Add("collection_upgrades_button", "Mejoras", "Upgrades", "Melhorias", "Améliorations", "Verbesserungen", "アップグレード");
        Add("collection_shop_button", "Tienda", "Shop", "Loja", "Boutique", "Shop", "ショップ");
        Add("collection_debt_button", "Deuda", "Debt", "Dívida", "Dette", "Schulden", "借金");
        Add("collection_continue_button", "Continuar", "Continue", "Continuar", "Continuer", "Weiter", "続ける");
        Add("run_accuracy", "{0}% de punteria", "{0}% accuracy", "{0}% de precisão", "{0} % de précision", "{0} % Trefferquote", "命中率 {0}%");
        Add("coins_found_one", "+{0} moneda encontrada", "+{0} coin found", "+{0} moeda encontrada", "+{0} pièce trouvée", "+{0} Münze gefunden", "+{0}枚のコインを発見");
        Add("coins_found_many", "+{0} monedas encontradas", "+{0} coins found", "+{0} moedas encontradas", "+{0} pièces trouvées", "+{0} Münzen gefunden", "+{0}枚のコインを発見");
        Add("unlock_order", "Pedido {0}", "Order {0}", "Pedido {0}", "Commande {0}", "Bestellung {0}", "注文 {0}");

        // ---------- Panel de Deuda ----------
        Add("debt_title", "Deuda", "Debt", "Dívida", "Dette", "Schulden", "借金");
        Add("debt_days_left", "{0} dias para pagar", "{0} days left to pay", "{0} dias para pagar", "{0} jours pour payer", "Noch {0} Tage zum Bezahlen", "支払いまであと{0}日");
        Same("debt_amount_pending", "{0}");
        Add("debt_paid_label", "Pagada", "Paid", "Paga", "Payée", "Bezahlt", "支払い済み");
        Add("debt_pay_button", "Pagar Deuda", "Pay Debt", "Pagar dívida", "Payer la dette", "Schulden bezahlen", "借金を返す");
        Add("deuda_ready", "¡Pedido listo! Entregalo", "Order ready! Deliver it", "Pedido pronto! Entregue-o", "Commande prête ! Livre-la", "Bestellung fertig! Liefere sie aus", "注文の準備ができた！届けよう");
        Add("deuda_remaining", "Faltan {0}", "{0} to go", "Faltam {0}", "Il manque {0}", "Es fehlen noch {0}", "あと{0}");
        Add("deuda_last_day_pay", "¡ÚLTIMO DÍA! Paga o bancarrota", "LAST DAY! Pay up or go bankrupt", "ÚLTIMO DIA! Pague ou vá à falência", "DERNIER JOUR ! Paie ou fais faillite", "LETZTER TAG! Zahlen oder Pleite gehen", "最終日！払うか破産か");
        Add("deuda_last_day_short", "¡ÚLTIMO DÍA! No alcanza: bancarrota", "LAST DAY! Not enough: bankruptcy", "ÚLTIMO DIA! Não deu: falência", "DERNIER JOUR ! Pas assez : faillite", "LETZTER TAG! Nicht genug: Pleite", "最終日！足りない：破産");
        Add("deuda_last_day_collect", "¡ÚLTIMO DÍA PARA JUNTAR EL AGUA!", "LAST DAY TO COLLECT THE WATER!", "ÚLTIMO DIA PARA JUNTAR A ÁGUA!", "DERNIER JOUR POUR RÉCOLTER L'EAU !", "LETZTER TAG, UM WASSER ZU SAMMELN!", "水を集める最終日！");
        Add("days_left_one", "{0} día restante", "{0} day left", "{0} dia restante", "{0} jour restant", "{0} Tag übrig", "残り{0}日");
        Add("days_left_many", "{0} días restantes", "{0} days left", "{0} dias restantes", "{0} jours restants", "{0} Tage übrig", "残り{0}日");

        // ---------- Panel de Mejoras / Tienda ----------
        Add("upgrades_title", "Mejoras", "Upgrades", "Melhorias", "Améliorations", "Verbesserungen", "アップグレード");
        Add("shop_title", "Tienda", "Shop", "Loja", "Boutique", "Shop", "ショップ");
        Add("shop_current_machete", "Machete actual: {0}", "Current machete: {0}", "Machete atual: {0}", "Machette actuelle : {0}", "Aktuelle Machete: {0}", "現在のマチェーテ：{0}");
        Same("shop_next_machete", "{0} - {1}");
        Add("shop_max_machete", "Maximo alcanzado", "Max level reached", "Nível máximo alcançado", "Niveau maximum atteint", "Maximalstufe erreicht", "最大レベルに到達");
        Add("shop_upgrade_button", "Mejorar Machete", "Upgrade Machete", "Melhorar machete", "Améliorer la machette", "Machete verbessern", "マチェーテを強化");
        Add("stat_damage", "Daño", "Damage", "Dano", "Dégâts", "Schaden", "ダメージ");
        Add("stat_swing", "Golpe", "Swing", "Golpe", "Coup", "Schlag", "間隔");
        Add("stat_radius", "Radio", "Radius", "Raio", "Rayon", "Radius", "範囲");

        // ---------- Eleccion de Perk ----------
        Add("perk_title", "Elige una mejora", "Choose a perk", "Escolha uma melhoria", "Choisis une amélioration", "Wähle eine Verbesserung", "強化を選ぼう");

        // ---------- Entre partidas ----------
        Add("betweenruns_title", "Fin de la partida", "Run over", "Fim da partida", "Fin de la partie", "Runde vorbei", "ラン終了");
        Add("betweenruns_continue_button", "Nueva Partida", "New Run", "Nova partida", "Nouvelle partie", "Neue Runde", "新しいラン");
        Add("betweenruns_legacy_points", "{0} Puntos de Legado", "{0} Legacy Points", "{0} Pontos de Legado", "{0} points d'héritage", "{0} Vermächtnispunkte", "レガシーポイント {0}");

        // ---------- Historia ----------
        Add("story_speaker", "El Emprendedor", "The Entrepreneur", "O Empreendedor", "L'Entrepreneur", "Der Unternehmer", "起業家");

        // ---------- Mensajes de log (feedback corto en pantalla) ----------
        Same("log_money_earned", "+{0}");
        Add("log_debt_paid", "Completa el pedido", "Complete the order", "Complete o pedido", "Termine la commande", "Schließe die Bestellung ab", "注文を完了しよう");
        Add("log_new_cycle", "Nuevo ciclo. Cuenta: {0}", "New cycle. Bill: {0}", "Novo ciclo. Conta: {0}", "Nouveau cycle. Facture : {0}", "Neuer Zyklus. Rechnung: {0}", "新サイクル。請求額：{0}");
        Add("log_bankruptcy", "Bancarrota. +{0} puntos de legado.", "Bankruptcy. +{0} legacy points.", "Falência. +{0} pontos de legado.", "Faillite. +{0} points d'héritage.", "Pleite. +{0} Vermächtnispunkte.", "破産。レガシーポイント+{0}。");
        Add("log_new_machete", "Nuevo machete: {0}", "New machete: {0}", "Novo machete: {0}", "Nouvelle machette : {0}", "Neue Machete: {0}", "新しいマチェーテ：{0}");
        Add("log_upgrade_bought", "Mejora comprada: {0}", "Upgrade bought: {0}", "Melhoria comprada: {0}", "Amélioration achetée : {0}", "Verbesserung gekauft: {0}", "強化を購入：{0}");
        Add("log_perk_chosen", "Perk elegido: {0}", "Perk chosen: {0}", "Perk escolhido: {0}", "Atout choisi : {0}", "Perk gewählt: {0}", "パーク選択：{0}");
        Add("log_legacy_bought", "Legado adquirido: {0}", "Legacy item acquired: {0}", "Legado adquirido: {0}", "Héritage acquis : {0}", "Vermächtnis erworben: {0}", "レガシー獲得：{0}");
    }
}