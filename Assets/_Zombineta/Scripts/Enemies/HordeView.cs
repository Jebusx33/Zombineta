using UnityEngine;
using Zombineta.Core;

namespace Zombineta.Enemies
{
    /// <summary>
    /// Dibuja la horda: un objeto por zombie simulado, con el tinte y la escala de su tipo.
    /// No decide nada; la masa vive en HordeSimulation. Dispara los triggers Hit y Die del
    /// Animator que ya trae el prefab, y reinicia la animacion cuando un zombie se recicla.
    /// </summary>
    public sealed class HordeView : MonoBehaviour
    {
        [SerializeField] RunController run;
        [SerializeField] Transform zombiePrefab;

        [Tooltip("Metros que la horda sigue avanzando por encima de la moto al atraparla.")]
        [SerializeField] float overrunMeters = 12f;

        [Header("Faro")]
        [Tooltip("Los zombies iluminados se aclaran y se echan para atras: se ve por que frenan.")]
        [SerializeField] Color litTint = new Color(1.25f, 1.2f, 1.1f);
        [SerializeField] float litLeanDegrees = -12f;
        [Tooltip("Metros detras de la moto que alcanza el cono del faro.")]
        [SerializeField] float headlightRangeMeters = 28f;

        Transform[] bodies;
        SpriteRenderer[] sprites;
        Animator[] animators;
        int[] generations;
        bool[] wasAlive;
        float[] lastStagger;

        // Solo presentacion: la simulacion ya termino al atraparte, pero la horda sigue
        // avanzando por encima de la moto (en camara lenta, con el tiempo escalado).
        float overrun;

        void Start()
        {
            if (run == null || zombiePrefab == null || run.Sim == null)
                return;

            int count = run.Sim.Horde.Units.Length;
            bodies = new Transform[count];
            sprites = new SpriteRenderer[count];
            animators = new Animator[count];
            generations = new int[count];
            wasAlive = new bool[count];
            lastStagger = new float[count];

            for (int i = 0; i < count; i++)
            {
                bodies[i] = Instantiate(zombiePrefab, transform);
                sprites[i] = bodies[i].GetComponentInChildren<SpriteRenderer>();
                animators[i] = bodies[i].GetComponentInChildren<Animator>();
                generations[i] = -1;
                wasAlive[i] = true;
            }
        }

        void LateUpdate()
        {
            if (run == null || run.Sim == null || bodies == null)
                return;

            var state = run.Sim.State;
            var horde = run.Sim.Horde;

            if (state.Phase == RunPhase.Lost)
                overrun = Mathf.Min(overrunMeters, overrun + run.Config.hordeBaseSpeed * Time.deltaTime);
            else if (state.Phase == RunPhase.Running)
                overrun = 0f;

            bool lightOn = state.HeadlightOn;

            for (int i = 0; i < bodies.Length; i++)
            {
                var u = horde.Units[i];
                var type = horde.Type(u.Type);

                // Se reciclo: es otro zombie, vuelve a caminar desde cero.
                if (generations[i] != u.Generation)
                {
                    generations[i] = u.Generation;
                    wasAlive[i] = true;
                    lastStagger[i] = 0f;
                    if (animators[i] != null)
                    {
                        animators[i].Rebind();
                        animators[i].Update(0f);
                    }
                }

                if (wasAlive[i] && !u.Alive && animators[i] != null)
                    animators[i].SetTrigger("Die");
                else if (u.Alive && u.Stagger > lastStagger[i] + 0.01f && animators[i] != null)
                    animators[i].SetTrigger("Hit");

                wasAlive[i] = u.Alive;
                lastStagger[i] = u.Stagger;

                bodies[i].position = new Vector3(
                    run.ToWorldX(u.X + overrun), run.LaneToWorldY(u.Lane), 0f);
                bodies[i].localScale = Vector3.one * type.scale;

                bool lit = lightOn && u.Alive &&
                           u.X < state.PlayerX && u.X > state.PlayerX - headlightRangeMeters;
                bodies[i].rotation = Quaternion.Euler(0f, 0f, lit ? litLeanDegrees : 0f);
                if (sprites[i] != null)
                    sprites[i].color = lit ? type.tint * litTint : type.tint;
            }
        }
    }
}
