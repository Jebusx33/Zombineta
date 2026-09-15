using System;
using UnityEngine;
using Zombineta.Core;

namespace Zombineta.Fx
{
    /// <summary>
    /// La vibracion del joystick sin tocar el joystick: cada evento arranca a su fuerza y se apaga
    /// solo; si hay varios, cada motor toma el mas fuerte. GamepadRumble aplica Low/High.
    /// </summary>
    public sealed class RumbleDirector
    {
        static readonly int KindCount = Enum.GetValues(typeof(RumbleKind)).Length;

        readonly RumbleConfig config;
        readonly float[] left = new float[KindCount];

        public float Low { get; private set; }
        public float High { get; private set; }

        public RumbleDirector(RumbleConfig config) => this.config = config;

        public void Trigger(RumbleKind kind)
        {
            var e = config != null ? config.Get(kind) : null;
            if (e != null)
                left[(int)kind] = e.seconds;
        }

        public void Trigger(RunEvent events)
        {
            if ((events & RunEvent.Shot) != 0) Trigger(RumbleKind.Shot);
            if ((events & RunEvent.Crashed) != 0) Trigger(RumbleKind.Crash);
            if ((events & RunEvent.Fell) != 0) Trigger(RumbleKind.Fall);
            if ((events & RunEvent.RanOver) != 0) Trigger(RumbleKind.RanOver);
            if ((events & RunEvent.Explosion) != 0) Trigger(RumbleKind.Explosion);
        }

        public void Tick(float dt)
        {
            float low = 0f, high = 0f;
            for (int i = 0; i < KindCount; i++)
            {
                if (left[i] <= 0f)
                    continue;
                // Primero se descuenta el tiempo y despues se mide: Tick(0) da la fuerza entera.
                left[i] = Mathf.Max(0f, left[i] - dt);
                if (left[i] <= 0f)
                    continue;
                var e = config.Get((RumbleKind)i);
                float k = Mathf.Clamp01(left[i] / e.seconds);
                low = Mathf.Max(low, e.low * k);
                high = Mathf.Max(high, e.high * k);
            }
            Low = low;
            High = high;
        }

        public void Stop()
        {
            Array.Clear(left, 0, left.Length);
            Low = 0f;
            High = 0f;
        }
    }
}
