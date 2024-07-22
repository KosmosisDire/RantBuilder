using System;
using System.Threading.Tasks;

namespace NodeBuilder.Internal;

public static class TaskUtils
{
    public static async void RunAfter(Action action, int milliseconds)
    {
        await Task.Delay(milliseconds);
        action();
    }

    public static int? TryParseInt(this string value)
    {
        if (int.TryParse(value, out var result))
        {
            return result;
        }
        return null;
    }

    public static float? TryParseFloat(this string value)
    {
        if (float.TryParse(value, out var result))
        {
            return result;
        }
        return null;
    }
}
