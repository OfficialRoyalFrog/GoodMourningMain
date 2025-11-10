using UnityEngine;

public class TreeViewQuickTest : MonoBehaviour
{
    [SerializeField] private TreeView view;

    void Reset()
    {
        if (!view) view = GetComponent<TreeView>();
    }

    [ContextMenu("Show: Full")]
    void CtxShowFull() => view?.ShowFull();

    [ContextMenu("Show: Damage 1")]
    void CtxShowDmg1() => view?.ShowDamage1();

    [ContextMenu("Show: Damage 2")]
    void CtxShowDmg2() => view?.ShowDamage2();

    [ContextMenu("Show: Stump")]
    void CtxShowStump() => view?.ShowStump();

    [ContextMenu("Anim: Start Chop")]
    void CtxAnimStart() => view?.SetChopAnimating(true);

    [ContextMenu("Anim: Stop Chop")]
    void CtxAnimStop() => view?.SetChopAnimating(false);
}