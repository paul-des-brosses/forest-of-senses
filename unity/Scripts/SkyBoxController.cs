using UnityEngine;

public class SkyboxController : MonoBehaviour
{
    [Header("--- LIENS ---")]
    public SeasonManager seasonManager;
    public Material skyboxMaterial; 
    public Light sunLight; // Glisse ta Directional Light ici

    [Header("--- NOMS DES VARIABLES (Shader) ---")]
    public string nightProp = "_NightIntensity"; 
    public string blendProp = "_Blend";

    [Header("--- RÉGLAGES : INTENSITÉ NUIT ---")]
    [Range(0, 3)] public float nightStartSeason = 0.0f; 
    [Range(0, 3)] public float nightEndSeason = 1.0f;   
    public float nightValStart = 1.5f; 
    public float nightValEnd = 1.0f;   

    [Header("--- RÉGLAGES : MÉLANGE (BLEND) & LUMIÈRE ---")]
    [Range(0, 3)] public float blendStartSeason = 1.0f; 
    [Range(0, 3)] public float blendEndSeason = 2.0f;   
    
    [Space(5)]
    [Range(0, 1)] public float blendValStart = 1.0f;    // 1 = Nuit
    [Range(0, 1)] public float blendValEnd = 0.0f;      // 0 = Jour

    [Space(5)]
    public float sunIntensityStart = 0f; // Lumière éteinte au début du blend
    public float sunIntensityEnd = 1f;   // Lumière à 1 à la fin du blend

    [Header("--- DEBUG (Lecture Seule) ---")]
    public float currentNight;
    public float currentBlend;
    public float currentSun;

    void Update()
    {
        if (seasonManager == null || skyboxMaterial == null) return;

        float progress = seasonManager.globalSeasonProgress;

        // 1. CALCUL DE L'INTENSITÉ NUIT
        float tNight = Mathf.InverseLerp(nightStartSeason, nightEndSeason, progress);
        currentNight = Mathf.Lerp(nightValStart, nightValEnd, tNight);
        skyboxMaterial.SetFloat(nightProp, currentNight);

        // 2. CALCUL DU MÉLANGE (BLEND) ET DE LA DIRECTIONAL LIGHT
        // tBlend va de 0 à 1 entre blendStartSeason et blendEndSeason
        float tBlend = Mathf.InverseLerp(blendStartSeason, blendEndSeason, progress);

        // Mise à jour de la Skybox
        currentBlend = Mathf.Lerp(blendValStart, blendValEnd, tBlend);
        skyboxMaterial.SetFloat(blendProp, currentBlend);

        // Mise à jour de la Directional Light
        if (sunLight != null)
        {
            currentSun = Mathf.Lerp(sunIntensityStart, sunIntensityEnd, tBlend);
            sunLight.intensity = currentSun;

            // Optionnel : Désactive la lumière si l'intensité est à 0 pour gagner en performance
            sunLight.enabled = (sunLight.intensity > 0.001f);
        }
    }
}