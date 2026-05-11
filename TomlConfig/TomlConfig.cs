using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;
using TomlConfig;
using Tomlet;
using Tomlet.Attributes;
using Tomlet.Models;

namespace SimpleTomlConfig;

public class TomlConfig<TClass> where TClass : class
{
    private readonly string _configPath;

    [TomlIgnore]
    public string TomlPath => _configPath;

    /// <summary>
    /// TomlConfig 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="configPath">
    /// TOML 설정 파일의 경로입니다.
    /// null이면 기본 경로 "{ClassName}.toml"가 사용됩니다.
    /// </param>
    protected TomlConfig(string? configPath = null)
    {
        _configPath = configPath ?? $"{GetType().Name}.toml";
        _configPath = Path.GetFullPath(_configPath);

        Initialize();
    }

    private void Initialize()
    {
        EnsureDirectoryExists(_configPath);
        if (!File.Exists(_configPath))
        {
            CreateDefaultConfig();
        }

        LoadConfig();
    }

    /// <summary>
    /// 현재 TOML 파일의 내용을 읽어와 인스턴스의 프로퍼티 값을 갱신합니다.
    /// </summary>
    public void Load() => LoadConfig();

    /// <summary>
    /// 현재 인스턴스의 프로퍼티 값을 TOML 형식으로 기존 설정 파일에 저장합니다.
    /// </summary>
    public void Save() => SaveAs(_configPath);

    /// <summary>
    /// 현재 인스턴스의 프로퍼티 값을 TOML 형식으로 지정된 경로에 저장합니다.
    /// </summary>
    /// <param name="path">TOML 파일을 저장할 경로. 경로가 존재하지 않으면 자동으로 생성됩니다.</param>
    public void SaveAs(string path)
    {
        EnsureDirectoryExists(path);
        File.WriteAllText(path, SerializeObject(this));
    }

    private static void EnsureDirectoryExists(string path)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private void CreateDefaultConfig() => File.WriteAllText(_configPath, SerializeObject(this));

    private void LoadConfig()
    {
        if (!File.Exists(_configPath)) return;

        var rootNode = TomlParser.ParseFile(_configPath);
        var rootSection = GetTomlSectionName(GetType());

        if (!string.IsNullOrWhiteSpace(rootSection))
        {
            if (rootNode.TryGetValue(rootSection, out var sectionNode) && sectionNode is TomlTable sectionTable)
            {
                ApplyTomlTable(sectionTable, this);
            }

            return;
        }

        ApplyTomlTable(rootNode, this);
    }

    private static void ApplyTomlTable(TomlTable table, object target)
    {
        foreach (var prop in GetSerializableProperties(target.GetType()))
        {
            if (!prop.CanWrite)
            {
                continue;
            }

            var keyToSearch = GetTomlKey(prop);
            if (!table.TryGetValue(keyToSearch, out var tomlNode) || tomlNode is null)
            {
                continue;
            }

            var value = ConvertTomlNode(tomlNode, prop.PropertyType);
            prop.SetValue(target, value);
        }
    }

    private static object? ConvertTomlNode(TomlValue node, Type targetType)
    {
        var type = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (IsSimpleType(type) || type.IsArray || typeof(IEnumerable).IsAssignableFrom(type))
        {
            return TomletMain.To(targetType, node);
        }

        if (node is TomlTable table)
        {
            var instance = Activator.CreateInstance(type);
            if (instance is null)
            {
                return null;
            }

            ApplyTomlTable(table, instance);
            return instance;
        }

        return TomletMain.To(targetType, node);
    }

    private static string SerializeObject(object obj)
    {
        var builder = new StringBuilder();
        var rootSection = GetTomlSectionName(obj.GetType());
        WriteObject(builder, obj, rootSection);
        return builder.ToString();
    }

