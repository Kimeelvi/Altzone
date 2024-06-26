using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Raid0_Timer : MonoBehaviour
{
    [Header("Component")]
    public TextMeshProUGUI TimerText;

    [Header("Timer settings")]
    public float CurrentTime;
    public bool CountUp;

    [Header("Limit settings")]
    public bool HasLimit;
    public float TimerLimit;

    [Header("Format settings")]
    public bool HasFormat;
    public TimerFormat Format;
    private Dictionary<TimerFormat, string> TimeFormat = new Dictionary<TimerFormat, string>();

    void Start()
    {
        TimeFormat.Add(TimerFormat.Whole, "0");
        TimeFormat.Add(TimerFormat.TenthDecimal, "0.0");
        TimeFormat.Add(TimerFormat.HundrethsDecimal, "0.00");
    }

    void Update()
    {
        CurrentTime = CountUp ? CurrentTime += Time.deltaTime : CurrentTime -= Time.deltaTime;

        if (HasLimit && ((CountUp && CurrentTime >= TimerLimit) || (!CountUp && CurrentTime <= TimerLimit)))
        {
            CurrentTime = TimerLimit;
            SetTimerText();
            TimerText.color = Color.red;
            enabled = false;
        }

        if (Raid0_Grid.MineWasHit)
        {
            SetTimerText();
            enabled = false;
        }
        SetTimerText();
    }

    private void SetTimerText()
    {
        TimerText.text = HasFormat ? CurrentTime.ToString(TimeFormat[Format]) : CurrentTime.ToString();
    }

    public enum TimerFormat
    {
        Whole, TenthDecimal, HundrethsDecimal
    }
}
