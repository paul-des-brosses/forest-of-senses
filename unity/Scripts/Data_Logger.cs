using UnityEngine;
using System.IO;
using System.Text;
using System;
using System.Globalization;

public class DataLogger : MonoBehaviour
{
    [Header("--- LIENS ---")]
    public SeasonManager seasonManager;

    [Header("--- PROTOCOLE D'ÉTUDE ---")]
    [Tooltip("Cochez cette case pour activer la création du fichier CSV. Décochez pour de simples tests sans enregistrement.")]
    public bool enableLogging = false;
    
    [Tooltip("Identifiant du participant (ex: Sujet_01_Gaucher). Sera inclus au début du nom de fichier.")]
    public string participantID = "Test";

    [Header("--- PARAMÈTRES D'ENREGISTREMENT ---")]
    [Tooltip("Fréquence d'enregistrement en Hz (ex: 20 = 20 fois par seconde)")]
    public float recordFrequency = 20f;

    private string filePath;
    private StreamWriter writer;
    private float timer;
    private float timeSinceStart;
    private bool isCurrentlyLogging = false;

    void Start()
    {
        if (!enableLogging) 
        {
            Debug.Log("<color=yellow>DataLogger : Enregistrement désactivé (Mode Test). Aucun CSV ne sera créé.</color>");
            return;
        }

        string folderPath = Path.Combine(Application.dataPath, "StudyData");
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        string fileName = participantID + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
        filePath = Path.Combine(folderPath, fileName);

        writer = new StreamWriter(filePath, false, Encoding.UTF8);
        
        WriteHeader();
        
        isCurrentlyLogging = true;
        timeSinceStart = 0f;
        Debug.Log($"<color=cyan>DataLogger : Début de l'enregistrement à {recordFrequency}Hz -> {filePath}</color>");
    }

    void WriteHeader()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("Time(s),Phase,Progress,WinnerIndex,");
        
        for (int i = 0; i < 5; i++) sb.Append($"Force_F{i},");
        for (int i = 0; i < 5; i++) sb.Append($"Fatigue_F{i},");
        
        sb.Append("P3_Accuracy");
        writer.WriteLine(sb.ToString());
    }

    void Update()
    {
        if (!isCurrentlyLogging || seasonManager == null) return;

        timeSinceStart += Time.deltaTime;
        timer += Time.deltaTime;

        if (timer >= (1f / recordFrequency))
        {
            LogData();
            timer = 0f; 
        }
    }

    void LogData()
    {
        float progress = seasonManager.globalSeasonProgress;
        int currentPhase = 0;
        if (progress < 0f) currentPhase = 0;
        else if (progress < 1f) currentPhase = 1;
        else if (progress < 2f) currentPhase = 2;
        else currentPhase = 3;

        CultureInfo ci = CultureInfo.InvariantCulture;
        StringBuilder sb = new StringBuilder();

        // 1. TEMPS ET CONTEXTE GLOBAL
        sb.Append(timeSinceStart.ToString("F2", ci) + ",");
        sb.Append(currentPhase.ToString() + ",");
        sb.Append(progress.ToString("F3", ci) + ",");

        // Calcul du Winner Index (L'index du doigt qui a l'impact dans le jeu)
        int winner = -1;
        if (currentPhase > 0)
        {
            float maxF = 0;
            for (int i = 0; i < 5; i++) { if (seasonManager.currentRawForces[i] > maxF) { maxF = seasonManager.currentRawForces[i]; winner = i; } }
            if (maxF <= 0.1f) winner = -1;
        }
        sb.Append(winner.ToString() + ",");

        // 2. FORCES BRUTES ABSOLUES (Correction pour l'étude)
        // On by-pass totalement la logique du jeu pour enregistrer le matériel pur.
        for (int i = 0; i < 5; i++)
        {
            float rawForce = seasonManager.useDebugInput 
                ? seasonManager.debugFingerForces[i] 
                : (float)Manipulandum_data_aquired.Force_Data[i];
                
            float absoluteForce = Mathf.Abs(rawForce) * seasonManager.forceMultiplier;
            sb.Append(absoluteForce.ToString("F2", ci) + ",");
        }

        // 3. FATIGUES
        for (int i = 0; i < 5; i++)
        {
            if (currentPhase == 0 || currentPhase == 3)
            {
                sb.Append("NaN,");
            }
            else
            {
                float health = seasonManager.GetFingerHealthForVisuals(i);
                sb.Append(health.ToString("F2", ci) + ",");
            }
        }

        // 4. PRECISION PHASE 3
        if (currentPhase == 3)
        {
            sb.Append(seasonManager.currentAccuracyP3.ToString("F3", ci));
        }
        else
        {
            sb.Append("NaN");
        }

        writer.WriteLine(sb.ToString());
    }

    void OnApplicationQuit()
    {
        if (writer != null && isCurrentlyLogging)
        {
            isCurrentlyLogging = false;
            writer.Flush();
            writer.Close();
            Debug.Log("<color=cyan>DataLogger : Fichier CSV sauvegardé avec succès.</color>");
        }
    }
}