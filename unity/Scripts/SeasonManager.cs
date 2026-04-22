using UnityEngine;

public class SeasonManager : MonoBehaviour
{
    // --- SINGLETON ---
    public static SeasonManager Instance;
    void Awake() 
    { 
        if (Instance == null) Instance = this; else Destroy(gameObject); 
        InitializeActions(); 
        
        // FORCER LE DÉBUT EN PHASE 0
        globalSeasonProgress = -1.0f; 
    }

    [Header("--- 1. MAÎTRE DU TEMPS ---")]
    [Tooltip("-1=Phase 0 (Intro), 0=Hiver, 1=Dégel, 2=Printemps, 3=Été")]
    [Range(-1f, 3f)] public float globalSeasonProgress = -1.0f; // Commence à -1

    [Header("--- PHASE 0 (INTRO) ---")]
    [Tooltip("Durée en secondes de la phase 0 (temps actif uniquement)")]
    public float phase0Duration = 60f;

    [Header("--- 2. INPUT & DEBUG ---")]
    public bool useDebugInput = false;
    [Range(0f, 10f)] public float[] debugFingerForces = new float[5];
    public float forceMultiplier = 1.2f;

    [Header("--- 3. VITESSE PROGRESSION ---")]
    public float phase1FillSpeed = 0.5f; 
    public float phase2FillSpeed = 0.3f; 
    public float phase3FillSpeed = 0.1f;

    [Header("--- 4. ENDURANCE (FATIGUE) ---")]
    [Header("Phase 1 - Hiver")]
    public bool enablePhase1Fatigue = true;
    [Tooltip("Temps avant que le doigt commence à fatiguer en Phase 1")]
    public float p1_gracePeriod = 2.0f; 
    [Tooltip("Vitesse de perte de santé en Phase 1")]
    public float p1_decaySpeed = 0.5f; 

    [Space(10)]
    [Header("Phase 2 - Printemps")]
    public bool enablePhase2Fatigue = true;
    [Tooltip("Temps avant que le doigt commence à fatiguer en Phase 2")]
    public float p2_gracePeriod = 2.0f; 
    [Tooltip("Vitesse de perte de santé en Phase 2")]
    public float p2_decaySpeed = 0.5f; 
    
    [Header("--- 5. RÉCUPÉRATION (RECOVERY) ---")]
    public float passiveRechargeSpeed = 1.0f;
    public float activeRechargeBonus = 1.5f;
    public AnimationCurve rechargeCurve = new AnimationCurve(new Keyframe(0, 0.2f), new Keyframe(1, 2.0f));

    [Header("--- OPTIONS DE GAMEPLAY ---")]
    public bool splitActiveRecovery = false;

    [Header("--- 6. AUDIO FEEDBACK ---")]
    [Range(0f, 1f)] public float minVolumeFloor = 0.15f;
    public bool enableDirtySound = true;
    [Range(0f, 0.5f)] public float maxPitchJitter = 0.05f;

