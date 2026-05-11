# SimpleTomlConfig

SimpleTomlConfig is a small generic helper for managing TOML configuration files in .NET applications.

## Features

- Automatically loads and saves TOML files.
- Creates a new configuration file with default values when the file does not exist.
- Uses a generic base class so the same pattern can be reused across many configuration types.

## Notes

- If the configuration file does not exist, it is created from the current default property values.
- Changes are not written to disk until `Save()` is called.
- By default, TOML keys use the class or property name.
- Use `[TomlProperty("key")]` to map a property to a different TOML key.
- Use `[TomlProperty]` or `[TomlSection]` without a name to use the class or property name as-is.
- Use `[TomlSection("sectionName")]` or `[TomlSection]` to save an object as a TOML section/table.
- Use `[TomlIgnore]` to exclude a property from both loading and saving.
- If `configPath` is not provided, a class that inherits `TomlConfig<TClass>` uses `{TClass}.toml` as the default file name.

## Usage

### 1. Define a Configuration Class

```csharp
using SimpleTomlConfig;

[TomlSection]
public class MyConfig(string? configPath = null) : TomlConfig<MyConfig>(configPath)
{
    [TomlProperty]
    public string? Name { get; set; }

    [TomlProperty("age")]
    public int Age { get; set; }

    [TomlIgnore]
    public string RuntimeOnlyValue { get; set; } = "";

    [TomlProperty("myType")]
    public MyEnum MyType { get; set; }

    [TomlSection("Device")]
    public DeviceConfig Device { get; set; } = new();
}

public class DeviceConfig
{
    public string Port { get; set; } = "COM1";
}
```

This produces TOML similar to:

```toml
[MyConfig]
Name = "Camera"
age = 3
myType = "Default"

[MyConfig.Device]
Port = "COM1"
```

### 2. Create a Configuration Instance

```csharp
var config = new MyConfig();
var customConfig = new MyConfig("path/to/custom/config.toml");
```

### 3. Read Values

```csharp
Console.WriteLine(config.Name);
Console.WriteLine(config.Device.Port);
```

### 4. Change and Save Values

```csharp
config.Name = "Mr.Kim";
config.Device.Port = "COM3";
config.Save();
```

### 5. Reload Values

```csharp
config.Load();
```

### 6. Save to Another Path

```csharp
config.SaveAs("path/to/new/config.toml");
```
