using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

/// <summary>
/// 统一的日志工具，自动识别并格式化常见数据类型（原生类型、Unity 结构、IEnumerable、Struct 等）。
/// </summary>
internal static class Logger
{

    public static void Log(object payload, string tag = null, UnityEngine.Object context = null, bool prettyPrint = false)
    {
        Debug.Log(BuildMessage(LogLevel.Info, tag, payload, prettyPrint), context);
    }

    public static void LogWarning(object payload, string tag = null, UnityEngine.Object context = null, bool prettyPrint = false)
    {
        Debug.LogWarning(BuildMessage(LogLevel.Warning, tag, payload, prettyPrint), context);
    }

    public static void LogError(object payload, string tag = null, UnityEngine.Object context = null, bool prettyPrint = false)
    {
        Debug.LogError(BuildMessage(LogLevel.Error, tag, payload, prettyPrint), context);
    }

    private static string BuildMessage(LogLevel level, string tagOverride, object payload, bool prettyPrint)
    {
        var builder = new StringBuilder(256);
        builder.Append('[').Append(level).Append(']');

        if (!string.IsNullOrWhiteSpace(tagOverride))
        {
            builder.Append('[').Append(tagOverride).Append(']');
        }
        else if (payload != null)
        {
            builder.Append('[').Append(payload.GetType().Name).Append(']');
        }

        builder.Append(' ').Append(Serialize(payload, prettyPrint, 0, new HashSet<object>(ReferenceEqualityComparer.Instance)));
        return builder.ToString();
    }

    private static string Serialize(object payload, bool prettyPrint, int depth, HashSet<object> visited)
    {
        if (payload == null)
        {
            return "null";
        }

        if (depth > 4)
        {
            return "<MaxDepthReached>";
        }

        var type = payload.GetType();

        if (IsSimple(type))
        {
            return FormatSimple(payload);
        }

        if (payload is UnityEngine.Object unityObject)
        {
            return unityObject == null ? "null" : $"{unityObject.name} ({type.Name})";
        }

        if (!type.IsValueType)
        {
            if (!visited.Add(payload))
            {
                return $"<Cyclic:{type.Name}>";
            }
        }

        if (payload is IDictionary dictionary)
        {
            return SerializeDictionary(dictionary, prettyPrint, depth, visited);
        }

        if (payload is IEnumerable enumerable)
        {
            return SerializeEnumerable(enumerable, prettyPrint, depth, visited);
        }

        var json = SerializeUsingJsonUtility(payload, prettyPrint);
        if (!string.IsNullOrEmpty(json) && json != "{}")
        {
            return json;
        }

        return SerializeWithReflection(payload, prettyPrint, depth, visited);
    }

    private static string SerializeDictionary(IDictionary dictionary, bool prettyPrint, int depth, HashSet<object> visited)
    {
        var builder = new StringBuilder(128);
        var separator = prettyPrint ? ",\n" : ", ";
        var indent = new string(' ', (depth + 1) * 2);
        var newline = prettyPrint ? "\n" : string.Empty;

        builder.Append('{');
        if (prettyPrint)
        {
            builder.Append(newline);
        }
        else
        {
            builder.Append(' ');
        }

        int index = 0;
        foreach (DictionaryEntry entry in dictionary)
        {
            builder.Append(indent)
                   .Append(Serialize(entry.Key, prettyPrint, depth + 1, visited))
                   .Append(prettyPrint ? ": " : ":")
                   .Append(Serialize(entry.Value, prettyPrint, depth + 1, visited));

            if (index < dictionary.Count - 1)
            {
                builder.Append(separator);
            }

            index++;
        }

        if (prettyPrint)
        {
            builder.Append('\n').Append(new string(' ', depth * 2));
        }
        else
        {
            builder.Append(' ');
        }

        builder.Append('}');
        return builder.ToString();
    }

