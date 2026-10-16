using System;
using UnityEngine;

public class TimeZoneParticles : MonoBehaviour
{
    [SerializeField] private TimeZone zone;
    [SerializeField] private ParticleSystem particles;
    [SerializeField] private DemoFlow flow;

    private bool initialized;
    private bool focused = true;
    private bool paused;
    private float originalSimulationSpeed;

    private void OnEnable()
    {
        Initialize();
        Refresh();
    }

    private void Initialize()
    {
        if (initialized) return;
        if (zone == null || particles == null || flow == null)
        {
            throw new InvalidOperationException($"TimeZoneParticles '{name}' requires zone, particles and flow references.");
        }
        ParticleSystem.MainModule main = particles.main;
        if (!main.loop || main.playOnAwake || main.stopAction != ParticleSystemStopAction.None || particles.subEmitters.enabled)
        {
            throw new InvalidOperationException($"TimeZoneParticles '{name}' requires looping authored particles without playOnAwake, stop action or subemitters: loop={main.loop}, playOnAwake={main.playOnAwake}, stopAction={main.stopAction}, subEmitters={particles.subEmitters.enabled}.");
        }
        originalSimulationSpeed = main.simulationSpeed;
        initialized = true;
    }

    private void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        bool active = zone.isActiveAndEnabled && !flow.Finished && focused && !paused;
        if (!active)
        {
            StopParticles();
            return;
        }
        ParticleSystem.MainModule main = particles.main;
        main.simulationSpeed = originalSimulationSpeed * zone.Multiplier;
        if (!particles.isPlaying) particles.Play(false);
    }

    private void StopParticles()
    {
        if (initialized && particles != null)
        {
            if (particles.isPlaying || particles.IsAlive(false)) particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particles.main;
            main.simulationSpeed = originalSimulationSpeed;
        }
    }

    private void OnDisable()
    {
        StopParticles();
    }

    private void OnApplicationFocus(bool value)
    {
        focused = value;
        if (!value) StopParticles();
    }

    private void OnApplicationPause(bool value)
    {
        paused = value;
        if (value) StopParticles();
    }
}
