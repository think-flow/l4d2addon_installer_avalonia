using l4d2addon_installer.Models;
using YamlDotNet.Serialization;

namespace l4d2addon_installer;

[YamlStaticContext]
[YamlSerializable(typeof(AppConfig))]
public partial class MyYamlSerializerContext
{
}
