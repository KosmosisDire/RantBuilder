using System.Numerics;
using Avalonia;

namespace NodeBuilder.Internal;

public static class AvaloniaExtensions
{
    public static Vector2 ToVector2(this Point point) => new Vector2((float)point.X, (float)point.Y);
}
