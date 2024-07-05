using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace TTSPlogon;

public class EventHandler : IDisposable
{
    private readonly EventProvider _eventProvider;
    private readonly OpenAiClient _client;
    private readonly PluginConfig _config;
    private readonly IPluginLog _log;

    public EventHandler(EventProvider eventProvider, OpenAiClient client, PluginConfig config, IPluginLog log)
    {
        _eventProvider = eventProvider;
        _client = client;
        _config = config;
        _log = log;
        _eventProvider.ChatEvent += OnChat;
    }

    private void OnChat(string entity, string text)
    {
        var cleanText = TextUtils.CleanText(text);
        if (string.IsNullOrWhiteSpace(cleanText))
        {
            _log.Warning($"Cleaned text is empty: {text}");
            return;
        }
        
        var openApiConfig = _config.OpenApiConfig;
        var speed = openApiConfig.BaseSpeed;
        var volume = openApiConfig.BaseVolume;
        
        var voice = voiceCache.GetValueOrDefault(entity, "alloy");
        
        var _ = _client.Say(voice, speed, volume, cleanText);
    }
    
    // entity-name -> voice cache
    private Dictionary<string, string> voiceCache = new();
    
    private List<string> feminineVoices = new()
    {
        "alloy",
        "fable",
        "nova",
        "shimmer"
    };
    
    private List<string> masculineVoices = new()
    {
        "echo",
        "onyx"
    };

    public void Dispose()
    {
        _eventProvider.ChatEvent -= OnChat;
    }
}