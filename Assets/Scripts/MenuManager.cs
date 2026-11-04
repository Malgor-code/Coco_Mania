using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private GameObject panelMenu;
    [SerializeField] private GameObject panelCargando;
    [SerializeField] private GameObject panelCreditos;

    [Header("Opcional")]
    [SerializeField] private Slider barraProgreso;  
    [SerializeField] private float duracionCarga = 2f; 

    private void Start()
    {
        panelMenu.SetActive(true);
        panelCargando.SetActive(false);
        panelCreditos.SetActive(false);
    }

    public void IrACreditos()
    {
        StartCoroutine(CargarYMostrarCreditos());
    }

    private IEnumerator CargarYMostrarCreditos()
    {
        panelMenu.SetActive(false);
        panelCargando.SetActive(true);

        float t = 0f;
        while (t < duracionCarga)
        {
            t += Time.unscaledDeltaTime;
            if (barraProgreso != null)
                barraProgreso.value = t / duracionCarga;
            yield return null;
        }

        panelCargando.SetActive(false);
        panelCreditos.SetActive(true);
    }

    public void VolverAlMenu()
    {
        panelCreditos.SetActive(false);
        panelMenu.SetActive(true);
    }
}