using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class Raid_Timer : MonoBehaviour
{
    [SerializeField, Header("Raid Inventory ref")]
    private Raid_References raid_References;

    [Header("Timer texts")]
    public TextMeshProUGUI TimerText;
    public TextMeshProUGUI StartTimerText;

    [Header("Timer graphic")]
    public Image Lungs;

    [Header("Timer settings")]
    public float CurrentTime;
    public bool CountUp;
    private bool timerActive;
    private bool started;

    [Header("Start Timer settings")]
    public float TimeUntilStart;

    [Header("Limit settings")]
    public bool HasLimit;
    public float TimerLimit;

    [Header("Format settings")]
    public bool HasFormat;
    public TimerFormat Format;
    private Dictionary<TimerFormat, string> TimeFormat = new Dictionary<TimerFormat, string>();

    public event Action TimeEnded;
    public ExitRaid exitRaid;

    public Image lungsEmpty;
    private Color startColor = Color.white;
    private Color endColor = Color.red;
    [SerializeField]
    private float duration;

    void Start()
    {
        TimeFormat.Add(TimerFormat.Whole, "0");
        TimeFormat.Add(TimerFormat.TenthDecimal, "0.0");
        TimeFormat.Add(TimerFormat.HundrethsDecimal, "0.00");

        if (exitRaid != null)
        {
            exitRaid.ExitedRaid += RaidExited;
        }
    }

    void Update()
    {
        if (timerActive)
        {
            CurrentTime = CountUp ? CurrentTime += Time.deltaTime : CurrentTime -= Time.deltaTime;
            if (CurrentTime <= 5)
            {
                float t = Mathf.PingPong(Time.time / duration, 1f);
                Color lerped = Color.Lerp(startColor, endColor, t);

                lungsEmpty.color = lerped;
                TimerText.color = lerped;
            }
            if (HasLimit && ((CountUp && CurrentTime >= TimerLimit) || (!CountUp && CurrentTime <= TimerLimit)))
            {
                OnTimeEnd();
                raid_References.RedScreen.SetActive(true);
                raid_References.EndMenu.SetActive(true);
                if (raid_References.OutOfSpace.enabled || raid_References.RaidEndedText.enabled)
                {
                    raid_References.OutOfTime.enabled = false;
                }
                else if (!raid_References.OutOfSpace.enabled && !raid_References.RaidEndedText.enabled)
                {
                    raid_References.OutOfTime.enabled = true;
                }
                CurrentTime = TimerLimit;
                SetTimerText();
                TimerText.color = Color.red;
                enabled = false;
            }
        }
        else
        {
            if (!started)
            {
                TimeUntilStart -= Time.deltaTime;
                StartTimerText.text = "Aikaa alkuun: " + TimeUntilStart.ToString("F0");
                if (TimeUntilStart <= 0)
                {
                    StartTimer();
                }
            }
        }
        
        SetTimerText();
        SetTimerGraphic();
    }
    public void StartTimer()
    {
        StartTimerText.gameObject.transform.parent.gameObject.SetActive(false);
        if (!timerActive && !started)
        {
            timerActive = true;
            started = true;
        }
            
    }
    public void FinishRaid()
    {
        timerActive = false;

    }

    private void SetTimerText()
    {
        //TimerText.text = HasFormat ? CurrentTime.ToString(TimeFormat[Format]) : CurrentTime.ToString();
        TimerText.text = CurrentTime.ToString("F0");
    }
    private void SetTimerGraphic()
    {
        Lungs.fillAmount = 1.0f - (10.0f - CurrentTime) * 0.1f;
    }
    void OnTimeEnd()
    {
        TimeEnded?.Invoke();
    }
    void RaidExited()
    {
        CurrentTime = 0;
    }

    public enum TimerFormat
    {
        Whole, TenthDecimal, HundrethsDecimal
    }
}
