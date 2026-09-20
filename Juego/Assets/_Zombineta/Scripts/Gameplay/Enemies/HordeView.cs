using UnityEngine;
using Zombineta.Core;
using Zombineta.Fx;

namespace Zombineta.Enemies
{
    /// <summary>
    /// Dibuja la horda: un objeto por zombie simulado, con el look y la escala de su tipo.
    /// No decide nada; la masa vive en HordeSimulation. Si el tipo tiene looks (ZombieLookSet),
    /// sortea uno por unidad al reciclarse y anima caminata/impacto/muerte con un Flipbook por
    /// codigo, apagando el Animator del prefab; si no, cae en los triggers Hit/Die de siempre.
    /// </summary>
    public sealed class HordeView : MonoBehaviour
    {
        [SerializeField] RunController run;
        [SerializeField] Transform zombiePrefab;

        [Tooltip("Escala base de todos los zombies, antes de la del tipo (Zombies.asset) y la del look.")]
        [SerializeField] float bodyScale = 1f;

        [Header("Looks")]
        [Tooltip("Aspectos por tipo de zombie. Sin asignar (o el tipo sin looks): sigue el Animator del prefab.")]
        [SerializeField] ZombieLookSet looks;
        [Tooltip("Semilla del sorteo de look por unidad: mismo seed, misma pinta.")]
        [SerializeField] int lookSeed = 12345;

        [Header("Sombra")]
        [SerializeField] Sprite shadowSprite;
        [SerializeField] Color shadowColor = new Color(0f, 0f, 0f, 0.45f);

        [Tooltip("Ancho de la sombra relativo a sombra.png, con la escala base y la del tipo pero sin la " +
                 "del look (esa solo empareja la resolucion de cada hoja, no el tamano del cuerpo).")]
        [SerializeField] float shadowWidth = 1f;

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
        GroundShadow[] shadows;
        int[] generations;
        bool[] wasAlive;
        float[] lastStagger;

        // Looks por codigo: cuando la unidad tiene un look asignado, el flipbook maneja el sprite
        // y el Animator del prefab se apaga para que no lo pise.
        ZombieLook[] currentLook;
        Flipbook[] flipbooks;
        int[] lastLookIndex;

        // Solo presentacion: la simulacion ya termino al atraparte, pero la horda sigue
        // avanzando por encima de la moto (en camara lenta, con el tiempo escalado).
        float overrun;

#if UNITY_EDITOR
        // --- Solo lectura, solo editor: para verificar looks/flipbooks por MCP en Play. Nunca se
        // compila en un build (ni siquiera development), asi que no ensancha la API en runtime.
        // LookNameAt devuelve el nombre en vez del ZombieLook: el look es un asset compartido con
        // campos publicos, y devolver la referencia dejaria a cualquier caller mutarlo para todas
        // las unidades que lo usan.

        public int UnitCount => bodies != null ? bodies.Length : 0;

        public string LookNameAt(int index) =>
            currentLook != null && index >= 0 && index < currentLook.Length && currentLook[index] != null
                ? currentLook[index].name
                : null;

        public FlipbookClip ClipAt(int index) =>
            flipbooks != null && index >= 0 && index < flipbooks.Length ? flipbooks[index].Clip : FlipbookClip.Walk;

        public int FrameAt(int index) =>
            flipbooks != null && index >= 0 && index < flipbooks.Length ? flipbooks[index].Frame : 0;

        public bool FinishedAt(int index) =>
            flipbooks != null && index >= 0 && index < flipbooks.Length && flipbooks[index].Finished;
#endif

        void Start()
        {
            if (run == null || zombiePrefab == null || run.Sim == null)
                return;

            int count = run.Sim.Horde.Units.Length;
            bodies = new Transform[count];
            sprites = new SpriteRenderer[count];
            animators = new Animator[count];
            shadows = new GroundShadow[count];
            generations = new int[count];
            wasAlive = new bool[count];
            lastStagger = new float[count];
            currentLook = new ZombieLook[count];
            flipbooks = new Flipbook[count];
            lastLookIndex = new int[count];

            for (int i = 0; i < count; i++)
            {
                bodies[i] = Instantiate(zombiePrefab, transform);
                sprites[i] = bodies[i].GetComponentInChildren<SpriteRenderer>();
                animators[i] = bodies[i].GetComponentInChildren<Animator>();
                generations[i] = -1;
                wasAlive[i] = true;
                lastLookIndex[i] = -1;
                shadows[i] = CreateShadow(bodies[i]);
            }
        }

        /// <summary>
        /// Balanceo del sprite (en unidades locales del cuerpo y grados). Si el SpriteRenderer vive en
        /// un hijo, se mueve el hijo; si vive en la raiz del prefab (como Zombie.prefab), se suma a la
        /// pose del cuerpo ya ubicada: tocar su localPosition lo mandaria al origen del mundo.
        /// </summary>
        void SetSpriteOffset(int i, float bobY, float bobDegrees)
        {
            var t = sprites[i].transform;
            if (t != bodies[i])
            {
                t.localPosition = new Vector3(0f, bobY, 0f);
                t.localRotation = Quaternion.Euler(0f, 0f, bobDegrees);
                return;
            }

            if (bobY == 0f && bobDegrees == 0f)
                return;
            t.position += Vector3.up * (bobY * t.localScale.y);
            t.rotation *= Quaternion.Euler(0f, 0f, bobDegrees);
        }

