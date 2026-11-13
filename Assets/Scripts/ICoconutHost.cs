using UnityEngine;
public interface ICoconutHost
{
    int Hp { get; }
    int HpMax { get; }
    Vector3 Position { get; }
    void Heal(int amount);
    void Flash(Color color, float duration);
    void SetDarken(float t);
    void SetAlpha(float a);
    void FloatText(string text, float yOffset, bool emphasized = true);
}