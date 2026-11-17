using UnityEngine;

public static class ManagerBootstrap
{
    public static T Ensure<T>() where T : MonoBehaviour
    {
        T found = Object.FindFirstObjectByType<T>();
        if (found != null) return found;

        Debug.LogWarning($"[{typeof(T).Name}] No estaba en la escena; se creo automaticamente. " +
                         "Agregalo al objeto de managers para poder ajustar sus valores en el Inspector.");
        return new GameObject(typeof(T).Name).AddComponent<T>();
    }
}