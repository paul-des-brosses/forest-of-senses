using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Terrain))]
public class TerrainToMesh : MonoBehaviour
{
    // Plus le chiffre est petit, plus c'est détaillé (et lourd).
    // Pour ton style Low Poly, mets entre 2 et 4.
    // 1 = Qualité Max (Trop lourd)
    // 4 = Style "Zelda" (Bien)
    [Range(1, 10)] public int meshSimplification = 2; 

    public void Convert()
    {
        Terrain terrain = GetComponent<Terrain>();
        TerrainData data = terrain.terrainData;

        // 1. Préparation des données
        int w = data.heightmapResolution;
        int h = data.heightmapResolution;
        Vector3 size = data.size;
        float[,] heights = data.GetHeights(0, 0, w, h);

        // On saute des points pour simplifier (Low Poly)
        int step = meshSimplification;
        int wNew = (w - 1) / step + 1;
        int hNew = (h - 1) / step + 1;

        Vector3[] vertices = new Vector3[wNew * hNew];
        Vector2[] uvs = new Vector2[wNew * hNew];
        int[] triangles = new int[(wNew - 1) * (hNew - 1) * 6];

        // 2. Création des sommets (Vertices)
        for (int z = 0; z < hNew; z++)
        {
            for (int x = 0; x < wNew; x++)
            {
                int origX = x * step;
                int origZ = z * step;

                // Normalisation (0 à 1)
                float xNorm = (float)origX / (w - 1);
                float zNorm = (float)origZ / (h - 1);

                float y = heights[origZ, origX] * size.y;

                vertices[z * wNew + x] = new Vector3(xNorm * size.x, y, zNorm * size.z);
                uvs[z * wNew + x] = new Vector2(xNorm, zNorm);
            }
        }

        // 3. Création des triangles
        int triIndex = 0;
        for (int z = 0; z < hNew - 1; z++)
        {
            for (int x = 0; x < wNew - 1; x++)
            {
                int a = z * wNew + x;
                int b = (z + 1) * wNew + x;
                int c = z * wNew + x + 1;
                int d = (z + 1) * wNew + x + 1;

                triangles[triIndex++] = a;
                triangles[triIndex++] = b;
                triangles[triIndex++] = c;

                triangles[triIndex++] = b;
                triangles[triIndex++] = d;
                triangles[triIndex++] = c;
            }
        }

        // 4. Assemblage du Mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.name = "Snow_Generated_Mesh";

#if UNITY_EDITOR
        // Sauvegarder le mesh dans les assets pour ne pas le perdre
        string path = "Assets/_Game/Meshes/GeneratedSnow_" + Random.Range(0, 1000) + ".asset";
        if (!System.IO.Directory.Exists("Assets/_Game/Meshes"))
            AssetDatabase.CreateFolder("Assets/_Game", "Meshes");
            
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
        Debug.Log("Mesh sauvegardé ici : " + path);
#endif

        // 5. Création de l'objet dans la scène
        GameObject snowObj = new GameObject("Snow_Cover_Final");
        snowObj.transform.position = this.transform.position; // Même position que le terrain
        snowObj.AddComponent<MeshFilter>().mesh = mesh;
        snowObj.AddComponent<MeshRenderer>().material = new Material(Shader.Find("Universal Render Pipeline/Lit")); // Material par defaut
        
        Debug.Log("Terminé ! Tu peux supprimer le Terrain maintenant.");
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TerrainToMesh))]
public class TerrainToMeshEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        TerrainToMesh script = (TerrainToMesh)target;
        if (GUILayout.Button("CONVERTIR EN OBJET (Bake)"))
        {
            script.Convert();
        }
    }
}
#endif