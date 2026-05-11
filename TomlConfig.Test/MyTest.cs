using SimpleTomlConfig;

namespace TomlConfig.Test;

[TomlSection("AppConfigure")]
public class MyTest : TomlConfig<MyTest>
{
    [TomlProperty]
    public string Guid { get; set; } = "EEEE";

    [TomlIgnore]
    public string RuntimeOnlyValue { get; set; } = "IgnoreMe";

    public int Age { get; set; }

    public HumanType HumanType { get; set; } = HumanType.White;

    [TomlSection("Hardware")]
    public HardwareConfig Hardware { get; set; } = new();
}

public enum HumanType
{
    Black,
    White,
    Yellow,
}

public class HardwareConfig
{
    [TomlProperty]
    public string ComPort { get; set; } = "COM1";
}
