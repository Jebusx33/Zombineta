using UnityEngine;

namespace Zombineta.Enemies
{
    public enum FlipbookClip { Walk, Hit, Death }

    /// <summary>
    /// Cuadro de animacion de un zombie sin Animator: caminata en loop a la velocidad del zombie,
    /// impacto corto que vuelve a caminar, muerte que queda en el ultimo cuadro.
    /// </summary>
    public struct Flipbook
    {
        readonly int walkFrames, hitFrames, deathFrames;
        readonly float walkFps, hitSeconds, deathFps;
        float time;

        public FlipbookClip Clip { get; private set; }
        public int Frame { get; private set; }
        public bool Finished { get; private set; }

        public Flipbook(int walkFrames, int hitFrames, int deathFrames, float walkFps,
                        float hitSeconds = 0.2f, float deathFps = 10f)
        {
            this.walkFrames = walkFrames;
            this.hitFrames = hitFrames;
            this.deathFrames = deathFrames;
            this.walkFps = walkFps;
            this.hitSeconds = hitSeconds;
            this.deathFps = deathFps;
            time = 0f;
            Clip = FlipbookClip.Walk;
            Frame = 0;
            Finished = false;
        }

        public void Play(FlipbookClip clip)
        {
            Clip = clip;
            time = 0f;
            Frame = 0;
            Finished = false;
        }

        public void Tick(float dt, float speedRatio)
        {
            switch (Clip)
            {
                case FlipbookClip.Walk:
                    time += dt * Mathf.Max(0f, speedRatio);
                    Frame = walkFrames > 0 ? Mathf.FloorToInt(time * walkFps + 1e-4f) % walkFrames : 0;
                    break;

                case FlipbookClip.Hit:
                    time += dt;
                    if (time >= hitSeconds)
                    {
                        Play(FlipbookClip.Walk);
                        return;
                    }
                    Frame = hitFrames > 0 ? Mathf.Min(hitFrames - 1, Mathf.FloorToInt(time / hitSeconds * hitFrames)) : 0;
                    break;

                case FlipbookClip.Death:
                    time += dt;
                    int f = Mathf.FloorToInt(time * deathFps + 1e-4f);
                    int last = Mathf.Max(0, deathFrames - 1);
                    Frame = Mathf.Min(f, last);
                    Finished = f >= last;
                    break;
            }
        }
    }
}