    private static void WriteObject(StringBuilder builder, object obj, string? sectionPath)
    {
        if (!string.IsNullOrWhiteSpace(sectionPath))
        {
            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append('[').Append(sectionPath).AppendLine("]");
        }

        var properties = GetSerializableProperties(obj.GetType()).Where(prop => prop.CanRead).ToArray();

        foreach (var prop in properties.Where(prop => prop.GetCustomAttribute<TomlSectionAttribute>() is null))
        {
            var value = prop.GetValue(obj);
            if (value is null)
            {
                continue;
            }

            builder
                .Append(GetTomlKey(prop))
                .Append(" = ")
                .AppendLine(FormatTomlValue(value));
        }

        foreach (var prop in properties.Where(prop => prop.GetCustomAttribute<TomlSectionAttribute>() is not null))
        {
            var value = prop.GetValue(obj);
            if (value is null)
            {
                continue;
            }

            var sectionName = GetTomlSectionName(prop);
            var childPath = string.IsNullOrWhiteSpace(sectionPath)
                ? sectionName
                : $"{sectionPath}.{sectionName}";

            WriteObject(builder, value, childPath);
        }
    }

    private static string FormatTomlValue(object value)
    {
        var type = value.GetType();

        if (value is string stringValue)
        {
            return $"\"{EscapeString(stringValue)}\"";
        }

        if (value is char charValue)
        {
            return $"\"{EscapeString(charValue.ToString())}\"";
        }

        if (value is bool boolValue)
        {
            return boolValue ? "true" : "false";
        }

        if (type.IsEnum)
        {
            return $"\"{value}\"";
        }

        if (value is IFormattable formattable && IsNumber(type))
        {
            return formattable.ToString(null, CultureInfo.InvariantCulture);
        }

        if (value is IEnumerable enumerable && value is not string)
        {
            var items = enumerable.Cast<object>().Select(FormatTomlValue);
            return $"[{string.Join(", ", items)}]";
        }

        return FormatInlineTable(value);
    }

    private static string FormatInlineTable(object value)
    {
        var entries = GetSerializableProperties(value.GetType())
            .Where(prop => prop.CanRead && prop.GetCustomAttribute<TomlSectionAttribute>() is null)
            .Select(prop => new { Key = GetTomlKey(prop), Value = prop.GetValue(value) })
            .Where(entry => entry.Value is not null)
            .Select(entry => $"{entry.Key} = {FormatTomlValue(entry.Value!)}");

        return $"{{ {string.Join(", ", entries)} }}";
    }

    private static IEnumerable<PropertyInfo> GetSerializableProperties(Type type)
        => type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(prop => prop.GetCustomAttribute<TomlIgnoreAttribute>() is null)
            .Where(prop => prop.GetCustomAttribute<TomlNonSerializedAttribute>() is null);

    private static string GetTomlKey(PropertyInfo prop)
        => GetTomlSectionName(prop)
           ?? GetTomlPropertyName(prop)
           ?? prop.Name;

    private static string? GetTomlSectionName(Type type)
    {
        var attribute = type.GetCustomAttribute<TomlSectionAttribute>();
        if (attribute is null)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(attribute.Name) ? type.Name : attribute.Name;
    }

    private static string? GetTomlSectionName(PropertyInfo prop)
    {
        var attribute = prop.GetCustomAttribute<TomlSectionAttribute>();
        if (attribute is null)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(attribute.Name) ? prop.Name : attribute.Name;
    }

    private static string? GetTomlPropertyName(PropertyInfo prop)
    {
        var attribute = prop.GetCustomAttribute<TomlConfig.TomlPropertyAttribute>();
        if (attribute is null)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(attribute.Name) ? prop.Name : attribute.Name;
    }

    private static bool IsSimpleType(Type type)
        => type.IsPrimitive
           || type.IsEnum
           || type == typeof(string)
           || type == typeof(decimal)
           || type == typeof(DateTime)
           || type == typeof(DateTimeOffset)
           || type == typeof(TimeSpan)
           || type == typeof(Guid);

    private static bool IsNumber(Type type)
        => type == typeof(byte)
           || type == typeof(sbyte)
           || type == typeof(short)
           || type == typeof(ushort)
           || type == typeof(int)
           || type == typeof(uint)
           || type == typeof(long)
           || type == typeof(ulong)
           || type == typeof(float)
           || type == typeof(double)
           || type == typeof(decimal);

    private static string EscapeString(string value)
        => value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");

    /// <summary>
    /// 주어진 객체를 TOML 문자열로 직렬화합니다.
    /// </summary>
    /// <param name="obj">직렬화할 객체</param>
    /// <returns>TOML 형식의 문자열</returns>
    public static string Serializer(object obj) => SerializeObject(obj);
}
