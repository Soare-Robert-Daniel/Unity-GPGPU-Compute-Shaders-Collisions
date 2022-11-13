using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UIHandler : MonoBehaviour
{
    [SerializeField] private UIDocument root;
    private Label _cpuTime;
    private Label _gpuTime;
    private Label _fps;

    private float pollingTime = 1f;
    private float time;
    private int frameCount;

    // Update is called once per frame
    private void Start()
    {
        _fps = root.rootVisualElement.Q<Label>("fps");
        _cpuTime = root.rootVisualElement.Q<Label>("cpuTime");
        _gpuTime = root.rootVisualElement.Q<Label>("gpuTime");
    }

    private void Update()
    {
        time += Time.unscaledDeltaTime;

        frameCount++;

        if (time >= pollingTime)
        {
            _fps.text = Mathf.RoundToInt(frameCount / time).ToString();
            frameCount = 0;
            time -= pollingTime;
        }
    }

    public void UpdateCPUTime(TimeSpan elapsed)
    {
        _cpuTime.text = $"{elapsed.Hours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}.{elapsed.Milliseconds / 10:00}";
    }
    
    public void UpdateGPUTime(TimeSpan elapsed)
    {
        _gpuTime.text = $"{elapsed.Hours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}.{elapsed.Milliseconds / 10:00}";
    }
}
