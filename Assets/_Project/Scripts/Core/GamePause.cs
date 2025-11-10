using System.Collections;
using UnityEngine;

public class GamePause : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    [Header("UI")]
    [SerializeField] GameObject pausePanel;      // HUD_Canvas/PausePanel
    [SerializeField] Animator sidebarAnimator;   // Sidebar's Animator (has Bool "IsOpen")

    [Header("Animation Timing")]
    [Tooltip("Length of Pause_Sidebar_Out animation in seconds.")]
    [SerializeField] float slideDuration = 0.20f; // must match your Out clip

    Coroutine closeRoutine;

    public void SetPaused(bool paused)
    {
        // If we're already in the requested state (including while closing), ignore.
        if (paused && IsPaused) return;
        if (!paused && !IsPaused && closeRoutine == null) return;

        if (paused)
        {
            // Enter pause immediately
            if (closeRoutine != null) { StopCoroutine(closeRoutine); closeRoutine = null; }
            IsPaused = true;
            Time.timeScale = 0f;

            if (pausePanel && !pausePanel.activeSelf)
                pausePanel.SetActive(true);

            if (sidebarAnimator)
            {
                sidebarAnimator.updateMode = AnimatorUpdateMode.UnscaledTime; // animate while paused
                sidebarAnimator.SetBool("IsOpen", true); // play slide IN
            }
        }
        else
        {
            // Begin resume: play slide OUT first, then hide panel & unpause
            if (!IsPaused) return; // already unpaused or in process

            if (sidebarAnimator)
                sidebarAnimator.SetBool("IsOpen", false); // play slide OUT

            if (closeRoutine != null) StopCoroutine(closeRoutine);
            closeRoutine = StartCoroutine(CloseAfterSlide());
        }
    }

    IEnumerator CloseAfterSlide()
    {
        // Wait in REAL time so animation runs with timeScale == 0
        float t = 0f;
        while (t < slideDuration)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (pausePanel && pausePanel.activeSelf)
            pausePanel.SetActive(false);

        Time.timeScale = 1f;
        IsPaused = false;
        closeRoutine = null;
    }
}