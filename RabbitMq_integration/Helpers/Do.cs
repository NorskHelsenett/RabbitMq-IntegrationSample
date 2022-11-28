using System.Reflection;

namespace RabbitMq_integration.Helpers;

public static class Do
{
    private static readonly HashSet<MethodInfo> MethodsAlreadyInvokedOnce = new();

    /// <summary>
    /// Do the specified action, unless it has already been done before.
    /// Used for logging things once.
    /// </summary>
    /// <param name="action"></param>
    public static void OnlyOnce(Action action)
    {
        if (!MethodsAlreadyInvokedOnce.Contains(action.Method))
        {
            MethodsAlreadyInvokedOnce.Add(action.Method);
            action();
        }
    }
}