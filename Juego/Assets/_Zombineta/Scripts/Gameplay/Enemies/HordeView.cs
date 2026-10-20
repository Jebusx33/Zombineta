using UnityEngine;
using Zombineta.Core;
using Zombineta.Fx;
using Zombineta.Luz;

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

        [Tooltip("Color del zombie que se prepara para pegar en la pelea a pie.")]
        [SerializeField] Color avisoDeAtaque = new Color(1f, 0.25f, 0.2f);

        [Header("Aviso de ataque (pelea a pie)")]
        [Tooltip("Cuanto se echa hacia atras el zombie que prepara el golpe, en grados.")]
        [SerializeField] float inclinacionAviso = 16f;
        [Tooltip("Cuanto se lanza hacia adelante justo antes de pegar, en metros.")]
        [SerializeField] float estocadaAviso = 0.4f;
        [Tooltip("Aro que aparece en el piso bajo el zombie que va a pegar: se achica hasta el golpe.")]
        [SerializeField] Color colorAro = new Color(1f, 0.2f, 0.15f, 0.85f);

        [Header("Derribados por el remate")]
        [Tooltip("Cuanto se inclina el zombie tirado en el piso, en grados.")]
        [SerializeField] float anguloDerribado = 85f;

        [Tooltip("Carriles por segundo con los que un zombie cambia de carril: se desliza en vez de saltar.")]
        [SerializeField] float laneChangeSpeed = 4f;

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
        SombraProyectada[] shadows;
        int[] generations;
        float[] lastX;
        float[] saltoAlto;     // metros que lleva levantado el zombie que salta (se alisa para que no pegue saltos)
        float[] caminata;      // fraccion del ciclo de la caminata lenta (0..1)
        SpriteRenderer[] avisos;
        Sprite aroSprite;
        float[] laneVisual;
        RunPhase lastPhase = RunPhase.Running;
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
            shadows = new SombraProyectada[count];
            generations = new int[count];
            lastX = new float[count];
            saltoAlto = new float[count];
            caminata = new float[count];
            avisos = new SpriteRenderer[count];
            laneVisual = new float[count];
            wasAlive = new bool[count];
            lastStagger = new float[count];
            currentLook = new ZombieLook[count];
            flipbooks = new Flipbook[count];
            lastLookIndex = new int[count];

            for (int i = 0; i < count; i++)
            {
                bodies[i] = Instantiate(zombiePrefab, transform);
                ShadowCasterQuality.Apply(bodies[i].gameObject);
                sprites[i] = bodies[i].GetComponentInChildren<SpriteRenderer>();
                animators[i] = bodies[i].GetComponentInChildren<Animator>();
                generations[i] = -1;
                avisos[i] = CrearAviso(bodies[i]);
                wasAlive[i] = true;
                lastLookIndex[i] = -1;
                // La sombra sale sola de la silueta del zombie, segun las luces que tenga cerca.
                shadows[i] = SombraProyectada.Poner(bodies[i].gameObject, sprites[i], "", 0, true);
            }
        }

        /// <summary>El aro del aviso de ataque: vive como hijo del cuerpo pero se ubica en el piso, a mano, cada cuadro.</summary>
        SpriteRenderer CrearAviso(Transform cuerpo)
        {
            var go = new GameObject("Aviso de ataque");
            go.transform.SetParent(cuerpo, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AroSprite();
            sr.sortingLayerID = LaneSorting.GameLayerId;
            sr.enabled = false;
            return sr;
        }

        /// <summary>Un aro suave y blanco (se tine por color), hecho por codigo: no necesita arte.</summary>
        Sprite AroSprite()
        {
            if (aroSprite != null)
                return aroSprite;

            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixeles = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float r = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                    float borde = Mathf.Clamp01(1f - Mathf.Abs(r - 0.86f) / 0.12f);   // el anillo
                    float relleno = r < 0.86f ? 0.18f : 0f;                           // un velo adentro
                    byte a = (byte)(Mathf.Clamp01(Mathf.Max(borde, relleno)) * 255f);
                    pixeles[y * n + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(pixeles);
            tex.Apply(false, true);
            aroSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            aroSprite.name = "AroDeAviso";
            return aroSprite;
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
            // A pie cada zombie camina a su ritmo, no al del frente de la horda.
            bool pelea = state.Phase == RunPhase.Pelea;
            float speedRatio = state.Phase == RunPhase.Lost || pelea ? 1f
                : run.Config.hordeBaseSpeed > 0f ? horde.FrontSpeed / run.Config.hordeBaseSpeed : 0f;

            for (int i = 0; i < bodies.Length; i++)
            {
                var u = horde.Units[i];
                var type = horde.Type(u.Type);

                // Se reciclo: es otro zombie, vuelve a caminar desde cero (y puede tocarle otra pinta).
                if (generations[i] != u.Generation)
                {
                    generations[i] = u.Generation;
                    lastX[i] = u.X;
                    saltoAlto[i] = 0f;
                    caminata[i] = 0f;
                    laneVisual[i] = u.Lane;
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
                            look.walkFps, look.hitSeconds, look.deathFps);
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

                float desplazamiento = Mathf.Abs(u.X - lastX[i]);
                if (desplazamiento > 1.5f || state.Phase != lastPhase)
                    desplazamiento = 0f;
                lastX[i] = u.X;

                if (hasLook)
                {
                    if (wasAlive[i] && !u.Alive)
                    {
                        // La causa recien se conoce al morir: rearmar el flipbook con los cuadros que tocan.
                        var look = currentLook[i];
                        var death = look.DeathFor(u.Cause);
                        flipbooks[i] = new Flipbook(
                            look.walk != null ? look.walk.Length : 0,
                            look.hit != null ? look.hit.Length : 0,
                            death != null ? death.Length : 0,
                            look.walkFps, look.hitSeconds, look.deathFps);
                        flipbooks[i].Play(FlipbookClip.Death);
                    }
                    else if (u.Alive && u.Stagger > lastStagger[i] + 0.01f)
                        flipbooks[i].Play(FlipbookClip.Hit);

                    // Aturdido (o tirado por el remate) no camina: el cuadro se queda quieto.
                    flipbooks[i].Tick(Time.deltaTime, pelea && u.Alive && u.Stagger > 0f ? 0f : speedRatio);
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

                // El carril visual se desliza hasta el de la simulacion (que es entero). Al cambiar de
                // fase (empieza o termina la pelea) los zombies se reubican de golpe: ahi no se desliza.
                laneVisual[i] = state.Phase != lastPhase
                    ? u.Lane
                    : Mathf.MoveTowards(laneVisual[i], u.Lane, laneChangeSpeed * Time.deltaTime);

                float worldX = run.ToWorldX(u.X + overrun);
                float groundY = run.LaneToWorldY(laneVisual[i]);
                bodies[i].position = new Vector3(worldX, groundY, 0f);
                bodies[i].localScale = Vector3.one * bodyScale * type.scale * (hasLook ? currentLook[i].scale : 1f);

                bool lit = lightOn && u.Alive &&
                           u.X < state.PlayerX && u.X > state.PlayerX - headlightRangeMeters;

                float angulo = lit ? litLeanDegrees : 0f;
                float estocada = 0f;
                bool preparando = pelea && u.Alive && u.Preparando >= 0f && run.Config.zombiePreparacion > 0f;
                float avance = preparando ? Mathf.Clamp01(u.Preparando / run.Config.zombiePreparacion) : 0f;

                // Con cuadros dibujados de ataque o de derribo, el cuerpo no se inclina ni rota por codigo.
                var lookActual = currentLook[i];
                bool ataqueDibujado = preparando && lookActual != null && lookActual.attack != null && lookActual.attack.Length > 0;
                bool derribado = pelea && u.Alive && u.Derribado > 0f && u.DerribadoTotal > 0f;
                bool derriboDibujado = derribado && lookActual != null && lookActual.knockdown != null && lookActual.knockdown.Length > 0;
                // Saltando sobre ella (primera oleada): el cuadro sale de la fase y del tiempo del salto de la simulacion.
                // Recien golpeado en el piso (aterrizando) se ve el golpe, no el aterrizaje.
                bool saltoDibujado = pelea && u.Alive && u.SaltoFase != 0 && !(u.SaltoFase == 3 && u.Stagger > 0f) &&
                                     lookActual != null && lookActual.salto != null && lookActual.salto.Length > 0;
                float saltoObjetivo = 0f;   // cuanto se sube o baja el cuerpo respecto del piso
                float saltoNeto = 0f;       // altura real de los pies sobre el piso
                bool anunciaSalto = pelea && u.Alive && (u.SaltoFase == 1 || u.SaltoFase == 2);

                if (preparando && !ataqueDibujado)
                {
                    // Se echa hacia atras casi toda la preparacion y, al final, se lanza: se ve venir el golpe.
                    float atras = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(avance / 0.8f));
                    float lanza = Mathf.Clamp01((avance - 0.85f) / 0.15f);
                    angulo = u.Mira * inclinacionAviso * (atras * (1f - lanza) - 0.6f * lanza);
                    estocada = u.Mira * estocadaAviso * lanza;
                }
                else if (derribado && !derriboDibujado)
                {
                    // Cae hacia donde lo empujaron, queda tirado y se levanta en el ultimo tramo.
                    float t = 1f - u.Derribado / u.DerribadoTotal;
                    float cae = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.12f));
                    float sube = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.78f) / 0.22f));
                    angulo = -u.DerribadoDir * anguloDerribado * cae * (1f - sube);
                }

                bodies[i].rotation = Quaternion.Euler(0f, 0f, angulo);
                if (estocada != 0f)
                    bodies[i].position += new Vector3(estocada, 0f, 0f);

                // El aro del piso: aparece al empezar a preparar el golpe y se achica hasta que pega.
                // Lo mismo para el salto: el aro marca en el piso donde va a caer, y se achica hasta que cae.
                float avanceAro = avance;
                float xAro = worldX;
                float yAro = groundY;
                if (anunciaSalto)
                {
                    yAro = run.LaneToWorldY(u.SaltoLane);
                    var cfgSalto = run.Config;
                    avanceAro = u.SaltoFase == 1
                        ? 0.5f * Mathf.Clamp01(u.SaltoT / Mathf.Max(0.01f, cfgSalto.saltoAgacharse))
                        : 0.5f + 0.5f * Mathf.Clamp01(u.SaltoT / Mathf.Max(0.01f, cfgSalto.saltoVuelo));
                    xAro = run.ToWorldX(u.SaltoX1 + overrun);
                }

                var aviso = avisos[i];
                if (aviso != null)
                {
                    bool aro = preparando || anunciaSalto;
                    aviso.enabled = aro;
                    if (aro)
                    {
                        float escala = (1.5f - 0.7f * avanceAro) / Mathf.Max(0.0001f, bodies[i].localScale.x);
                        aviso.transform.rotation = Quaternion.identity;
                        aviso.transform.position = new Vector3(xAro, yAro + 0.02f, 0f);
                        aviso.transform.localScale = new Vector3(1.7f * escala, 0.55f * escala, 1f);
                        var col = colorAro;
                        col.a = colorAro.a * Mathf.Lerp(0.35f, 1f, avanceAro);
                        aviso.color = col;
                        aviso.sortingOrder = LaneSorting.Order(laneVisual[i], SortSlot.Shadow) + 2;
                    }
                }
                if (sprites[i] != null)
                {
                    if (hasLook)
                    {
                        var look = currentLook[i];
                        var frames = flipbooks[i].Clip == FlipbookClip.Walk ? look.walk
                                   : flipbooks[i].Clip == FlipbookClip.Hit ? look.hit
                                   : look.DeathFor(u.Cause);
                        if (frames != null && frames.Length > 0)
                            sprites[i].sprite = frames[Mathf.Clamp(flipbooks[i].Frame, 0, frames.Length - 1)];

                        // A pie, los lentos caminan arrastrando los pies: el ciclo avanza con lo que se
                        // desplazan de verdad (sin patinar) y quedan quietos si no se mueven. Los que corren, y
                        // todo en la moto, siguen con la animacion de correr.
                        bool caminaLento = pelea && u.Alive && !ataqueDibujado && !derriboDibujado &&
                                           flipbooks[i].Clip == FlipbookClip.Walk &&
                                           look.walkSlow != null && look.walkSlow.Length > 0 &&
                                           run.Sim.Pelea.VelocidadDe(u) < look.walkSlowSpeedLimit;
                        if (caminaLento)
                        {
                            if (u.Stagger <= 0f && look.walkSlowStrideMeters > 0f)
                                caminata[i] = Mathf.Repeat(caminata[i] + desplazamiento / look.walkSlowStrideMeters, 1f);
                            int n = look.walkSlow.Length;
                            sprites[i].sprite = look.walkSlow[Mathf.Min(n - 1, Mathf.FloorToInt(caminata[i] * n))];
                        }

                        // Preparando el golpe: los cuadros de ataque se reparten entre el inicio y el golpe.
                        if (ataqueDibujado)
                        {
                            int n = look.attack.Length;
                            sprites[i].sprite = look.attack[Mathf.Min(n - 1, Mathf.FloorToInt(avance * n))];
                        }
                        // Derribado por el remate: cae, queda tirado en el ultimo cuadro y se levanta.
                        else if (derriboDibujado)
                        {
                            int n = look.knockdown.Length;
                            float transcurrido = u.DerribadoTotal - u.Derribado;
                            float caida = Mathf.Min(look.knockdownFallSeconds, u.DerribadoTotal * 0.7f);
                            float k = caida > 0f ? Mathf.Clamp01(transcurrido / caida) : 1f;
                            sprites[i].sprite = look.knockdown[Mathf.Min(n - 1, Mathf.FloorToInt(k * n))];
                        }

                        // Saltando sobre ella: agacharse, vuelo y aterrizaje, segun la fase de la simulacion. Los cuadros
                        // del vuelo ya vienen levantados del piso (cada uno a su altura): para que el arco sea parejo,
                        // la altura que se le agrega es la del arco menos la que el cuadro ya trae. Asi sube, llega a
                        // 'saltoAltura' y baja hasta tocar el piso justo cuando la simulacion lo hace caer.
                        if (saltoDibujado)
                        {
                            var cfg = run.Config;
                            int n = look.salto.Length;
                            int primero = Mathf.Clamp(look.saltoPrimerCuadro, 0, Mathf.Max(0, n - 3));
                            int agachar = Mathf.Clamp(look.saltoCuadrosAgachar, 1, Mathf.Max(1, n - primero - 2));
                            int vuelo = Mathf.Clamp(look.saltoCuadrosVuelo, 1, Mathf.Max(1, n - primero - agachar - 1));
                            int aterrizar = Mathf.Max(1, n - primero - agachar - vuelo);
                            if (look.saltoCuadrosAterrizar > 0)
                                aterrizar = Mathf.Min(aterrizar, look.saltoCuadrosAterrizar);
                            int cuadro;
                            if (u.SaltoFase == 1)
                                cuadro = primero + Mathf.Min(agachar - 1, Mathf.FloorToInt(u.SaltoT / Mathf.Max(0.01f, cfg.saltoAgacharse) * agachar));
                            else if (u.SaltoFase == 2)
                                cuadro = primero + agachar + Mathf.Min(vuelo - 1, Mathf.FloorToInt(u.SaltoT / Mathf.Max(0.01f, cfg.saltoVuelo) * vuelo));
                            else
                                cuadro = primero + agachar + vuelo + Mathf.Min(aterrizar - 1, Mathf.FloorToInt(u.SaltoT / Mathf.Max(0.01f, cfg.saltoAterrizaje) * aterrizar));
                            var elegido = look.salto[Mathf.Clamp(cuadro, 0, n - 1)];
                            sprites[i].sprite = elegido;

                            if (u.SaltoFase == 2)
                            {
                                float t = Mathf.Clamp01(u.SaltoT / Mathf.Max(0.01f, cfg.saltoVuelo));
                                float arco = look.saltoAltura * 4f * t * (1f - t);
                                float yaTrae = Mathf.Max(0f, elegido.bounds.min.y) * bodies[i].localScale.y;
                                // Sin tope en cero: en los ultimos cuadros el dibujo todavia trae los pies en el aire y el arco
                                // ya llego abajo; el cuerpo baja lo que haga falta para tocar el piso justo al caer.
                                saltoObjetivo = arco - yaTrae;
                                saltoNeto = arco;
                            }
                        }

                        // Todos miden lo mismo; el tipo se distingue por un tinte suave (Zombies.asset).
                        sprites[i].color = lit ? type.tint * litTint : type.tint;

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

                    // En la pelea miran hacia la protagonista, y el que se prepara para pegar
                    // se pone rojo: el aviso para esquivarlo o pegarle primero.
                    sprites[i].flipX = pelea && u.Mira < 0;
                    if (pelea && u.Alive && u.Preparando >= 0f && !ataqueDibujado)
                        sprites[i].color = Color.Lerp(sprites[i].color, avisoDeAtaque, 0.6f);

                    // De pie tapa lo de arriba; caido queda como cualquier cosa tirada en el piso.
                    sprites[i].sortingOrder = LaneSorting.Order(laneVisual[i], u.Alive ? SortSlot.Zombie : SortSlot.Item, i);
                    sprites[i].sortingLayerID = LaneSorting.GameLayerId;
                }

                // El salto levanta el cuerpo. Sube tal cual la curva; si algo lo corta (un balazo) cae rapido, no de golpe.
                saltoAlto[i] = saltoObjetivo >= saltoAlto[i]
                    ? saltoObjetivo
                    : Mathf.MoveTowards(saltoAlto[i], saltoObjetivo, 8f * Time.deltaTime);
                if (saltoAlto[i] != 0f)
                    bodies[i].position += Vector3.up * saltoAlto[i];

                if (shadows[i] != null)
                {
                    // Debajo de todo lo de su carril, y solo de pie: el caido ya esta en el piso. En el aire no
                    // proyecta desde los pies (como la moto).
                    shadows[i].Visible = u.Alive && saltoNeto < 0.08f && saltoAlto[i] < 0.08f;
                    shadows[i].Orden = LaneSorting.Order(laneVisual[i], SortSlot.Shadow);
                }
            }

            lastPhase = state.Phase;
        }
    }
}
