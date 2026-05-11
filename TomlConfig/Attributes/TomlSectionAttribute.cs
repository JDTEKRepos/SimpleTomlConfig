namespace TomlConfig;

/// <summary>
/// TOML section/table 이름을 지정합니다.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public sealed class TomlSectionAttribute(string? name = null) : Attribute
{
    public string? Name { get; } = name;
}
