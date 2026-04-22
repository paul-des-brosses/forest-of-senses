using UnityEngine;

public class Phase0_Manager : MonoBehaviour
{
    [Header("--- LIENS ---")]
    public SeasonManager seasonManager;

    [Header("--- PARTICULES (5 Fontaines Blanches) ---")]
    [Tooltip("Ordre : Pouce, Index, Majeur, Annulaire, Auriculaire")]
    public ParticleSystem[] fingerFountains = new ParticleSystem[5];

    [Header("--- RÉGLAGES ---")]
    [Tooltip("Multiplicateur pour convertir la force (Newtons) en quantité de particules.")]
    public float emissionMultiplier = 20f; // Ex: 5 Newtons * 20 = 100 particules/sec
    [Tooltip("Seuil min pour allumer la fontaine")]
    public float activationThreshold = 0.5f;

    void Update()
    {
        // Sécurités
        if (seasonManager == null || fingerFountains == null || fingerFountains.Length < 5) return;

        // 1. GESTION ON/OFF SELON LA PHASE
        // Si on n'est PAS en phase 0 (donc si progress >= 0), on coupe tout et on sort.
        if (seasonManager.globalSeasonProgress >= 0.0f)
        {
            TurnOffAll();
            return;
        }

        // 2. LOGIQUE MULTI-TOUCH (PHASE 0)
        for (int i = 0; i < 5; i++)
        {
            // A. Récupération de la force brute (SANS le filtre WinnerTakesAll)
            // On va chercher directement à la source ou dans le debug array
            float rawForce = seasonManager.useDebugInput 
                ? seasonManager.debugFingerForces[i] 
                : (float)Manipulandum_data_aquired.Force_Data[i];

            // On applique le multiplicateur global du manager pour rester cohérent
            float finalForce = Mathf.Abs(rawForce) * seasonManager.forceMultiplier;

            // B. Application aux particules
            if (fingerFountains[i] != null)
            {
                var emission = fingerFountains[i].emission;

                if (finalForce > activationThreshold)
                {
                    // La quantité dépend directement de la force (Feedback analogique pur)
                    emission.rateOverTime = finalForce * emissionMultiplier;
                }
                else
                {
                    emission.rateOverTime = 0f;
                }
            }
        }
    }

    // Fonction pour tout éteindre proprement quand on passe à la Phase 1
    void TurnOffAll()
    {
        for (int i = 0; i < 5; i++)
        {
            if (fingerFountains[i] != null)
            {
                var emission = fingerFountains[i].emission;
                if (emission.rateOverTime.constant > 0f) // Optimisation : on ne set à 0 que si nécessaire
                {
                    emission.rateOverTime = 0f;
                }
            }
        }
    }
}