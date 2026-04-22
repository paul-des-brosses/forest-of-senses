using UnityEngine;

public class SeasonObjectMover : MonoBehaviour
{
    [Header("--- LIENS ---")]
    public SeasonManager seasonManager;

    [Header("--- TIMING (Quand l'objet bouge-t-il ?) ---")]
    [Tooltip("À quelle valeur de la saison le mouvement commence ?")]
    [Range(0f, 3f)] public float moveStartSeason = 2.0f;

    [Tooltip("À quelle valeur de la saison le mouvement est terminé ?")]
    [Range(0f, 3f)] public float moveEndSeason = 2.5f;

    [Header("--- POSITIONS (Hauteur Y) ---")]
    public float startY = -0.8f; // Hauteur initiale (Haut)
    public float endY = -2.5f;   // Hauteur finale (Bas)

    void Update()
    {
        if (seasonManager == null) return;

        // 1. On récupère la progression globale
        float globalProgress = seasonManager.globalSeasonProgress;

        // 2. On calcule le "t" (0 à 1) spécifique à cet objet
        // Si globalProgress = moveStartSeason, t = 0
        // Si globalProgress = moveEndSeason, t = 1
        float t = Mathf.InverseLerp(moveStartSeason, moveEndSeason, globalProgress);

        // 3. On calcule la nouvelle position Y exacte
        float newY = Mathf.Lerp(startY, endY, t);

        // 4. On applique la position (en gardant X et Z intacts)
        Vector3 currentPos = transform.position;
        currentPos.y = newY;
        transform.position = currentPos;
    }
}