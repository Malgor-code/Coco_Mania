using System.Collections.Generic;
using UnityEngine;

public enum Language { Spanish, English }

[DefaultExecutionOrder(-1000)]
public class LocalizationManager : MonoBehaviour
{
    private static LocalizationManager _instance;
    public static LocalizationManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<LocalizationManager>();
            }
            return _instance;
        }
        private set { _instance = value; }
    }

    public Language currentLanguage = Language.Spanish;

    public System.Action OnLanguageChanged;

    private Dictionary<string, (string es, string en)> table = new Dictionary<string, (string, string)>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        BuildTable();
    }

    public string Get(string key)
    {
        if (table.TryGetValue(key, out var entry))
        {
            return currentLanguage == Language.Spanish ? entry.es : entry.en;
        }
        return key;
    }

    public string Get(string key, params object[] args)
    {
        string raw = Get(key);
        return string.Format(raw, args);
    }

    public void SetLanguage(Language lang)
    {
        currentLanguage = lang;
        OnLanguageChanged?.Invoke();
    }

    void Add(string key, string es, string en)
    {
        table[key] = (es, en);
    }

    void BuildTable()
    {
        // ---------- Menu principal ----------
        Add("menu_play", "Jugar", "Play");
        Add("menu_settings", "Configuracion", "Settings");
        Add("menu_quit", "Salir", "Quit");
        Add("menu_title", "Deudas de Coco", "Coconut Debts");

        // ---------- Configuracion ----------
        Add("settings_title", "Configuracion", "Settings");
        Add("settings_resolution", "Resolucion", "Resolution");
        Add("settings_fullscreen", "Pantalla completa", "Fullscreen");
        Add("settings_language", "Idioma", "Language");
        Add("settings_back", "Volver", "Back");

        // ---------- Pantalla de Recaudacion (fin de energia) ----------
        Add("collection_title", "Recaudacion", "Collection");
        Add("collection_coconuts_killed", "Cocos destruidos: {0}", "Coconuts destroyed: {0}");
        Add("collection_money_earned", "Dinero conseguido: ${0}", "Money earned: ${0}");
        Add("collection_current_money", "Dinero actual: ${0}", "Current money: ${0}");
        Add("collection_cycle", "Ciclo de deuda: {0}", "Debt cycle: {0}");
        Add("collection_upgrades_button", "Mejoras", "Upgrades");
        Add("collection_shop_button", "Tienda", "Shop");
        Add("collection_debt_button", "Deuda", "Debt");
        Add("collection_continue_button", "Continuar", "Continue");
        Add("collection_money", "${0}", "${0}");
        // ---------- Panel de Deuda ----------
        Add("debt_title", "Deuda", "Debt");
        Add("debt_days_left", "{0} dias para pagar", "{0} days left to pay");
        Add("debt_amount_pending", "${0}", "${0}");
        Add("debt_paid_label", "Pagada", "Paid");
        Add("debt_pay_button", "Pagar Deuda", "Pay Debt");

        // ---------- Panel de Mejoras / Tienda ----------
        Add("upgrades_title", "Mejoras", "Upgrades");
        Add("shop_title", "Tienda", "Shop");
        Add("shop_current_machete", "Machete actual: {0}", "Current machete: {0}");
        Add("shop_next_machete", "{0} - ${1}", "{0} - ${1}");
        Add("shop_max_machete", "Maximo alcanzado", "Max level reached");
        Add("shop_upgrade_button", "Mejorar Machete", "Upgrade Machete");

        // ---------- Eleccion de Perk ----------
        Add("perk_title", "Elige una mejora", "Choose a perk");

        // ---------- Entre partidas ----------
        Add("betweenruns_title", "Fin de la partida", "Run over");
        Add("betweenruns_continue_button", "Nueva Partida", "New Run");
        Add("betweenruns_legacy_points", "{0} Puntos de Legado", "{0} Legacy Points");

        // ---------- Mensajes de log (feedback corto en pantalla) ----------
        Add("log_money_earned", "+${0}", "+${0}");
        Add("log_debt_paid", "Completa el pedido", "Complete the order");
        Add("log_new_cycle", "Nuevo ciclo. Cuenta: ${0}", "New cycle. Bill: ${0}");
        Add("log_bankruptcy", "Bancarrota. +{0} puntos de legado.", "Bankruptcy. +{0} legacy points.");
        Add("log_new_machete", "Nuevo machete: {0}", "New machete: {0}");
        Add("log_upgrade_bought", "Mejora comprada: {0}", "Upgrade bought: {0}");
        Add("log_perk_chosen", "Perk elegido: {0}", "Perk chosen: {0}");
        Add("log_legacy_bought", "Legado adquirido: {0}", "Legacy item acquired: {0}");
    }
}