namespace TomlConfig;

/// <summary>
/// TOML 로드/저장 대상에서 제외합니다.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class TomlIgnoreAttribute : Attribute;
