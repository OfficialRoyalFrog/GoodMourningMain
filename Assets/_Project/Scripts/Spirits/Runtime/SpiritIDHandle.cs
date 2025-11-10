using UnityEngine;

/// <summary>
/// Lightweight tag component added to each spawned Spirit instance so UI/openers
/// can resolve which Spirit Id this instance represents.
/// </summary>
[DisallowMultipleComponent]
public class SpiritIdHandle : MonoBehaviour
{
    [Tooltip("Unique Spirit ID for this instance (matches SpiritSO.Id)")]
    [SerializeField] private string id;

    public string Id => id;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Editor-only: keep id trimmed; SpiritManager assigns the runtime id.
        if (!string.IsNullOrEmpty(id)) id = id.Trim();
    }
#endif
}