using UnityEngine;

public class PlayerHealthDebugHotkeys : MonoBehaviour
{
    public PlayerHealth health;   // ← changed from Health to PlayerHealth
    public int damageAmount = 1;
    public int healAmount = 1;

    void Update()
    {
        if (!health) return;

        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
            health.TakeDamage(damageAmount);

        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
            health.Heal(healAmount);

        if (Input.GetKeyDown(KeyCode.K))
            health.Kill();
    }
}