using System.Collections;
using UnityEngine;

public class SC_Vefects_Particle_Systems_Auto_Restart : MonoBehaviour
{
    [Min(0.1f)]
    [SerializeField] private float restartEverySeconds = 5f;

    private ParticleSystem[] particleSystems;
    private AudioSource[] audioSources;
    private Coroutine restartCoroutine;

    private void OnEnable()
    {
        // "true" incluye hijos inactivos.
        particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        audioSources = GetComponentsInChildren<AudioSource>(true);
        restartCoroutine = StartCoroutine(RestartRoutine());
    }

    private void OnDisable()
    {
        if (restartCoroutine != null)
        {
            StopCoroutine(restartCoroutine);
            restartCoroutine = null;
        }
    }

    private IEnumerator RestartRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(restartEverySeconds);
            RestartAllParticles();
            RestartAllAudioSources();
        }
    }

    public void RestartAllParticles()
    {
        foreach (ParticleSystem particles in particleSystems)
        {
            if (particles == null)
                continue;

            particles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );

            particles.Play(true);
        }
    }

    public void RestartAllAudioSources()
    {
        foreach (AudioSource audioSource in audioSources)
        {
            if (audioSource == null || audioSource.clip == null)
                continue;

            audioSource.Stop();
            audioSource.time = 0f;
            audioSource.Play();
        }
    }
}
