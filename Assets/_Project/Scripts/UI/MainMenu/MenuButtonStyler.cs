using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class MenuButtonStyler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Refs")]
    public GameObject highlight;
    public TMP_Text label;

    [Header("Colors")]
    public Color normalTextColor = new(0.965f, 0.945f, 0.902f, 1f);
    public Color selectedTextColor = Color.white;

    [Header("Audio")]
    [Tooltip("AudioSource used to play UI sounds (e.g., 2D source on MainMenuCanvas/UIAudio)")]
    public AudioSource uiAudioSource;
    [Tooltip("Randomized flicker clips played when selection moves onto this button (e.g., flicker1..5)")]
    public AudioClip[] flickerClips;
    [Tooltip("Sound played when this button is confirmed / pressed.")]
    public AudioClip confirmClip;
    [Range(0f, 1f)] public float sfxVolume = 0.85f;

    [Header("First-Select Behavior")]
    [Tooltip("If ON, do NOT play SFX the very first time this button becomes selected after Play starts.")]
    public bool suppressFirstSelectSFX = true;
    [Tooltip("If ON, do NOT play highlight animation the very first time this button becomes active after Play starts.")]
    public bool suppressFirstSelectAnim = true;

    HighlighterAnimator anim;
    bool isHovered;
    bool wasActive;
    bool wasSelected;
    bool effectsArmed;
    EventSystem es;
    Button button; // <-- new

    void Awake()
    {
        es = EventSystem.current;
        if (highlight) anim = highlight.GetComponent<HighlighterAnimator>();
        button = GetComponent<Button>(); // capture the button component
        Apply(false, force: true);
        wasSelected = false;
        effectsArmed = false;

        // subscribe to button click event for confirm sound
        if (button != null)
            button.onClick.AddListener(PlayConfirmSFX);
    }

    void Start() => StartCoroutine(ArmEffectsNextFrame());

    System.Collections.IEnumerator ArmEffectsNextFrame()
    {
        yield return null;
        effectsArmed = true;
    }

    void Update()
    {
        if (es == null) es = EventSystem.current;

        bool isSelected = (es && es.currentSelectedGameObject == gameObject);
        bool active = isHovered || isSelected;

        Apply(active, force: false);

        if (isSelected && !wasSelected)
        {
            if (!(suppressFirstSelectSFX && !effectsArmed))
                PlayMoveSFX();
        }

        wasSelected = isSelected;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    void Apply(bool active, bool force)
    {
        if (highlight && highlight.activeSelf != active)
            highlight.SetActive(active);

        if (label)
            label.color = active ? selectedTextColor : normalTextColor;

        if ((force || (!wasActive && active)) && anim != null && highlight.activeInHierarchy)
        {
            if (!(suppressFirstSelectAnim && !effectsArmed))
            {
                anim.ResetState();
                anim.PlayRandom();
            }
        }

        wasActive = active;
    }

    void PlayMoveSFX()
    {
        if (uiAudioSource == null || flickerClips == null || flickerClips.Length == 0) return;
        int i = Random.Range(0, flickerClips.Length);
        var clip = flickerClips[i];
        if (clip != null)
            uiAudioSource.PlayOneShot(clip, sfxVolume);
    }

    void PlayConfirmSFX()
    {
        if (uiAudioSource == null || confirmClip == null) return;
        uiAudioSource.PlayOneShot(confirmClip, sfxVolume);
    }
}