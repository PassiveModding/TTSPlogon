namespace TTSPlogon.Clients.OpenAi;

public class OpenApiConfig
{
    public string? ApiKey { get; set; }
    
    public Dictionary<string, string> VoiceCache { get; set; } = new();
    public string GetSpeakerVoiceOrDefault(string entity, string defaultVoice)
    {
        if (VoiceCache.TryGetValue(entity, out var voice))
        {
            return voice;
        }
        
        VoiceCache[entity] = defaultVoice;
        return defaultVoice;
    }
}