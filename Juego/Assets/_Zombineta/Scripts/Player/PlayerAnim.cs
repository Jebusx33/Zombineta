using UnityEngine;

namespace Zombineta.Player
{
    public enum PlayerClip { Idle, Down, Up, Shoot, Crash }

    /// <summary>
    /// Animacion de la jugadora sin Animator, mismo criterio que Flipbook (zombies): Idle en
    /// loop; Down, Up y Shoot son un solo ciclo que vuelve solo a Idle; Crash es un solo ciclo
    /// que se congela en el ultimo cuadro (la vista lo saca de Crash cuando el estado deja de
    /// estar caido, no este struct).
    /// </summary>
    public struct PlayerAnim
    {
        readonly int idleFrames, downFrames, upFrames, shootFrames, crashFrames;
        readonly float idleFps, downFps, upFps, shootFps, crashFps;
        float time;

        public PlayerClip Clip { get; private set; }
        public int Frame { get; private set; }
        public bool Finished { get; private set; }

        public PlayerAnim(int idleFrames, int downFrames, int upFrames, int shootFrames, int crashFrames,
                          float idleFps, float downFps, float upFps, float shootFps, float crashFps)
        {
            this.idleFrames = idleFrames;
            this.downFrames = downFrames;
            this.upFrames = upFrames;
            this.shootFrames = shootFrames;
            this.crashFrames = crashFrames;
            this.idleFps = idleFps;
            this.downFps = downFps;
            this.upFps = upFps;
            this.shootFps = shootFps;
            this.crashFps = crashFps;
            time = 0f;
            Clip = PlayerClip.Idle;
            Frame = 0;
            Finished = false;
        }

        public void Play(PlayerClip clip)
        {
            Clip = clip;
            time = 0f;
            Frame = 0;
            Finished = false;
        }

        /// <summary>
        /// Arranca el clip ya en el cuadro 'cuadro' (desde 0). El disparo empieza en el cuadro en que el
        /// brazo apunta: el tiro, el fogonazo y el sonido salen en el mismo instante en que se aprieta.
        /// </summary>
        public void Play(PlayerClip clip, int cuadro)
        {
            Play(clip);
            float fps = FpsDe(clip);
            int frames = FramesDe(clip);
            if (fps <= 0f || frames <= 0)
                return;
            Frame = Mathf.Clamp(cuadro, 0, frames - 1);
            time = Frame / fps;
        }

        float FpsDe(PlayerClip clip) => clip switch
        {
            PlayerClip.Down => downFps,
            PlayerClip.Up => upFps,
            PlayerClip.Shoot => shootFps,
            PlayerClip.Crash => crashFps,
            _ => idleFps,
        };

        int FramesDe(PlayerClip clip) => clip switch
        {
            PlayerClip.Down => downFrames,
            PlayerClip.Up => upFrames,
            PlayerClip.Shoot => shootFrames,
            PlayerClip.Crash => crashFrames,
            _ => idleFrames,
        };

        public void Tick(float dt)
        {
            time += dt;
            switch (Clip)
            {
                case PlayerClip.Idle:
                    Frame = idleFrames > 0 ? Mathf.FloorToInt(time * idleFps + 1e-4f) % idleFrames : 0;
                    break;

                case PlayerClip.Down: TickOneShot(downFrames, downFps, PlayerClip.Idle); break;
                case PlayerClip.Up: TickOneShot(upFrames, upFps, PlayerClip.Idle); break;
                case PlayerClip.Shoot: TickOneShot(shootFrames, shootFps, PlayerClip.Idle); break;

                case PlayerClip.Crash:
                    int last = Mathf.Max(0, crashFrames - 1);
                    int f = crashFrames > 0 ? Mathf.FloorToInt(time * crashFps + 1e-4f) : 0;
                    Frame = Mathf.Min(f, last);
                    Finished = f >= last;
                    break;
            }
        }

        // Un solo ciclo: avanza hasta el ultimo cuadro y ahi mismo pasa a 'next' (Idle).
        void TickOneShot(int frames, float fps, PlayerClip next)
        {
            int f = frames > 0 ? Mathf.FloorToInt(time * fps + 1e-4f) : 0;
            if (f >= frames)
            {
                Play(next);
                return;
            }
            Frame = f;
        }
    }
}
