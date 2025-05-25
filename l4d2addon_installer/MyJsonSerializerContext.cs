using System.Text.Json.Serialization;
using l4d2addon_installer.Models;

// ReSharper disable once CheckNamespace
namespace l4d2addon_installer.Json.Context;

[JsonSerializable(typeof(AppConfig))]
public partial class MyJsonSerializerContext : JsonSerializerContext
{
}
