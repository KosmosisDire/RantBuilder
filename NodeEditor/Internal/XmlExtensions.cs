using System;
using System.Xml;

namespace NodeBuilder.Internal;

public static class XmlExtensions
{
    public static string? GetAttribute(this XmlNode element, string name)
    {
        return element.Attributes?[name]?.Value;
    }

    public static string GetString(this XmlNode element, string name)
    {
        return element.GetAttribute(name) ?? string.Empty;
    }

    public static float GetFloat(this XmlNode element, string name)
    {
        if (float.TryParse(element.GetAttribute(name), out var result))
        {
            return result;
        }
        return default;
    }

    public static int GetInt(this XmlNode element, string name)
    {
        if (int.TryParse(element.GetAttribute(name), out var result))
        {
            return result;
        }
        return default;
    }

    public static bool GetBool(this XmlNode element, string name, bool defaultValue = false)
    {
        if (bool.TryParse(element.GetAttribute(name), out var result))
        {
            return result;
        }
        return defaultValue;
    }

    public static T GetEnum<T>(this XmlNode element, string name, T defaultValue = default) where T : struct
    {
        if (Enum.TryParse(typeof(T), element.GetAttribute(name), out var result))
        {
            return (T)result;
        }
        return defaultValue;
    }
}