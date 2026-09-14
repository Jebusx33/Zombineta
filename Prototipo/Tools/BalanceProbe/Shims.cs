// Shims minimos para correr la simulacion real de Zombineta fuera de Unity.
// Solo replican las APIs que RunSimulation y sus tests usan de verdad.
using System;

namespace UnityEngine
{
    public static class Mathf
    {
        public static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
        public static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
        public static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);
        public static float Max(float a, float b) => a > b ? a : b;
        public static float Min(float a, float b) => a < b ? a : b;
        public static int Min(int a, int b) => a < b ? a : b;
        public static int RoundToInt(float v) => (int)MathF.Round(v, MidpointRounding.ToEven);

        public static float MoveTowards(float current, float target, float maxDelta)
        {
            if (MathF.Abs(target - current) <= maxDelta) return target;
            return current + MathF.Sign(target - current) * maxDelta;
        }
    }

    public class ScriptableObject
    {
        public static T CreateInstance<T>() where T : ScriptableObject
            => (T)Activator.CreateInstance(typeof(T));
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CreateAssetMenuAttribute : Attribute
    {
        public string menuName;
        public string fileName;
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class HeaderAttribute : Attribute
    {
        public HeaderAttribute(string header) { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TooltipAttribute : Attribute
    {
        public TooltipAttribute(string tooltip) { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class RangeAttribute : Attribute
    {
        public RangeAttribute(float min, float max) { }
    }
}

namespace NUnit.Framework
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestAttribute : Attribute { }

    public class AssertionException : Exception
    {
        public AssertionException(string m) : base(m) { }
    }

    public static class Assert
    {
        static string Tail(string m) => string.IsNullOrEmpty(m) ? "" : "  -- " + m;

        public static void AreEqual(object expected, object actual, string message = null)
        {
            if (!Equals(expected, actual))
                throw new AssertionException(
                    $"esperaba <{expected}> pero fue <{actual}>{Tail(message)}");
        }

        public static void AreEqual(double expected, double actual, double delta,
                                    string message = null)
        {
            if (Math.Abs(expected - actual) > delta)
                throw new AssertionException(
                    $"esperaba <{expected}> +/- {delta} pero fue <{actual}>{Tail(message)}");
        }

        public static void AreNotEqual(object expected, object actual, string message = null)
        {
            if (Equals(expected, actual))
                throw new AssertionException($"no esperaba <{expected}>{Tail(message)}");
        }

        public static void Greater(double a, double b, string message = null)
        {
            if (!(a > b))
                throw new AssertionException($"esperaba {a} > {b}{Tail(message)}");
        }

        public static void Less(double a, double b, string message = null)
        {
            if (!(a < b))
                throw new AssertionException($"esperaba {a} < {b}{Tail(message)}");
        }

        public static void IsTrue(bool c, string message = null)
        {
            if (!c) throw new AssertionException($"esperaba true{Tail(message)}");
        }

        public static void IsFalse(bool c, string message = null)
        {
            if (c) throw new AssertionException($"esperaba false{Tail(message)}");
        }
    }
}
