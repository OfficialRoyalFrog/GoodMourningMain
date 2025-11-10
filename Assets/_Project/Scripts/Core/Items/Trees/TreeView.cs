using UnityEngine;

public class TreeView : MonoBehaviour
{
    [Header("Renderers (assign from prefab children)")]
    [SerializeField] private SpriteRenderer leavesRenderer; // stays static while chopping
    [SerializeField] private SpriteRenderer trunkRenderer;  // swaps for damage/anim frames

    [Header("Sprites (assign sliced assets)")]
    [SerializeField] private Sprite leavesFull;
    [SerializeField] private Sprite trunkFull;
    [SerializeField] private Sprite trunkDmg1;
    [SerializeField] private Sprite trunkDmg2;
    [SerializeField] private Sprite trunkStump;

    [Header("Chop Animation")]
    [SerializeField, Min(0.05f)] private float swapSeconds = 1.0f; // 1 Hz by default
    [SerializeField] private bool animateOnStart = false;


    float nextSwapAt;
    int animIndex;      // 0 = full, 1 = dmg1, 2 = dmg2
    bool animating;
    Quaternion trunkOriginalRot;

    void Awake()
    {
        // initial pose = full tree
        if (leavesRenderer) { leavesRenderer.enabled = true; leavesRenderer.sprite = leavesFull; }
        if (trunkRenderer)  trunkRenderer.sprite = trunkFull;

        if (trunkRenderer) trunkOriginalRot = trunkRenderer.transform.localRotation;

        if (animateOnStart) SetChopAnimating(true);
    }

    void Update()
    {
        if (!animating) return;
        if (Time.time < nextSwapAt) return;

        animIndex = (animIndex + 1) % 3; // 0 -> 1 -> 2 -> 0 ...
        ApplyAnimFrame(animIndex);
        nextSwapAt = Time.time + swapSeconds;
    }

    void ApplyAnimFrame(int idx)
    {
        if (!trunkRenderer) return;
        switch (idx)
        {
            case 0: trunkRenderer.sprite = trunkFull; break;
            case 1: trunkRenderer.sprite = trunkDmg1; break;
            case 2: trunkRenderer.sprite = trunkDmg2; break;
            default: trunkRenderer.sprite = trunkFull; break;
        }
    }

    // ---- Public API (ResourceNode_Tree will call these in 4C) ----
    public void ShowFull()
    {
        if (leavesRenderer) { leavesRenderer.enabled = true; leavesRenderer.sprite = leavesFull; }
        if (trunkRenderer)
        {
            trunkRenderer.sprite = trunkFull;
            trunkRenderer.transform.localRotation = trunkOriginalRot;
        }
        animIndex = 0;
    }

    public void ShowDamage1()
    {
        if (trunkRenderer)
        {
            trunkRenderer.sprite = trunkDmg1;
            trunkRenderer.transform.localRotation = trunkOriginalRot;
        }
        animIndex = 1;
    }

    public void ShowDamage2()
    {
        if (trunkRenderer)
        {
            trunkRenderer.sprite = trunkDmg2;
            trunkRenderer.transform.localRotation = trunkOriginalRot;
        }
        animIndex = 2;
    }

    public void ShowStump()
    {
        // on break: leaves disappear; stump trunk remains
        if (leavesRenderer) leavesRenderer.enabled = false;
        if (trunkRenderer)
        {
            trunkRenderer.sprite = trunkStump;
        }
        animating = false;
    }

    public void SetChopAnimating(bool on)
    {
        animating = on;
        if (animating)
            nextSwapAt = Time.time + swapSeconds;
    }

    public void SetSwapSeconds(float seconds)
    {
        swapSeconds = Mathf.Max(0.05f, seconds);
    }

    // Helpers to wire in prefab quickly
    public void SetRenderers(SpriteRenderer leaves, SpriteRenderer trunk)
    {
        leavesRenderer = leaves;
        trunkRenderer  = trunk;
    }

    public void SetSprites(Sprite leaves, Sprite full, Sprite dmg1, Sprite dmg2, Sprite stump)
    {
        leavesFull = leaves;
        trunkFull  = full;
        trunkDmg1  = dmg1;
        trunkDmg2  = dmg2;
        trunkStump = stump;
    }
}