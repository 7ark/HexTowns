using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using TMPro;

public class GameTime : MonoBehaviour
{
    private const float TIME_CHANGE_ADJUSTMENT = 0.1f;

    [SerializeField]
    private Light directionalLight;
    [SerializeField]
    private PostProcessVolume postProcessing;
    [SerializeField]
    private AnimationCurve timeCurve;
    [SerializeField]
    private float dawnTime = 4f;
    [SerializeField]
    private float duskTime = 20f;
    [SerializeField]
    private float dayTimeChangeMultiplier;
    [SerializeField]
    private float nightTimeChangeMultiplier;
    [SerializeField]
    private TextMeshProUGUI timeDisplay;

    private float currentTime = 0;
    //private float checkTimer = 0;
    //private float dayTimer = 0;
    //private float nightTimer = 0;
    //private float timeSpeed = 1;
    private int daysPassed = 0;

    public static GameTime Instance;

    public float CurrentTime { get { return currentTime; } }
    public float Dawn { get { return dawnTime; } }
    public float Dusk { get { return duskTime; } }
    public float Sunrise { get { return dawnTime; } }
    public float Sunset { get { return duskTime - 1; } }
    //public float TimeSpeed { get { return timeSpeed; } }

    private void Awake()
    {
        Instance = this;
    }

    public bool IsItDay()
    {
        return !(currentTime < dawnTime || currentTime > duskTime);
    }

    public bool IsItLightOutside()
    {
        return !(currentTime < Sunrise || currentTime > Sunset);
    }

    public void SetTimeSpeed(float speed)
    {
        Time.timeScale = speed;
        //timeSpeed = speed;
    }

    public void SetTime(float time)
    {
        currentTime = time;
        UpdateTimeVisuals();
    }

    private void UpdateTimeVisuals()
    {
        float timeValue = timeCurve.Evaluate(currentTime / 24f);

        directionalLight.color = Color.Lerp(Color.black, Color.white, timeValue);

        if(!IsItDay())
        {
            directionalLight.transform.rotation = Quaternion.Euler(new Vector3(180f, 60, 0));
        }
        else
        {
            float timeProgressionDelta = (currentTime - dawnTime) / (duskTime - dawnTime);
            float dayTimeValue = timeCurve.Evaluate(timeProgressionDelta);
            float rotationValue = timeProgressionDelta <= 0.5f ? Mathf.Lerp(0f, 90f, dayTimeValue) : Mathf.Lerp(180f, 90f, dayTimeValue);
            directionalLight.transform.rotation = Quaternion.Euler(new Vector3(rotationValue, 60, 0));
        }

        postProcessing.profile.GetSetting<ColorGrading>().temperature.value = Mathf.Lerp(-40, 40, timeValue);
        postProcessing.profile.GetSetting<Vignette>().intensity.value = Mathf.Lerp(0.5f, 0.2f, timeValue);
        postProcessing.profile.GetSetting<AmbientOcclusion>().intensity.value = Mathf.Lerp(1.5f, 0.7f, timeValue);
    }

    private void Update()
    {
        //if(Input.GetKeyDown(KeyCode.Space))
        //{
        //    if(timeSpeed == 1)
        //    {
        //        timeSpeed = 10;
        //    }
        //    else
        //    {
        //        timeSpeed = 1;
        //    }
        //}

        //checkTimer += Time.deltaTime;
        //if(IsItDay())
        //{
        //    dayTimer += Time.deltaTime;
        //}
        //else
        //{
        //    nightTimer += Time.deltaTime;
        //}
        currentTime += Time.deltaTime * (IsItDay() ? dayTimeChangeMultiplier : nightTimeChangeMultiplier) * TIME_CHANGE_ADJUSTMENT;
        if(currentTime >= 24f)
        {
            currentTime = 0;
            daysPassed++;
            //Debug.Log("A full cycle is " + checkTimer + " seconds! With a day being " + dayTimer + " seconds, and a night being " + nightTimer + " seconds");
        }

        timeDisplay.text = "Day " + (daysPassed + 1) + " " + System.TimeSpan.FromHours(currentTime).ToString(@"hh\:mm");

        UpdateTimeVisuals();
    }
}
