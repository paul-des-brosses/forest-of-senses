using UnityEngine;

public class CampfireController : MonoBehaviour
{
    [Header("--- LIENS ---")]
    public SeasonManager seasonManager;
    public ParticleSystem fireParticles;
    public Light fireLight;
    public AudioSource fireAudio;

    [Header("--- TIMING (Quand le feu grandit-il ?) ---")]
    [Tooltip("À quelle saison le feu commence à s'allumer ?")]
    [Range(0f, 3f)] public float growthStartSeason = 0.0f;
    
    [Tooltip("À quelle saison le feu est à sa taille maximale ?")]
    [Range(0f, 3f)] public float growthEndSeason = 1.0f;

    [Header("--- RÉGLAGES TAILLE PARTICULES ---")]
    public float startSizeMin = 0f; 
    public float startSizeMax = 2.0f;

    [Header("--- RÉGLAGES LUMIÈRE ---")]
    public float lightIntensityMin = 0f; // Mets 0 ici pour le noir total
    public float lightIntensityMax = 3.0f;

    [Header("--- RÉGLAGES SON ---")]
    public float volumeMin = 0f;
    public float volumeMax = 1.0f;

    void Update()
    {
        if (seasonManager == null) return;

        float progress = seasonManager.globalSeasonProgress;

        // C'est ici que la magie opère :
        // InverseLerp transforme la valeur de saison (ex: 0.5) en un pourcentage (0 à 1)
        // basé sur tes bornes Start et End.
        float fireIntensity = Mathf.InverseLerp(growthStartSeason, growthEndSeason, progress);

        UpdateParticles(fireIntensity);
        UpdateLight(fireIntensity);
        UpdateSound(fireIntensity);
    }

    void UpdateParticles(float intensity)
    {
        if (fireParticles != null)
        {
            var main = fireParticles.main;
            main.startSize = Mathf.Lerp(startSizeMin, startSizeMax, intensity);
            
            // Optimisation : Si la taille est minuscule, on coupe l'émission
            if (intensity <= 0.01f && fireParticles.isPlaying) fireParticles.Stop();
            else if (intensity > 0.01f && !fireParticles.isPlaying) fireParticles.Play();
        }
    }

    void UpdateLight(float intensity)
    {
        if (fireLight != null)
        {
            // Lerp permet de passer de Min à Max. 
            // Si intensity = 0, le résultat sera EXACTEMENT lightIntensityMin.
            fireLight.intensity = Mathf.Lerp(lightIntensityMin, lightIntensityMax, intensity);
            
            // Si tu veux être sûr que la lumière est éteinte à 0 :
            fireLight.enabled = (fireLight.intensity > 0.01f);
        }
    }

    void UpdateSound(float intensity)
    {
        if (fireAudio != null)
        {
            fireAudio.volume = Mathf.Lerp(volumeMin, volumeMax, intensity);
            
            if (intensity <= 0.01f && fireAudio.isPlaying) fireAudio.Stop();
            else if (intensity > 0.01f && !fireAudio.isPlaying) fireAudio.Play();
        }
    }
}