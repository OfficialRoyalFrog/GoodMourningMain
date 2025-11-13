// Assets/_Project/Scripts/UI/Utility/PlayModeVisibility.cs
using UnityEngine;

[ExecuteAlways]
public class PlayModeVisibility : MonoBehaviour
{
    [SerializeField] GameObject target;
    [SerializeField] bool hideInEditMode = true;

    void OnEnable() => Refresh();
    void Update() => Refresh();

    void Refresh()
    {
        if (!target) target = gameObject;
        bool shouldShow = Application.isPlaying || !hideInEditMode;
        if (target.activeSelf != shouldShow)
            target.SetActive(shouldShow);
    }
}
