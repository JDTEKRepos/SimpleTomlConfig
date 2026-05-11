# SimpleTomlConfig

SimpleTomlConfig는 TOML 형식의 설정 파일을 쉽게 관리할 수 있게 해주는 제네릭 클래스입니다.

## 특징

- TOML 파일을 자동으로 로드하고 저장합니다.
- 설정 파일이 없을 경우 기본 설정으로 새 파일을 생성합니다.
- 제네릭을 사용하여 다양한 설정 클래스에 적용할 수 있습니다.

## 주의사항

- 설정 파일이 존재하지 않으면 기본값으로 새 파일이 생성됩니다.
- Save() 메서드를 호출하지 않으면 변경사항이 파일에 저장되지 않습니다.
- TOML 파일의 키 이름은 프로퍼티 이름과 일치해야 합니다.
- 다른 키 이름을 사용하려면 `[TomlProperty("key")]`를 사용합니다.
- 이름을 생략한 `[TomlProperty]`, `[TomlSection]`는 class/property 이름을 사용합니다.
- 객체를 TOML section으로 저장하려면 `[TomlSection("sectionName")]` 또는 `[TomlSection]`를 사용합니다.
- TOML 로드/저장에서 제외하려면 `[TomlIgnore]`를 사용합니다.
- TomlConfig<TClass>를 상속받은 객체의 설정파일 이름은 configPath를 설정하지 않은 경우 `{TClass}.toml`입니다.

## 사용 방법

### 1. 설정 클래스 정의

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

    [TomlSection("device")]
    public DeviceConfig Device { get; set; } = new();
}

public class DeviceConfig
{
    [TomlProperty]
    public string Port { get; set; } = "COM1";
}
```

### 2. 설정 객체 생성

```csharp
var config = new MyConfig();
var customConfig = new MyConfig("path/to/custom/config.toml");
```

### 3. 설정 값 변경 및 저장

```csharp
config.Name = "Mr.Kim";
config.Save();
```

### 4. 설정 값 다시 불러오기

```csharp
config.Load();
```

### 5. 다른 경로로 설정 저장

```csharp
config.SaveAs("path/to/new/config.toml");
```
