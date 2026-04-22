using UnityEngine;

public class Particule_Controller : MonoBehaviour
{
    [Header("--- LIENS GLOBAUX ---")]
    public SeasonManager seasonManager;

    [Header("--- 🌑 PHASE 0 : INTRO ---")]
    public ParticleSystem[] p0_FingerParticles; 

    [Header("--- ❄️ PHASE 1 : HIVER ---")]
    public ParticleSystem p1_FountainParticles; 
    public WanderingMovement wanderingScript;
    public float p1_MaxEmissionRate = 40f; 
    public float p1_MinStartSpeed = 1.0f; 
    public float p1_MaxStartSpeed = 3.0f;
    public float p1_MinLifetime = 0.5f; 
    public float p1_MaxLifetime = 1.5f;
    public float wanderSpeedMin = 0.5f;
    public float wanderSpeedMax = 3.0f;
    private ParticleSystem.MainModule p1_Main;
    private ParticleSystem.EmissionModule p1_Emission;

    [Header("--- 🌸 PHASE 2 : PRINTEMPS ---")]
    public ParticleSystem[] p2_Layers; 
    public float p2_MaxEmission = 30f;
    private ParticleSystem.EmissionModule[] p2_Emissions;

    [Header("--- ☀️ PHASE 3 : ÉTÉ ---")]
    public ParticleSystem p3_Ring;
    public ParticleSystem p3_Burst;
    
    [Header("Couleurs & Vitesse")]
    public Color p3_ColorCold = Color.grey;
    public Color p3_ColorHot = new Color(1f, 0.7f, 0.1f); // Or (Parfait)
    [Tooltip("Couleur quand on appuie trop fort (Surcharge)")]
    public Color p3_ColorOverload = Color.red; // Rouge (Trop Fort)
    
    public float p3_RotationSpeedMin = 1.0f;
    public float p3_RotationSpeedMax = 5.0f;

    [Header("Densité & Tremblement")]
    public float p3_RingEmissionMin = 50f; 
    public float p3_RingEmissionMax = 200f; 
    public float p3_BurstMultiplier = 1000f;
    
    [Tooltip("AMPLITUDE : Distance maximale physique du tremblement")]
    public float p3_MaxShakeAmplitude = 0.2f;

    [Tooltip("COEFFICIENT : Sensibilité à l'erreur. Augmentez pour que ça tremble fort plus rapidement dès qu'on dépasse la cible.")]
    public float p3_ShakeSensitivity = 1.0f;

    private ParticleSystem.MainModule p3_RingMain;
    private ParticleSystem.EmissionModule p3_RingEmission;
    private ParticleSystem.EmissionModule p3_BurstEmission;
    private Vector3 p3_RingInitialPos;

    void Start()
    {
        if(p1_FountainParticles) {
            p1_Main = p1_FountainParticles.main;
            p1_Emission = p1_FountainParticles.emission;
        }
        if(p2_Layers != null && p2_Layers.Length == 3) {
            p2_Emissions = new ParticleSystem.EmissionModule[3];
            for(int i=0; i<3; i++) if(p2_Layers[i] != null) p2_Emissions[i] = p2_Layers[i].emission;
        }
        if(p3_Ring) {
            p3_RingMain = p3_Ring.main;
            p3_RingEmission = p3_Ring.emission;
            p3_RingInitialPos = p3_Ring.transform.localPosition;
        }
        if(p3_Burst) {
            p3_BurstEmission = p3_Burst.emission;
        }
    }

    void Update()
    {
        if (seasonManager == null) return;
        float progress = seasonManager.globalSeasonProgress;

        if (progress < 0.0f)
        {
            UpdatePhase0();
            StopPhase1(); StopPhase2(); StopPhase3();
        }
        else if (progress < 1.0f) 
        {
            StopPhase0(); 
            UpdatePhase1(progress);
            StopPhase2(); StopPhase3();
        }
        else if (progress < 2.0f) 
        {
            StopPhase0(); StopPhase1();
            UpdatePhase2(progress);
            StopPhase3();
        }
        else 
        {
            StopPhase0(); StopPhase1(); StopPhase2();
            UpdatePhase3(progress);
        }
    }

    void UpdatePhase0()
    {
        if (p0_FingerParticles == null || p0_FingerParticles.Length < 5) return;

        for (int i = 0; i < 5; i++)
        {
            if (p0_FingerParticles[i] == null) continue;
            
            float force = seasonManager.useDebugInput 
                ? seasonManager.debugFingerForces[i] 
                : (float)Manipulandum_data_aquired.Force_Data[i];
            
            force = Mathf.Abs(force) * seasonManager.forceMultiplier;
            var emission = p0_FingerParticles[i].emission;
            
            if (force > 0.5f) emission.rateOverTime = force * 10f;
            else emission.rateOverTime = 0f;
        }
    }

    void StopPhase0()
    {
        if (p0_FingerParticles == null) return;
        for (int i = 0; i < p0_FingerParticles.Length; i++)
        {
            if(p0_FingerParticles[i] != null) 
            {
                var emission = p0_FingerParticles[i].emission;
                emission.rateOverTime = 0f;
            }
        }
    }
    