        GroundShadow CreateShadow(Transform parent)
        {
            var go = new GameObject("Sombra");
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = shadowSprite;
            renderer.color = shadowColor;
            var shadow = go.AddComponent<GroundShadow>();
            shadow.Init(renderer);
            return shadow;
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
            // En el overrun de Lost la simulacion ya no tickea (FrontSpeed queda congelado en lo
            // que valia al perder), pero la horda sigue avanzando visualmente: las piernas tienen
            // que acompanar ese avance, no quedar congeladas en la velocidad de antes de perder.
            float speedRatio = state.Phase == RunPhase.Lost ? 1f
                : run.Config.hordeBaseSpeed > 0f ? horde.FrontSpeed / run.Config.hordeBaseSpeed : 0f;

            for (int i = 0; i < bodies.Length; i++)
            {
                var u = horde.Units[i];
                var type = horde.Type(u.Type);

                // Se reciclo: es otro zombie, vuelve a caminar desde cero (y puede tocarle otra pinta).
                if (generations[i] != u.Generation)
                {
                    generations[i] = u.Generation;
                    wasAlive[i] = true;
                    lastStagger[i] = 0f;

                    var pool = looks != null ? looks.For(u.Type) : null;
                    int poolCount = pool != null ? pool.Count : 0;
                    int idx = ZombieLookPicker.Pick(lookSeed, i, u.Generation, poolCount, lastLookIndex[i]);
                    lastLookIndex[i] = idx;
                    currentLook[i] = idx >= 0 ? pool[idx] : null;

                    if (currentLook[i] != null)
                    {
                        var look = currentLook[i];
                        flipbooks[i] = new Flipbook(
                            look.walk != null ? look.walk.Length : 0,
                            look.hit != null ? look.hit.Length : 0,
                            look.death != null ? look.death.Length : 0,
                            look.walkFps);
                        if (animators[i] != null)
                            animators[i].enabled = false;
                    }
                    else if (animators[i] != null)
                    {
                        animators[i].enabled = true;
                        animators[i].Rebind();
                        animators[i].Update(0f);
                    }
                }

                bool hasLook = currentLook[i] != null;

                if (hasLook)
                {
                    if (wasAlive[i] && !u.Alive)
                        flipbooks[i].Play(FlipbookClip.Death);
                    else if (u.Alive && u.Stagger > lastStagger[i] + 0.01f)
                        flipbooks[i].Play(FlipbookClip.Hit);

                    flipbooks[i].Tick(Time.deltaTime, speedRatio);
                }
                else
                {
                    if (wasAlive[i] && !u.Alive && animators[i] != null)
                        animators[i].SetTrigger("Die");
                    else if (u.Alive && u.Stagger > lastStagger[i] + 0.01f && animators[i] != null)
                        animators[i].SetTrigger("Hit");
                }

                wasAlive[i] = u.Alive;
                lastStagger[i] = u.Stagger;

                float worldX = run.ToWorldX(u.X + overrun);
                float groundY = run.LaneToWorldY(u.Lane);
                bodies[i].position = new Vector3(worldX, groundY, 0f);
                bodies[i].localScale = Vector3.one * bodyScale * type.scale * (hasLook ? currentLook[i].scale : 1f);

                bool lit = lightOn && u.Alive &&
                           u.X < state.PlayerX && u.X > state.PlayerX - headlightRangeMeters;
                bodies[i].rotation = Quaternion.Euler(0f, 0f, lit ? litLeanDegrees : 0f);
                if (sprites[i] != null)
                {
                    if (hasLook)
                    {
                        var look = currentLook[i];
                        var frames = flipbooks[i].Clip == FlipbookClip.Walk ? look.walk
                                   : flipbooks[i].Clip == FlipbookClip.Hit ? look.hit
                                   : look.death;
                        if (frames != null && frames.Length > 0)
                            sprites[i].sprite = frames[Mathf.Clamp(flipbooks[i].Frame, 0, frames.Length - 1)];

                        // El arte ya diferencia los looks: color blanco salvo el aviso del faro.
                        sprites[i].color = lit ? litTint : Color.white;

                        if (look.poseBob)
                        {
                            float t = Time.time * 9f + i;
                            SetSpriteOffset(i, Mathf.Sin(t) * 0.04f, Mathf.Sin(t) * 3f);
                        }
                        else
                        {
                            SetSpriteOffset(i, 0f, 0f);
                        }
                    }
                    else
                    {
                        sprites[i].color = lit ? type.tint * litTint : type.tint;
                        SetSpriteOffset(i, 0f, 0f);
                    }

                    // De pie tapa lo de arriba; caido queda como cualquier cosa tirada en el piso.
                    sprites[i].sortingOrder = LaneSorting.Order(u.Lane, u.Alive ? SortSlot.Zombie : SortSlot.Item);
                    sprites[i].sortingLayerName = LaneSorting.GameLayer;
                }

                if (shadows[i] != null)
                {
                    // La sombra es hija del cuerpo: descontar la escala del look para que no crezca con ella.
                    float lookScale = hasLook && currentLook[i].scale > 0f ? currentLook[i].scale : 1f;
                    shadows[i].Width = shadowWidth / lookScale;
                    shadows[i].Visible = u.Alive;
                    if (u.Alive)
                        shadows[i].Place(worldX, groundY, 0f, u.Lane);
                }
            }
        }
    }
}
