using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using System.Text.RegularExpressions; // Pour lire le texte intelligemment
using System.Globalization; // Pour gérer les points et virgules
#endif

[CreateAssetMenu(fileName = "NewMelody", menuName = "Winter/Melody", order = 1)]
public class WinterMelody : ScriptableObject
{
    [System.Serializable]
    public struct NoteData
    {
        public int[] notes;      // Les notes (ex: [60, 64])
        public float rhythmValue; // La durée (ex: 1.0)
    }

    public List<NoteData> sequence = new List<NoteData>();

#if UNITY_EDITOR
    [Header("--- OUTIL D'IMPORTATION ---")]
    [TextArea(10, 20)] // Crée une grande zone de texte dans l'inspecteur
    public string dataToPasteHere = "";

    [ContextMenu("⚡ GÉNÉRER LA SÉQUENCE")]
    void GenerateFromText()
    {
        if (string.IsNullOrEmpty(dataToPasteHere)) return;

        // On sauvegarde l'état pour pouvoir faire "Ctrl+Z" si on se trompe
        Undo.RecordObject(this, "Import Melody");

        sequence.Clear();

        // On découpe le texte ligne par ligne
        string[] lines = dataToPasteHere.Split('\n');

        foreach (string line in lines)
        {
            string cleanLine = line.Trim();
            if (string.IsNullOrEmpty(cleanLine)) continue;
            if (cleanLine.StartsWith("|---")) continue; // Ignore les lignes de séparation Markdown
            if (cleanLine.ToLower().Contains("midi key")) continue; // Ignore l'en-tête

            // 1. Chercher les Notes entre crochets []
            // Regex : Trouve ce qu'il y a entre [ et ]
            var matchNotes = Regex.Match(cleanLine, @"\[(.*?)\]");
            
            // 2. Chercher la durée (un nombre décimal genre 1.5 ou 0.5)
            // On cherche le nombre qui est JUSTE APRÈS les crochets
            string restOfLine = cleanLine.Substring(matchNotes.Index + matchNotes.Length);
            var matchRhythm = Regex.Match(restOfLine, @"[0-9]+(\.[0-9]+)?");

            if (matchNotes.Success && matchRhythm.Success)
            {
                NoteData newData = new NoteData();

                // A. Parsing des Notes (ex: "60, 64, 67")
                string[] noteStrings = matchNotes.Groups[1].Value.Split(',');
                List<int> parsedNotes = new List<int>();
                foreach (var n in noteStrings)
                {
                    if (int.TryParse(n.Trim(), out int val)) parsedNotes.Add(val);
                }
                newData.notes = parsedNotes.ToArray();

                // B. Parsing du Rythme (ex: "1.5")
                // InvariantCulture permet de forcer le point '.' comme séparateur décimal
                if (float.TryParse(matchRhythm.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out float rVal))
                {
                    newData.rhythmValue = rVal;
                }
                else
                {
                    newData.rhythmValue = 1.0f; // Fallback
                }

                sequence.Add(newData);
            }
        }

        Debug.Log($"<color=green>Import terminé ! {sequence.Count} notes générées.</color>");
        // Marque l'objet comme "sale" pour que Unity sauvegarde les changements sur le disque
        EditorUtility.SetDirty(this); 
    }
#endif
}