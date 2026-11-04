using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GamepadFocus : MonoBehaviour
{
    [Tooltip("Opcional: boton que se selecciona primero.")]
    public Selectable first;

    void OnEnable() { StartCoroutine(Focus()); }

    IEnumerator Focus()
    {
        yield return null;
        if (!GameInput.GamepadConnected || EventSystem.current == null) yield break;

        Selectable s = (first != null && first.IsInteractable() && first.gameObject.activeInHierarchy)
            ? first
            : GetComponentsInChildren<Selectable>().FirstOrDefault(x => x.interactable && x.gameObject.activeInHierarchy);

        if (s != null) EventSystem.current.SetSelectedGameObject(s.gameObject);
    }
}