    void UpdatePhase1(float progress)
    {
        if(p1_FountainParticles == null) return;
        float clampProgress = Mathf.Clamp01(progress);
        
        float maxForce = 0f;
        int winnerIndex = 0;
        if (seasonManager.currentRawForces != null) {
            for(int i=0; i<5; i++) {
                if(seasonManager.currentRawForces[i] > maxForce) {
                    maxForce = seasonManager.currentRawForces[i];
                    winnerIndex = i;
                }
            }
        }
        
        float health = seasonManager.GetFingerHealthForVisuals(winnerIndex);
        float normalizedForce = Mathf.Clamp01(maxForce / 5.0f);

        if (wanderingScript != null) wanderingScript.speed = Mathf.Lerp(wanderSpeedMin, wanderSpeedMax, clampProgress);
        p1_Main.startLifetime = Mathf.Lerp(p1_MinLifetime, p1_MaxLifetime, clampProgress);
        p1_Main.startSpeed = Mathf.Lerp(p1_MinStartSpeed, p1_MaxStartSpeed, normalizedForce);

        float emission = normalizedForce * p1_MaxEmissionRate;
        float seasonFactor = Mathf.Lerp(0.5f, 1.5f, clampProgress);
        emission *= seasonFactor * health; 

        p1_Emission.rateOverTime = emission;
    }
    
    void StopPhase1() { if(p1_FountainParticles) p1_Emission.rateOverTime = 0f; }

    void UpdatePhase2(float progress)
    {
        if(p2_Layers == null || p2_Layers.Length < 3) return;

        float maxForce = 0f;
        int winnerIndex = -1;
        for(int i=0; i<5; i++) {
            if(seasonManager.currentRawForces[i] > maxForce) {
                maxForce = seasonManager.currentRawForces[i];
                winnerIndex = i;
            }
        }

        if (winnerIndex == -1 || maxForce < 0.1f) {
            for(int i=0; i<3; i++) p2_Emissions[i].rateOverTime = 0f;
            return;
        }

        float globalFingerHealth = seasonManager.GetFingerHealthForVisuals(winnerIndex);
        
        int activeLayersCount = 0;
        if (maxForce > 8.0f) activeLayersCount = 3;
        else if (maxForce > 4.0f) activeLayersCount = 2;
        else if (maxForce > 0.5f) activeLayersCount = 1;

        for (int layerIndex = 0; layerIndex < 3; layerIndex++)
        {
            float targetEmission = 0f;
            if (layerIndex < activeLayersCount) 
            {
                targetEmission = p2_MaxEmission * globalFingerHealth;
            }
            p2_Emissions[layerIndex].rateOverTime = targetEmission;
        }
    }
    
    void StopPhase2() {
        if(p2_Layers == null) return;
        for(int i=0; i<3; i++) if(p2_Layers[i] != null) p2_Emissions[i].rateOverTime = 0f;
    }

    void UpdatePhase3(float progress)
    {
        if(p3_Ring == null) return;

        float maxForce = 0f;
        int winnerIndex = -1;
        
        if (seasonManager.currentRawForces != null) {
            for(int i=0; i<5; i++) {
                if(seasonManager.currentRawForces[i] > maxForce) {
                    maxForce = seasonManager.currentRawForces[i];
                    winnerIndex = i;
                }
            }
        }

        if (maxForce < 0.1f || winnerIndex == -1)
        {
            p3_RingEmission.rateOverTime = 0f;
            if(p3_Burst) p3_BurstEmission.rateOverTime = 0f;
            p3_Ring.transform.localPosition = p3_RingInitialPos; 
            return;
        }

        float targetForce = seasonManager.audioManager.GetTargetForceForFinger(winnerIndex);
        float accuracy = seasonManager.currentAccuracyP3;

        if (maxForce <= targetForce)
        {
            // PAS ASSEZ FORT ou PARFAIT
            float ringDensity = Mathf.Lerp(p3_RingEmissionMin, p3_RingEmissionMax, accuracy);
            p3_RingEmission.rateOverTime = ringDensity;

            Color targetColor = Color.Lerp(p3_ColorCold, p3_ColorHot, accuracy);
            targetColor.a = 1.0f;
            p3_RingMain.startColor = targetColor;

            float targetSpeed = Mathf.Lerp(p3_RotationSpeedMin, p3_RotationSpeedMax, accuracy);
            p3_RingMain.simulationSpeed = targetSpeed;

            p3_Ring.transform.localPosition = p3_RingInitialPos;

            if (p3_Burst) {
                if (accuracy > 0.8f) p3_BurstEmission.rateOverTime = (accuracy - 0.8f) * p3_BurstMultiplier; 
                else p3_BurstEmission.rateOverTime = 0f;
            }
        }
        else
        {
            // TROP FORT (Surcharge)
            p3_RingEmission.rateOverTime = p3_RingEmissionMax;
            p3_RingMain.simulationSpeed = p3_RotationSpeedMax;

            float excessForce = maxForce - targetForce;
            
            // Le facteur d'erreur (de 0 à 1) sert à la fois pour le tremblement et le changement de couleur
            float errorFactor = Mathf.Clamp01((excessForce * p3_ShakeSensitivity) / 5.0f); 

            // Transition de couleur : Plus on dépasse, plus ça vire au Rouge (p3_ColorOverload)
            Color overloadColor = Color.Lerp(p3_ColorHot, p3_ColorOverload, errorFactor);
            overloadColor.a = 1.0f;
            p3_RingMain.startColor = overloadColor;

            // Tremblement
            Vector3 shakeOffset = UnityEngine.Random.insideUnitSphere * errorFactor * p3_MaxShakeAmplitude;
            shakeOffset.z = 0f; 
            
            p3_Ring.transform.localPosition = p3_RingInitialPos + shakeOffset;

            // Coupure des étincelles de réussite
            if (p3_Burst) p3_BurstEmission.rateOverTime = 0f;
        }
    }

    void StopPhase3()
    {
        if(p3_Ring) {
            p3_RingEmission.rateOverTime = 0f;
            p3_Ring.transform.localPosition = p3_RingInitialPos;
        }
        if(p3_Burst) p3_BurstEmission.rateOverTime = 0f;
    }
}