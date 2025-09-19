using Newtonsoft.Json;

namespace ChessAI.Models
{
    /// <summary>
    /// Model class to represent the structure of keys.json file
    /// </summary>
    public class KeysModel
    {
        [JsonProperty("Anthropics")]
        public string Anthropics { get; set; } = string.Empty;
    }
}