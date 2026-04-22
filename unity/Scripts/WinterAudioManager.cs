using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class WinterAudioManager : MonoBehaviour
{
    [Header("--- 0. LIENS ---")]
    public SeasonManager seasonManager; 
    public AudioMixer mainMixer;
    public AudioMixerGroup groupPhase1;
    public AudioMixerGroup groupPhase2;
    public AudioMixerGroup groupPhase3;

    [Header("--- PARAMÈTRES MIXER ---")]
    public float minCutoffHz = 150f; 
    public float maxReverbMB = 0f;
    public float minReverbMB = -10000f;

    [Header("--- PARAMÈTRES SPÉCIFIQUES P3 ---")]
    [Tooltip("Fréquence MIN pour la phase 3 (quand tu es nul). Mets 2000 ou 3000 pour que ce soit moins étouffé.")]
    public float phase3MinCutoff = 2000f; 

    [Header("--- TRANSITIONS ---")]
    [Tooltip("Début du fondu P1 -> P2 (ex: 0.8)")]
    public float p1_p2_TransitionStart = 0.8f;
    [Tooltip("Fin du fondu P1 -> P2 (ex: 1.2)")]
    public float p1_p2_TransitionEnd = 1.2f;

    [System.Serializable] public class Phase1Settings { public AudioClip[] celloClips = new AudioClip[5]; }
    [System.Serializable] public class Phase2Settings { public AudioClip[] padNotes = new AudioClip[9]; public float delayBetweenNotes = 0.3f; }
    [System.Serializable] public class PianoSample { public string name; public AudioClip clip; public int rootKey = 60; }
    
    [System.Serializable]
    public class Phase3Settings {
        public List<PianoSample> library = new List<PianoSample>();
        public WinterMelody melodyThumb, melodyIndex, melodyMiddle, melodyRing, melodyPinky;
        [Range(1f, 10f)] public float targetForceThumb = 5f, targetForceIndex = 5f, targetForceMiddle = 5f, targetForceRing = 5f, targetForcePinky = 5f;
        public float minForce = 1.0f;
        public float maxBPM = 400f, minBPM = 60f;
        [Range(0.1f, 10f)] public float noteDuration = 5.0f; 
    }

    [Header("--- CONFIG PHASES ---")]
    public Phase1Settings p1;
    public Phase2Settings p2;
    public Phase3Settings p3;

    private AudioSource[] sourcesP1 = new AudioSource[5];
    private AudioSource[,] sourcesP2 = new AudioSource[5, 3];
    private List<AudioSource> pianoSourcesPool = new List<AudioSource>();
    private int[] p3_FingerIndexes = new int[5]; 
    private float p3_Timer = 0f;
    private int[] p2_CurrentNoteCount = new int[5];
    private int[] p2_TargetLocked = new int[5];
    private float[] p2_Timers = new float[5];
    private int[][] chordMapping = new int[][] { new int[] {0, 2, 4}, new int[] {1, 3, 5}, new int[] {2, 4, 6}, new int[] {3, 5, 7}, new int[] {4, 6, 8} };
    private float[] internalForces = new float[5];

    void Start() { CreateAudioSources(); CreatePianoPool(30); }

    public void UpdateFromManager(float[] forces) { for(int i=0; i<5; i++) internalForces[i] = forces[i]; }

    void Update() {
        if (seasonManager == null) return;
        int winnerIndex = -1; float maxVal = 0; 
        for (int i = 0; i < 5; i++) if (internalForces[i] > maxVal) { maxVal = internalForces[i]; winnerIndex = i; }

        UpdateSeasonMixer();
        UpdatePhase1_Cello(winnerIndex);
        UpdatePhase2_Pad(winnerIndex);
        UpdatePhase3_Piano(winnerIndex);
    }

    // --- PHASE 2 : PAD ---
    void UpdatePhase2_Pad(int winner) {
        // Optimisation : Si on est en Phase 3 ou en Phase 0 (Intro), pas de calculs
        if (seasonManager.globalSeasonProgress >= 2.0f || seasonManager.globalSeasonProgress < 0f) return;

        for (int i = 0; i < 5; i++) {
            float f = internalForces[i];
            int targetLevel = 0;
            if (f > 8.0f) targetLevel = 3; else if (f > 4.0f) targetLevel = 2; else if (f > 0.5f) targetLevel = 1;

            if (targetLevel > p2_TargetLocked[i]) p2_TargetLocked[i] = targetLevel;
            else if (p2_CurrentNoteCount[i] == p2_TargetLocked[i]) p2_TargetLocked[i] = targetLevel;

            p2_Timers[i] -= Time.deltaTime;
            if (p2_Timers[i] <= 0) {
                if (p2_CurrentNoteCount[i] < p2_TargetLocked[i]) {
                    p2_CurrentNoteCount[i]++; p2_Timers[i] = p2.delayBetweenNotes; PlayPadLayer(i, p2_CurrentNoteCount[i] - 1, true); 
                } else if (p2_CurrentNoteCount[i] > p2_TargetLocked[i]) {
                    PlayPadLayer(i, p2_CurrentNoteCount[i] - 1, false); p2_CurrentNoteCount[i]--; p2_Timers[i] = p2.delayBetweenNotes;
                }
            }

            for(int l=0; l < p2_CurrentNoteCount[i]; l++) {
                if (sourcesP2[i, l].isPlaying) {
                    float volMod, pitchMod;
                    seasonManager.GetAudioModifier(i, l, out volMod, out pitchMod);
                    sourcesP2[i, l].volume = 1.0f * volMod; 
                    sourcesP2[i, l].pitch = pitchMod;       
                }
            }
        }
    }

    // --- MIXER GLOBAL ---
    void UpdateSeasonMixer() {
        float progress = seasonManager.globalSeasonProgress;

        float w1 = 0f; float w2 = 0f; float w3 = 0f;

        // --- PHASE 0 : SILENCE TOTAL ---
        if (progress < 0.0f)
        {
            w1 = 0f; w2 = 0f; w3 = 0f;
            
            // On s'assure que les filtres sont fermés pour éviter un "pop" au démarrage de la phase 1
            mainMixer.SetFloat("Cut_P1", minCutoffHz);
            mainMixer.SetFloat("Cut_P3", minCutoffHz);
        }
        // --- LOGIQUE SÉGRÉGATION STRICTE ---
        else if (progress >= 2.0f) {
            // PHASE 3 (HARD CUT)
            w1 = 0f; w2 = 0f; w3 = 1f; 
        } else {
            // AVANT PHASE 3 (Transition P1 -> P2)
            w3 = 0f;
            float blend = Mathf.InverseLerp(p1_p2_TransitionStart, p1_p2_TransitionEnd, progress);
            w1 = 1.0f - blend;
            w2 = blend;
        }

        SetMixerVol("Vol_P1", w1); 
        SetMixerVol("Vol_P2", w2); 
        SetMixerVol("Vol_P3", w3);

        // Si on est en Phase 0, on sort ici pour ne pas écraser les filtres
        if (progress < 0.0f) return;

        // --- FILTRE PHASE 1 (Dégel) ---
        float p1Cutoff = minCutoffHz;
        if (progress < p1_p2_TransitionEnd) {
            float t = Mathf.Clamp01(progress / p1_p2_TransitionEnd);
            p1Cutoff = Mathf.Lerp(minCutoffHz, 22000f, t);
        } else {
            p1Cutoff = 22000f;
        }
        mainMixer.SetFloat("Cut_P1", p1Cutoff);

        // --- FILTRE PHASE 3 ---
        float p3Cutoff = phase3MinCutoff; 
        if (progress >= 2.0f) {
            float accuracy = seasonManager.currentAccuracyP3;
            p3Cutoff = Mathf.Lerp(phase3MinCutoff, 22000f, accuracy);
        }
        mainMixer.SetFloat("Cut_P3", p3Cutoff);
    }

    // --- PHASE 1 : CELLO ---
    void UpdatePhase1_Cello(int winner) {
        // Optimisation 1 : Si transition P1->P2 finie, stop.
        if (seasonManager.globalSeasonProgress > p1_p2_TransitionEnd && sourcesP1[0].isPlaying) {
             for(int i=0;i<5;i++) sourcesP1[i].Stop();
             return;
        }
        // Optimisation 2 : Si Phase 0 (Intro), pas de son.
        if (seasonManager.globalSeasonProgress < 0f) return;

        for (int i = 0; i < 5; i++) {
            float f = internalForces[i];
            if (i == winner && f > 0.5f) {
                if (!sourcesP1[i].isPlaying) sourcesP1[i].Play();
                float volMod, pitchMod;
                seasonManager.GetAudioModifier(i, 0, out volMod, out pitchMod);
                sourcesP1[i].volume = Mathf.Clamp01(f / 10f) * volMod;
                sourcesP1[i].pitch = pitchMod;
            } else {
                sourcesP1[i].volume = Mathf.Lerp(sourcesP1[i].volume, 0f, Time.deltaTime * 5f);
                if (sourcesP1[i].volume < 0.01f) sourcesP1[i].Stop();
            }
        }
    }

    // --- PHASE 3 : PIANO ---
    void UpdatePhase3_Piano(int winner) {
        // Sécurité : Pas de piano tant qu'on n'est pas à 2.0
        if (seasonManager.globalSeasonProgress < 2.0f) return;

        float force = (winner != -1) ? internalForces[winner] : 0;
        if (force < p3.minForce || winner == -1) return;

        WinterMelody m = GetMelodyForFinger(winner);
        if (m == null || m.sequence == null || m.sequence.Count == 0) return;

        float t = Mathf.Clamp01((force - p3.minForce) / (10f - p3.minForce));
        float currentBPM = Mathf.Lerp(p3.minBPM, p3.maxBPM, t);
        
        p3_Timer -= Time.deltaTime;
        if (p3_Timer <= 0) {
            int currentIndex = p3_FingerIndexes[winner];
            var noteData = m.sequence[currentIndex % m.sequence.Count];
            PlayPianoChord(noteData.notes);
            p3_Timer = (60f / currentBPM) * noteData.rhythmValue;
            p3_FingerIndexes[winner] = (currentIndex + 1) % m.sequence.Count;
        }
    }

    void PlayPianoChord(int[] midiNotes) {
        if (p3.library.Count == 0 || midiNotes == null) return;
        foreach (int note in midiNotes) {
            PianoSample best = null; int minDist = 1000;
            foreach (var s in p3.library) { int d = Mathf.Abs(s.rootKey - note); if (d < minDist) { minDist = d; best = s; } }
            if (best != null) {
                float pitch = Mathf.Pow(1.05946f, note - best.rootKey);
                AudioSource src = GetFreePianoSource();
                src.clip = best.clip; src.pitch = pitch; src.volume = 0.7f; src.Play();
                StartCoroutine(FadeOutAndStop(src, p3.noteDuration));
            }
        }
    }

    // --- UTILITAIRES ---
    public float GetTargetForceForFinger(int i) {
        switch(i) { case 0: return p3.targetForceThumb; case 1: return p3.targetForceIndex; case 2: return p3.targetForceMiddle; case 3: return p3.targetForceRing; case 4: return p3.targetForcePinky; default: return 5f; }
    }

    IEnumerator FadeOutAndStop(AudioSource source, float duration) {
        yield return new WaitForSeconds(duration);
        float t = 0; float startVol = source.volume;
        while (t < 0.5f) { t += Time.deltaTime; source.volume = Mathf.Lerp(startVol, 0f, t / 0.5f); yield return null; }
        source.Stop();
    }

    void PlayPadLayer(int finger, int layer, bool play) { if (layer < 0 || layer > 2) return; AudioSource src = sourcesP2[finger, layer]; if (play) { if (!src.isPlaying) src.Play(); } else { src.Stop(); } }

    WinterMelody GetMelodyForFinger(int i) { if (i == 0) return p3.melodyThumb; if (i == 1) return p3.melodyIndex; if (i == 2) return p3.melodyMiddle; if (i == 3) return p3.melodyRing; return p3.melodyPinky; }

    void SetMixerVol(string n, float w) { mainMixer.SetFloat(n, (w <= 0.001f) ? -80f : 20f * Mathf.Log10(w)); }

    AudioSource GetFreePianoSource() { foreach(var src in pianoSourcesPool) if (!src.isPlaying) return src; return CreatePianoSourceInPool(pianoSourcesPool[0].transform.parent, pianoSourcesPool.Count); }
    void CreatePianoPool(int count) { GameObject p = new GameObject("Piano_Pool_Container"); p.transform.parent = transform; pianoSourcesPool.Clear(); for (int i = 0; i < count; i++) CreatePianoSourceInPool(p.transform, i); }
    AudioSource CreatePianoSourceInPool(Transform parent, int i) { GameObject obj = new GameObject($"Piano_Voice_{i}"); obj.transform.parent = parent; AudioSource src = obj.AddComponent<AudioSource>(); src.outputAudioMixerGroup = groupPhase3; src.playOnAwake = false; src.spatialBlend = 0f; pianoSourcesPool.Add(src); return src; }
    void CreateAudioSources() { for (int i = 0; i < 5; i++) sourcesP1[i] = CreateSrc($"P1_Cello_{i}", p1.celloClips[i], groupPhase1); for (int i = 0; i < 5; i++) { int[] notes = chordMapping[i]; for (int j = 0; j < 3; j++) sourcesP2[i, j] = CreateSrc($"P2_Pad_{i}_{j}", p2.padNotes[notes[j]], groupPhase2); } }
    AudioSource CreateSrc(string n, AudioClip c, AudioMixerGroup g) { GameObject obj = new GameObject(n); obj.transform.parent = transform; AudioSource s = obj.AddComponent<AudioSource>(); s.clip = c; s.loop = true; s.playOnAwake = false; s.outputAudioMixerGroup = g; return s; }

#if UNITY_EDITOR
    [ContextMenu("🪄 AUTO-LOAD SÉLECTION")]
    void AutoLoadSelectedClips() { p3.library.Clear(); foreach (var obj in Selection.objects) { AudioClip clip = obj as AudioClip; if (clip != null) { PianoSample s = new PianoSample(); s.name = clip.name; s.clip = clip; s.rootKey = ParseNoteFromFilename(clip.name); p3.library.Add(s); } } }
    int ParseNoteFromFilename(string f) { string clean = f.ToUpper().Replace("PIANO", "").Trim(); string[] notes = {"C#", "D#", "F#", "G#", "A#", "C", "D", "E", "F", "G", "A", "B"}; int[] vals = {1, 3, 6, 8, 10, 0, 2, 4, 5, 7, 9, 11}; for(int i=0; i<notes.Length; i++) { if(clean.Contains(notes[i])) { int oct = 4; foreach(char c in clean) if(char.IsDigit(c)) oct = int.Parse(c.ToString()); return (oct + 1) * 12 + vals[i]; } } return 60; }
#endif
}