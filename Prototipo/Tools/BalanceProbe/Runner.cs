using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public static class Runner
{
    public static int Main()
    {
        var type = typeof(Zombineta.Tests.RunSimulationTests);
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                          .Where(m => m.GetCustomAttribute<TestAttribute>() != null)
                          .OrderBy(m => m.Name)
                          .ToList();

        int passed = 0, failed = 0;

        foreach (var m in methods)
        {
            var instance = Activator.CreateInstance(type);
            try
            {
                m.Invoke(instance, null);
                Console.WriteLine($"  PASS  {m.Name}");
                passed++;
            }
            catch (TargetInvocationException ex)
            {
                Console.WriteLine($"  FAIL  {m.Name}");
                Console.WriteLine($"        {ex.InnerException?.Message}");
                failed++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"{passed} pasaron, {failed} fallaron, {methods.Count} en total");
        BalanceProbe.Report();
        return failed == 0 ? 0 : 1;
    }
}