    [Header("--- 7. PHASE 3 (DATA) ---")]
    public AnimationCurve precisionCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(0.2f, 1), new Keyframe(0.5f, 0), new Keyframe(1, 0));
    public float[] phase3FingerProgress = new float[5]; 

    [Header("--- LIENS ---")]
    public WinterAudioManager audioManager; 

    // --- VARIABLES INTERNES ---
    public class ActionState {
        public float health = 1.0f; 
        public float holdTimer = 0f; 
    }
    public ActionState[,] matrixStates = new ActionState[5, 3]; 
    public float[] phase1States = new float[5]; 
    private float[] phase1Timers = new float[5];

    public float[] currentRawForces = new float[5]; 
    public float currentAccuracyP3 = 0f; 

    void InitializeActions() {
        for(int f=0; f<5; f++) {
            phase1States[f] = 1f;
            phase3FingerProgress[f] = 0f; // SÉCURITÉ : On remet la phase 3 à zéro au lancement
            for(int l=0; l<3; l++) matrixStates[f, l] = new ActionState();
        }
    }

    void Update() {
        ReadInputs();

        // --- SÉLECTEUR DE PHASE ---
        if (globalSeasonProgress < 0.0f) 
        {
            UpdatePhase0Logic(); 
        }
        else if (globalSeasonProgress < 1.0f) 
        {
            UpdatePhase1Logic(); 
        }
        else if (globalSeasonProgress < 2.0f) 
        {
            UpdatePhase2Logic(); 
        }
        else 
        {
            UpdatePhase3Logic(); 
        }

        if (globalSeasonProgress > 3.0f) globalSeasonProgress = 3.0f;
        
        if (audioManager != null) audioManager.UpdateFromManager(currentRawForces);
    }

    void ReadInputs() {
        float[] tempForces = new float[5];
        for(int i=0; i<5; i++) {
            float raw = useDebugInput ? debugFingerForces[i] : (float)Manipulandum_data_aquired.Force_Data[i];
            tempForces[i] = Mathf.Abs(raw) * forceMultiplier;
        }

        int winnerIndex = -1; float maxVal = 0f; float secondMaxVal = 0f;
        for(int i=0; i<5; i++) if(tempForces[i] > maxVal) { maxVal = tempForces[i]; winnerIndex = i; }
        for(int i=0; i<5; i++) if(i != winnerIndex && tempForces[i] > secondMaxVal) secondMaxVal = tempForces[i];

        for(int i=0; i<5; i++) currentRawForces[i] = 0f;
        if (winnerIndex != -1) currentRawForces[winnerIndex] = Mathf.Max(0f, maxVal - secondMaxVal);
    }

    // --- PHASE 0 : INTRO ---
    void UpdatePhase0Logic()
    {
        bool isAnyFingerActive = false;
        
        for(int i=0; i<5; i++) {
            float raw = useDebugInput ? debugFingerForces[i] : (float)Manipulandum_data_aquired.Force_Data[i];
            if (Mathf.Abs(raw) * forceMultiplier > 0.5f) {
                isAnyFingerActive = true;
                break;
            }
        }

        if (isAnyFingerActive)
        {
            globalSeasonProgress += (Time.deltaTime / phase0Duration);
        }

        for(int f=0; f<5; f++) phase1States[f] = 1f;
    }

    // --- PHASE 1 ---
    void UpdatePhase1Logic() {
        float totalProgress = 0f;
        float totalActiveEnergyGenerated = 0f;
        int restingFingersCount = 0;

        for(int i=0; i<5; i++) {
            float normalizedForce = Mathf.Clamp01(currentRawForces[i] / 5.0f);
            if (normalizedForce > 0.1f) totalActiveEnergyGenerated += (activeRechargeBonus * phase1States[i]);
            else restingFingersCount++;
        }

        for(int i=0; i<5; i++) {
            float normalizedForce = Mathf.Clamp01(currentRawForces[i] / 5.0f);
            bool isActive = normalizedForce > 0.1f;
            
            if (isActive) {
                if (enablePhase1Fatigue) {
                    phase1Timers[i] += Time.deltaTime;
                    if (phase1Timers[i] > p1_gracePeriod) phase1States[i] -= p1_decaySpeed * Time.deltaTime; 
                } else phase1States[i] = 1.0f;
                totalProgress += phase1FillSpeed * normalizedForce * phase1States[i] * Time.deltaTime;
            } 
            else {
                float passive = rechargeCurve.Evaluate(phase1States[i]) * passiveRechargeSpeed;
                float activeBonus = 0f;
                if (totalActiveEnergyGenerated > 0f) activeBonus = (splitActiveRecovery && restingFingersCount > 0) ? totalActiveEnergyGenerated / restingFingersCount : totalActiveEnergyGenerated;
                phase1States[i] += (passive + activeBonus) * Time.deltaTime;
                phase1Timers[i] = 0f;
            }
            phase1States[i] = Mathf.Clamp01(phase1States[i]);
        }
        globalSeasonProgress += totalProgress;
    }

    // --- PHASE 2 ---
    void UpdatePhase2Logic() {
        bool[,] activeMatrix = new bool[5, 3];
        float totalActiveEnergyGenerated = 0f;
        int restingFingersCount = 0; 

        for(int f=0; f<5; f++) {
            int level = GetPressureLevel(currentRawForces[f]);
            if (level > 0) {
                float fingerHealthSum = 0f; int activeLayers = 0;
                if (level >= 1) { activeMatrix[f, 0] = true; fingerHealthSum += matrixStates[f, 0].health; activeLayers++; }
                if (level >= 2) { activeMatrix[f, 1] = true; fingerHealthSum += matrixStates[f, 1].health; activeLayers++; }
                if (level >= 3) { activeMatrix[f, 2] = true; fingerHealthSum += matrixStates[f, 2].health; activeLayers++; }
                totalActiveEnergyGenerated += (activeRechargeBonus * ((activeLayers > 0) ? fingerHealthSum / activeLayers : 0f));
            } else restingFingersCount++;
        }

        float progressTick = 0f;
        for(int f=0; f<5; f++) {
            bool isFingerActive = (activeMatrix[f,0] || activeMatrix[f,1] || activeMatrix[f,2]);
            float activeBonusToApply = (!isFingerActive && totalActiveEnergyGenerated > 0) ? ((splitActiveRecovery && restingFingersCount > 0) ? totalActiveEnergyGenerated / restingFingersCount : totalActiveEnergyGenerated) : 0f;

            for(int l=0; l<3; l++) {
                ActionState state = matrixStates[f, l];
                if (activeMatrix[f, l]) {
                    if (enablePhase2Fatigue) {
                        state.holdTimer += Time.deltaTime;
                        if (state.holdTimer > p2_gracePeriod) state.health -= p2_decaySpeed * Time.deltaTime;
                    } else state.health = 1.0f;
                    progressTick += (phase2FillSpeed * state.health) * Time.deltaTime; 
                } else {
                    state.holdTimer = 0f;
                    state.health += (rechargeCurve.Evaluate(state.health) * passiveRechargeSpeed + activeBonusToApply) * Time.deltaTime;
                }
                state.health = Mathf.Clamp01(state.health);
            }
        }
        
        globalSeasonProgress += progressTick;

        // LE BOUCLIER : On empêche la Phase 2 de déborder sur la Phase 3
        if (globalSeasonProgress >= 2.0f) 
        {
            globalSeasonProgress = 2.0f;
        }
    }

    // --- PHASE 3 ---
    void UpdatePhase3Logic() {
        float realTotalP3 = 0f;
        for(int i=0; i<5; i++) realTotalP3 += phase3FingerProgress[i];
        float realGlobal = 2.0f + realTotalP3;
        
        if (globalSeasonProgress > realGlobal) {
            float missingJuice = globalSeasonProgress - realGlobal;
            for(int i=0; i<5; i++) {
                if (missingJuice <= 0) break; 
                float spaceLeft = 0.2f - phase3FingerProgress[i];
                if (spaceLeft > 0) { float add = Mathf.Min(spaceLeft, missingJuice); phase3FingerProgress[i] += add; missingJuice -= add; }
            }
        }
        
        int winner = GetStrongestFinger(); 
        if (winner != -1) {
            float force = currentRawForces[winner];
            float target = audioManager.GetTargetForceForFinger(winner); 
            float error = Mathf.Abs(force - target) / 5.0f; 
            currentAccuracyP3 = precisionCurve.Evaluate(Mathf.Clamp01(error)); 
            if (currentAccuracyP3 > 0.5f && phase3FingerProgress[winner] < 0.2f) {
                phase3FingerProgress[winner] += phase3FillSpeed * currentAccuracyP3 * Time.deltaTime;
                if (phase3FingerProgress[winner] > 0.2f) phase3FingerProgress[winner] = 0.2f;
            }
        } else currentAccuracyP3 = 0f;

        float totalP3 = 0f; for(int i=0; i<5; i++) totalP3 += phase3FingerProgress[i];
        globalSeasonProgress = 2.0f + totalP3;
    }

    // --- OUTILS ---
    public float GetFingerHealthForVisuals(int fingerIndex) {
        if (globalSeasonProgress < 1.0f) return phase1States[fingerIndex];
        else if (globalSeasonProgress < 2.0f) {
            float sum = matrixStates[fingerIndex, 0].health + matrixStates[fingerIndex, 1].health + matrixStates[fingerIndex, 2].health;
            return sum / 3.0f;
        }
        else return 1.0f; 
    }

    int GetPressureLevel(float force) {
        if (force > 8.0f) return 3;
        if (force > 4.0f) return 2;
        if (force > 0.5f) return 1;
        return 0;
    }

    int GetStrongestFinger() {
        int best = -1; float maxF = 0;
        for(int i=0; i<5; i++) if (currentRawForces[i] > maxF) { maxF = currentRawForces[i]; best = i; }
        return (maxF > 0.5f) ? best : -1;
    }
    
    public void GetAudioModifier(int finger, int layer, out float volMod, out float pitchMod) {
        float health = 1f;
        if (globalSeasonProgress < 1.0f) health = enablePhase1Fatigue ? phase1States[finger] : 1f;
        else if (globalSeasonProgress < 2.0f) health = enablePhase2Fatigue ? matrixStates[finger, layer].health : 1f;
        volMod = Mathf.Lerp(minVolumeFloor, 1.0f, health);
        if (enableDirtySound && health < 0.8f) pitchMod = 1.0f + Random.Range(-maxPitchJitter, maxPitchJitter) * (1f - health);
        else pitchMod = 1.0f;
    }
}