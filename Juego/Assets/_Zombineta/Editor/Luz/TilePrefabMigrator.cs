using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Zombineta.Scenery;

namespace Zombineta.Juego.EditorTools
{
    // Convierte cada SceneryVariant.sprite del tileset en un prefab propio, para que
    // arte tenga un objeto donde meterle Light2D/ShadowCaster2D/Flicker. No borra
    // 'sprite': lo deja solo como dato de migracion.
    public static class TilePrefabMigrator
    {
        const string PrefabFolder = "Assets/_Zombineta/Art/Tileset/Prefabs";

        // Se resuelve via AssetDatabase (mapea al paquete instalado, sin importar el
        // hash de carpeta que use el Package Manager en Library/PackageCache).
        const string UrpSpriteLitDefaultPath =
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";

        [MenuItem("Zombineta/Luz/Migrar tiles a prefabs")]
        public static void Migrar()
        {
            string[] guids = AssetDatabase.FindAssets("t:SceneryTileset");
            if (guids.Length == 0)
            {
                Debug.LogError("TilePrefabMigrator: no se encontro ningun SceneryTileset en el proyecto.");
                return;
            }

            EnsureFolder();

            Material fallback = LoadFallbackMaterial();
            var cache = new Dictionary<Sprite, GameObject>();

            int creados = 0;
            int reutilizados = 0;
            int sinPrefab = 0;

            foreach (string guid in guids)
            {
                string tilesetPath = AssetDatabase.GUIDToAssetPath(guid);
                var tileset = AssetDatabase.LoadAssetAtPath<SceneryTileset>(tilesetPath);
                if (tileset == null)
                    continue;

                bool dirty = false;

                foreach (var layer in tileset.layers)
                {
                    if (layer == null)
                        continue;

                    foreach (var variant in layer.variants)
                    {
                        if (variant == null || variant.sprite == null)
                        {
                            sinPrefab++;
                            continue;
                        }

                        if (string.IsNullOrEmpty(variant.grupo))
                            variant.grupo = "ciudad";

                        if (cache.TryGetValue(variant.sprite, out GameObject cached))
                        {
                            if (variant.prefab != cached)
                            {
                                variant.prefab = cached;
                                dirty = true;
                            }
                            reutilizados++;
                            continue;
                        }

                        string prefabPath = PrefabFolder + "/" + variant.sprite.name + ".prefab";
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                        if (prefab == null)
                        {
                            Material material = layer.material != null ? layer.material : fallback;
                            prefab = CreatePrefab(variant.sprite, material, prefabPath);
                            creados++;
                        }

                        cache[variant.sprite] = prefab;
                        if (variant.prefab != prefab)
                        {
                            variant.prefab = prefab;
                            dirty = true;
                        }
                    }
                }

                if (dirty)
                    EditorUtility.SetDirty(tileset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("TilePrefabMigrator: " + creados + " prefabs creados, " +
                      sinPrefab + " variantes sin sprite (quedan sin prefab).");
        }

        static GameObject CreatePrefab(Sprite sprite, Material material, string path)
        {
            var root = new GameObject(sprite.name);
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            if (material != null)
                sr.sharedMaterial = material;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
            Object.DestroyImmediate(root);

            if (!success)
                Debug.LogError("TilePrefabMigrator: fallo al guardar el prefab " + path);

            return prefab;
        }

        static void EnsureFolder()
        {
            if (AssetDatabase.IsValidFolder(PrefabFolder))
                return;

            if (!AssetDatabase.IsValidFolder("Assets/_Zombineta/Art/Tileset"))
                AssetDatabase.CreateFolder("Assets/_Zombineta/Art", "Tileset");

            AssetDatabase.CreateFolder("Assets/_Zombineta/Art/Tileset", "Prefabs");
        }

        static Material LoadFallbackMaterial()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(UrpSpriteLitDefaultPath);
            if (mat != null)
            {
                Debug.Log("TilePrefabMigrator: material por defecto resuelto en " + UrpSpriteLitDefaultPath);
                return mat;
            }

            string[] guids = AssetDatabase.FindAssets("Sprite-Lit-Default t:Material");
            if (guids.Length > 0)
            {
                string fallbackPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                Debug.Log("TilePrefabMigrator: 'Packages/...' no resolvio, se uso FindAssets: " + fallbackPath);
                return AssetDatabase.LoadAssetAtPath<Material>(fallbackPath);
            }

            Debug.LogWarning("TilePrefabMigrator: no se encontro Sprite-Lit-Default; los prefabs sin " +
                             "material de capa quedaran con el material por defecto de Unity.");
            return null;
        }
    }
}