    private static string SerializeEnumerable(IEnumerable enumerable, bool prettyPrint, int depth, HashSet<object> visited)
    {
        var builder = new StringBuilder(128);
        var separator = prettyPrint ? ",\n" : ", ";
        var indent = new string(' ', (depth + 1) * 2);
        var newline = prettyPrint ? "\n" : string.Empty;

        builder.Append('[');
        if (prettyPrint)
        {
            builder.Append(newline);
        }
        else
        {
            builder.Append(' ');
        }

        bool isFirst = true;
        foreach (var element in enumerable)
        {
            if (!isFirst)
            {
                builder.Append(separator);
            }

            builder.Append(indent)
                   .Append(Serialize(element, prettyPrint, depth + 1, visited));

            isFirst = false;
        }

        if (prettyPrint)
        {
            builder.Append('\n').Append(new string(' ', depth * 2));
        }
        else
        {
            builder.Append(' ');
        }

        builder.Append(']');
        return builder.ToString();
    }

    private static string SerializeUsingJsonUtility(object payload, bool prettyPrint)
    {
        try
        {
            return JsonUtility.ToJson(payload, prettyPrint);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string SerializeWithReflection(object payload, bool prettyPrint, int depth, HashSet<object> visited)
    {
        var type = payload.GetType();
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (fields.Length == 0)
        {
            return payload.ToString();
        }

        var builder = new StringBuilder(128);
        var separator = prettyPrint ? ",\n" : ", ";
        var indent = new string(' ', (depth + 1) * 2);
        var newline = prettyPrint ? "\n" : string.Empty;

        builder.Append('{');
        if (prettyPrint)
        {
            builder.Append(newline);
        }
        else
        {
            builder.Append(' ');
        }

        for (int i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            var value = field.GetValue(payload);

            builder.Append(indent)
                   .Append(field.Name)
                   .Append(prettyPrint ? ": " : ":")
                   .Append(Serialize(value, prettyPrint, depth + 1, visited));

            if (i < fields.Length - 1)
            {
                builder.Append(separator);
            }
        }

        if (prettyPrint)
        {
            builder.Append('\n').Append(new string(' ', depth * 2));
        }
        else
        {
            builder.Append(' ');
        }

        builder.Append('}');
        return builder.ToString();
    }

    private static bool IsSimple(Type type)
    {
        return type.IsPrimitive
               || type.IsEnum
               || type == typeof(string)
               || type == typeof(decimal)
               || type == typeof(double)
               || type == typeof(float)
               || type == typeof(Vector2)
               || type == typeof(Vector3)
               || type == typeof(Vector4)
               || type == typeof(Quaternion)
               || type == typeof(Color)
               || type == typeof(Vector2Int)
               || type == typeof(Vector3Int);
    }

    private static string FormatSimple(object value)
    {
        switch (value)
        {
            case null:
                return "null";
            case float f:
                return f.ToString("F3");
            case double d:
                return d.ToString("F3");
            case decimal m:
                return m.ToString("F3");
            case Vector2 v2:
                return $"({v2.x:F3}, {v2.y:F3})";
            case Vector3 v3:
                return $"({v3.x:F3}, {v3.y:F3}, {v3.z:F3})";
            case Vector4 v4:
                return $"({v4.x:F3}, {v4.y:F3}, {v4.z:F3}, {v4.w:F3})";
            case Quaternion q:
                return $"({q.x:F3}, {q.y:F3}, {q.z:F3}, {q.w:F3})";
            case Color color:
                return $"RGBA({color.r:F2}, {color.g:F2}, {color.b:F2}, {color.a:F2})";
            default:
                return value.ToString();
        }
    }

    private enum LogLevel
    {
        Info,
        Warning,
        Error
    }

    private readonly struct StructWithMeta<TStruct>
        where TStruct : struct
    {
        public StructWithMeta(TStruct payload, MetaStruct meta)
        {
            Payload = payload;
            Meta = meta;
        }

        public TStruct Payload { get; }
        public MetaStruct Meta { get; }
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        internal static readonly ReferenceEqualityComparer Instance = new();

        public new bool Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(object obj)
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
