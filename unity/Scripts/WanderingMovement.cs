using UnityEngine;

public class WanderingMovement : MonoBehaviour
{
    [Header("--- ZONE DE VOL ---")]
    public Vector3 boxSize = new Vector3(10, 5, 2); // Taille de la boîte
    public Vector3 centerPosition; // Centre de la boîte (défini au Start)

    [Header("--- MOUVEMENT (Perlin) ---")]
    public float speed = 1.0f; // Vitesse de déplacement
    public float volatility = 2.0f; // À quel point ça change de direction vite

    // Offsets aléatoires pour que chaque axe soit indépendant
    private float seedX, seedY, seedZ;

    void Start()
    {
        centerPosition = transform.position;
        // On initialise des graines aléatoires pour ne pas avoir le même mouvement à chaque fois
        seedX = Random.Range(0f, 100f);
        seedY = Random.Range(0f, 100f);
        seedZ = Random.Range(0f, 100f);
    }

    void Update()
    {
        // Le temps qui passe fait avancer le bruit
        float time = Time.time * speed;

        // Calcul de la nouvelle position avec Perlin Noise (résultat entre 0 et 1)
        // On remappe de (0 à 1) vers (-0.5 à 0.5) pour centrer, puis on multiplie par la taille
        float x = (Mathf.PerlinNoise(seedX + time, seedX + time * 0.5f) - 0.5f) * boxSize.x;
        float y = (Mathf.PerlinNoise(seedY + time, seedY + time * 0.5f) - 0.5f) * boxSize.y;
        float z = (Mathf.PerlinNoise(seedZ + time, seedZ + time * 0.5f) - 0.5f) * boxSize.z;

        // Application de la position lissée
        transform.position = centerPosition + new Vector3(x, y, z);
    }

    // Juste pour voir la boîte dans l'éditeur (Gizmos)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Application.isPlaying ? centerPosition : transform.position, boxSize);
    }
}