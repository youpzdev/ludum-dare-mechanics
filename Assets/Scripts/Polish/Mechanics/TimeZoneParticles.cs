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
        if (!Initialize())
        {
            enabled = false;
            return;
        }
        Refresh();
    }

    private bool Initialize()
    {
        if (initialized) return true;
        if (zone == null || particles == null || flow == null)
        {
            Debug.LogError($"Частицы зоны: zone={zone}, particles={particles}, flow={flow}", this);
            return false;
        }
        ParticleSystem.MainModule main = particles.main;
        if (!main.loop || main.playOnAwake || main.stopAction != ParticleSystemStopAction.None || particles.subEmitters.enabled)
        {
            Debug.LogError($"Частицы зоны: loop={main.loop}, awake={main.playOnAwake}, stop={main.stopAction}, subEmitters={particles.subEmitters.enabled}", this);
            return false;
        }
        originalSimulationSpeed = main.simulationSpeed;
        initialized = true;

        return true;
    }

    private void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (!initialized || !isActiveAndEnabled) return;
        if (zone == null || particles == null || flow == null)
        {
            Debug.LogError($"Частицы зоны: потеряны zone={zone}, particles={particles}, flow={flow}", this);
            enabled = false;
            return;
        }
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
        if (particles != null)
        {
            if (particles.isPlaying || particles.IsAlive(false)) particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (!initialized) return;
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
