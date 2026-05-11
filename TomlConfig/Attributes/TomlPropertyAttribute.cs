namespace TomlConfig;

/// <summary>
/// TOML property 이름을 지정합니다.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class TomlPropertyAttribute(string? name = null) : Attribute
{
    public string? Name { get; } = name;
}
