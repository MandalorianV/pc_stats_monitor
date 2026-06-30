using System.Text.Json.Serialization;
using Monitor.Core.Models;

namespace Monitor.Core
{
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified)]
    [JsonSerializable(typeof(PcStatsDto))]
    public partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}