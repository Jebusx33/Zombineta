using UnityEngine;
using Zombineta.Core;

namespace Zombineta.Level
{
    /// <summary>
    /// Dibuja el recorrido. Es solo presentacion: quien decide que agarraste o
    /// contra que chocaste es LevelRuntime. Recicla un punado de sprites sobre la
    /// ventana visible, asi un nivel de 4 km no cuesta 4 km de GameObjects.
    /// </summary>
    public sealed class LevelSpawner : MonoBehaviour
    {
        [SerializeField] RunController run;
        [SerializeField] SpriteRenderer itemPrefab;

        [Tooltip("Metros por delante y por detras de la jugadora que se mantienen dibujados.")]
        [SerializeField] float visibleRangeMeters = 90f;

        [SerializeField] int poolSize = 48;

        [Header("Colores de greybox")]
        [SerializeField] Color obstacleColor = new Color(0.55f, 0.55f, 0.6f);
        [SerializeField] Color fuelColor = new Color(1f, 0.65f, 0.15f);
        [SerializeField] Color batteryColor = new Color(0.3f, 0.85f, 0.95f);
        [SerializeField] Color ammoColor = new Color(0.95f, 0.9f, 0.4f);

        [Header("Rampa")]
        [Tooltip("Cuna con el pivot abajo a la derecha: el borde alto es donde lanza.")]
        [SerializeField] Sprite rampSprite;
        [SerializeField] Color rampColor = new Color(0.85f, 0.45f, 0.2f);

        SpriteRenderer[] pool;
        Sprite defaultSprite;

        void Start()
        {
            if (run == null || itemPrefab == null)
                return;

            defaultSprite = itemPrefab.sprite;
            pool = new SpriteRenderer[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                pool[i] = Instantiate(itemPrefab, transform);
                pool[i].gameObject.SetActive(false);
            }
        }

        void LateUpdate()
        {
            if (pool == null || run.Sim == null || run.Level == null)
                return;

            float playerX = run.Sim.State.PlayerX;
            float min = playerX - visibleRangeMeters;
            float max = playerX + visibleRangeMeters;

            var items = run.Level.Items;
            int used = 0;

            for (int i = 0; i < items.Length && used < pool.Length; i++)
            {
                var item = items[i];
                float d = item.Entry.distance;

                if (d < min) continue;
                if (d > max) break;      // van ordenadas por distancia
                if (item.Consumed) continue;

                var sr = pool[used++];
                sr.gameObject.SetActive(true);
                var kind = item.Entry.kind;
                sr.sprite = kind == LevelEntryKind.Ramp && rampSprite != null ? rampSprite : defaultSprite;
                sr.transform.position = new Vector3(
                    run.ToWorldX(d),
                    run.LaneToWorldY(item.Entry.lane) + run.HeightToWorld(item.Entry.height),
                    0f);
                sr.transform.localScale = ScaleFor(kind);
                sr.color = ColorFor(kind);
            }

            for (int i = used; i < pool.Length; i++)
                if (pool[i].gameObject.activeSelf)
                    pool[i].gameObject.SetActive(false);
        }

        Color ColorFor(LevelEntryKind kind)
        {
            switch (kind)
            {
                case LevelEntryKind.Fuel: return fuelColor;
                case LevelEntryKind.Battery: return batteryColor;
                case LevelEntryKind.Ammo: return ammoColor;
                case LevelEntryKind.Ramp: return rampColor;
                default: return obstacleColor;
            }
        }

        // Los obstaculos ocupan el carril; los recursos son chicos y flotan; la rampa ya viene a escala.
        static Vector3 ScaleFor(LevelEntryKind kind)
        {
            switch (kind)
            {
                case LevelEntryKind.Obstacle: return new Vector3(1.1f, 1.2f, 1f);
                case LevelEntryKind.Ramp: return Vector3.one;
                default: return new Vector3(0.55f, 0.55f, 1f);
            }
        }
    }
}
