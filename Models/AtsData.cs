using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ATS_Desktop.Models;

public class ATSData
{
    [JsonPropertyName("consumers")]
    public List<Consumer> Consumers { get; set; } = new List<Consumer>();
    
    [JsonPropertyName("tariffs")]
    public List<TariffInfo> Tariffs { get; set; } = new List<TariffInfo>();
    
    [JsonPropertyName("tariffsMap")]
    public Dictionary<string, List<string>> TariffsMap { get; set; } = new Dictionary<string, List<string>>();
    
    [JsonPropertyName("consumersTariffs")]
    public Dictionary<string, List<KeyValuePair<string, int>>> ConsumersTariffs { get; set; } = new Dictionary<string, List<KeyValuePair<string, int>>>();
}
