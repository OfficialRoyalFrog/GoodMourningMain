using System;
using UnityEngine;

namespace Game.Core.TimeSystem
{
    /// Central game clock.
    /// Default cadence: 1 real second = 2 in-game minutes (=> 12 real minutes per day).
    /// Uses Time.deltaTime (scaled), so it stops when Time.timeScale = 0 (your pause).
    public sealed class TimeManager : MonoBehaviour
    {
        // ===== Singleton (one instance in the scene) =====
        public static TimeManager Instance { get; private set; }

        [Header("Speed")]
        [Tooltip("Global multiplier (debug / future fast-forward). 1 = normal.")]
        [SerializeField] private float timeRate = 1f;

        [Header("Day/Night")]
        [Tooltip("Hour [0..24) considered sunrise; OnSunrise fires when current time meets/exceeds this hour (supports decimals, e.g., 2.5 = 02:30).")]
        [SerializeField, Range(0f, 23.99f)] private float sunriseHour = 6f;
        [Tooltip("Hour [0..24) considered sunset; OnSunset fires when current time meets/exceeds this hour (supports decimals, e.g., 21.5 = 21:30).")]
        [SerializeField, Range(0f, 23.99f)] private float sunsetHour = 18f;

        // ===== Events =====
        public event Action<int> OnHourChanged; // emits 0..23 at each hour boundary
        public event Action<int> OnDayStarted;  // emits dayIndex starting at 1
        public event Action OnSunrise;          // fires once after passing sunriseHour each day
        public event Action OnSunset;           // fires once after passing sunsetHour each day

        // ===== Public State (read-only) =====
        public int DayIndex { get; private set; } = 1; // day 1 on first load
        public int Hour     { get; private set; } = 6; // start morning vibe
        public int Minute   { get; private set; } = 0;

        /// 0..1 across current day
        public float NormalizedDay => minutesIntoDay / MinutesPerDay;

        /// Current time as a fractional hour (e.g., 21.5 == 21:30)
        public float CurrentHourFloat => Hour + (Minute / 60f);

        /// Public accessors for editor tools/UI
        public float SunriseHour => sunriseHour;
        public float SunsetHour  => sunsetHour;

        // ===== Internals =====
        // math: 1 real second = 2 in-game minutes
        // A full day = 24h * 60m = 1440 in-game minutes
        // 1440 / 2 = 720 real seconds = 12 real minutes per full day
        private const float MinutesPerRealSecond = 2f;
        private const float MinutesPerHour       = 60f;
        private const float MinutesPerDay        = 1440f;

        [Header("Start Time")]
        [SerializeField, Tooltip("Start hour [0..24), supports decimals e.g., 6.5 for 06:30.")]
        private float startHour = 6f;
        [SerializeField, Tooltip("Start minute [0..59]. If startHour has decimals, this is added on top.")]
        private int startMinute = 0;

        private float minutesIntoDay;
        private int   lastHourEmitted = -1;
        private bool  sunriseFiredToday, sunsetFiredToday;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // Initialize internal minute counter from start time
            float clampedStartHour = Mathf.Clamp(startHour, 0f, 23.99f);
            int   clampedStartMin  = Mathf.Clamp(startMinute, 0, 59);
            minutesIntoDay = clampedStartHour * MinutesPerHour + clampedStartMin;

            Hour   = Mathf.FloorToInt(minutesIntoDay / MinutesPerHour);
            Minute = Mathf.FloorToInt(minutesIntoDay % MinutesPerHour);

            // We want day 1 banner to show after everything enables.
            // We'll not emit OnDayStarted here; the DayBannerUI will show "Day 1" on enable.
        }

        private void Update()
        {
            Advance(Time.deltaTime * timeRate);
        }

        private void Advance(float realDeltaSeconds)
        {
            if (realDeltaSeconds <= 0f) return;

            // advance in-game minutes by real seconds * 2
            minutesIntoDay += realDeltaSeconds * MinutesPerRealSecond;

            // Loop when we pass the end of day
            if (minutesIntoDay >= MinutesPerDay)
            {
                // carry remainder into next day
                minutesIntoDay %= MinutesPerDay;

                // increment day, reset sunrise/sunset flags, emit event
                DayIndex++;
                sunriseFiredToday = sunsetFiredToday = false;
                OnDayStarted?.Invoke(DayIndex);
            }

            // compute new time
            int totalMinutes = Mathf.FloorToInt(minutesIntoDay);
            int newHour   = totalMinutes / 60;
            int newMinute = totalMinutes % 60;

            // Emit hour changes once at boundaries
            if (newHour != lastHourEmitted)
            {
                lastHourEmitted = newHour;
                Hour   = newHour;
                Minute = newMinute;
                OnHourChanged?.Invoke(Hour);
            }
            else
            {
                // No hour boundary, just update minute
                Hour   = newHour;
                Minute = newMinute;
            }

            // --- Sunrise/Sunset checks should be minute-accurate (not tied to hour changes) ---
            float currentHour = CurrentHourFloat; // Hour + Minute/60f

            if (!sunriseFiredToday && currentHour >= sunriseHour)
            {
                sunriseFiredToday = true;
                OnSunrise?.Invoke();
            }

            if (!sunsetFiredToday && currentHour >= sunsetHour)
            {
                sunsetFiredToday = true;
                OnSunset?.Invoke();
            }
        }

        // ===== Public API =====

        /// Change global rate (e.g., fast-forward). 0 = freeze this system.
        public void SetTimeRate(float rate) => timeRate = Mathf.Max(0f, rate);

        /// Teleport the clock to a specific day/time (useful for debugging).
        public void SetClock(int dayIndex, int hour, int minute)
        {
            DayIndex       = Mathf.Max(1, dayIndex);
            hour           = Mathf.Clamp(hour, 0, 23);
            minute         = Mathf.Clamp(minute, 0, 59);
            minutesIntoDay = hour * MinutesPerHour + minute;
            lastHourEmitted = -1; // force next Update to emit OnHourChanged

            float currentHour = hour + (minute / 60f);
            sunriseFiredToday = (currentHour >= sunriseHour);
            sunsetFiredToday  = (currentHour >= sunsetHour);
        }

        /// Overload: set time using a fractional hour (e.g., 21.5 == 21:30)
        public void SetClock(int dayIndex, float hourFraction)
        {
            DayIndex = Mathf.Max(1, dayIndex);
            float clamped = Mathf.Clamp(hourFraction, 0f, 23.99f);
            minutesIntoDay = clamped * MinutesPerHour;
            lastHourEmitted = -1;

            int h = Mathf.FloorToInt(minutesIntoDay / MinutesPerHour);
            int m = Mathf.FloorToInt(minutesIntoDay % MinutesPerHour);
            Hour = h; Minute = m;

            float currentHour = CurrentHourFloat;
            sunriseFiredToday = (currentHour >= sunriseHour);
            sunsetFiredToday  = (currentHour >= sunsetHour);
        }

        /// Update sunrise/sunset at runtime (e.g., for season changes or debug)
        public void SetSunriseSunset(float sunrise, float sunset)
        {
            sunriseHour = Mathf.Clamp(sunrise, 0f, 23.99f);
            sunsetHour  = Mathf.Clamp(sunset,  0f, 23.99f);

            // Re-evaluate flags immediately based on current time
            float currentHour = CurrentHourFloat;
            sunriseFiredToday = (currentHour >= sunriseHour);
            sunsetFiredToday  = (currentHour >= sunsetHour);
        }
    }
}