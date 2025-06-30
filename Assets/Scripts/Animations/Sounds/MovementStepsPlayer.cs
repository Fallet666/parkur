using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MovementStepsPlayer : MonoBehaviour
{
    [SerializeField] private SurfaceResources defaultSurface;

    [SerializeField] private float minSpeedToPlaySteps = 1f;
    [SerializeField] private float stepsDelay = 0.5f;
    [SerializeField] private float runningStepsDelay = 0.3f;
    [SerializeField] private float crouchingStepsDelay = 0.6f;
    [SerializeField][Range(0f, 1f)] private float defaultVolume = 0.6f;
    [SerializeField][Range(0f, 1f)] private float runningVolume = 1f;
    [SerializeField][Range(0f, 1f)] private float chrouchingVolume = 0.2f;

    public SurfaceResources Resources {
        get => currentSurface;
        set
        {
            if (value != null)
                currentSurface = value;
            else
                currentSurface = defaultSurface;
        }
    }
    public PlayerMovementData MovementData { get; set; }

    private SurfaceResources currentSurface;
    private AudioSource source;
    private float delayedTime;
    private int stepId = 0;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        Resources = defaultSurface;
    }

    private void Update()
    {
        if(MovementData.currentSpeed > minSpeedToPlaySteps)
        {
            if(Time.time > delayedTime && MovementData.isGrounded)
            {
                if (MovementData.isCrouching)
                {
                    delayedTime = Time.time + crouchingStepsDelay;
                    source.volume = chrouchingVolume;
                }
                else if (MovementData.isRunning)
                {
                    delayedTime = Time.time + runningStepsDelay;
                    source.volume = runningVolume;
                }
                else
                {
                    delayedTime = Time.time + stepsDelay;
                    source.volume = defaultVolume;
                }
                PlayStep();
            }
        }
        else
        {
            delayedTime = Time.time;
        }
    }
    private void PlayStep()
    {
        if (Resources.steps.Length <= 0)
            return;

        stepId += 1;
        if (stepId >= Resources.steps.Length)
        {
            stepId = 0;
        }

        source.PlayOneShot(Resources.steps[stepId]);
    }
}
