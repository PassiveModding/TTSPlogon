using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using TTSPlogon.Clients;
using TTSPlogon.Utils;

namespace TTSPlogon;

public class EventHandler : IDisposable
{
    private readonly IEnumerable<ISpeechClient> _speechClients;
    private readonly PluginConfig _config;
    private readonly IPluginLog _log;
    
    public void InvokeChatEvent(ChatSource source, string entity, string text, IGameObject? speaker)
    {
        ChatEvent?.Invoke(source, entity, text, speaker);
    }
    public event Action<ChatSource, string, string, IGameObject?>? ChatEvent;

    public EventHandler(IEnumerable<ISpeechClient> speechClients, PluginConfig config, IPluginLog log)
    {
        _speechClients = speechClients;
        _config = config;
        _log = log;
        ChatEvent += OnChat;
    }

    public DateTime? VoicedLineTime { get; set; }

    public enum ChatSource
    {
        TalkAddon,
        ChatLog,
        Custom
    }
    
    private void OnChat(ChatSource source, string entity, string text, IGameObject? speaker)
    {
        var client = _speechClients.FirstOrDefault(x => x.Provider == _config.TtsProvider);
        if (client == null)
        {
            _log.Error("No TTS client found");
            return;
        }
        
        // check if voiced line
        if (source == ChatSource.TalkAddon && VoicedLineTime.HasValue && DateTime.Now - VoicedLineTime.Value < TimeSpan.FromSeconds(1))
        {
            _log.Info($"Skipping spoken line: {entity} - {text}");
            return;
        }
        
        var cleanText = TextUtils.CleanText(text);
        if (string.IsNullOrWhiteSpace(cleanText))
        {
            _log.Warning($"Cleaned text is empty: {text}");
            return;
        } 
        
        var speed = _config.BaseSpeed;
        var volume = _config.BaseVolume;

        if (_config.Enabled == false)
        {
            _log.Debug($"Skipping line: {entity} - {cleanText}");
            return;
        }
        
        var speakerGender = GenderUtils.GetCharacterGender(speaker);
        _log.Debug($"Queueing line: {entity} - {cleanText}");
        var _ = Task.Run(() => client.Say(new ActorInfo(entity, speakerGender), cleanText, speed, volume));
    }

    public record ActorInfo(string Name, GenderUtils.Gender Gender);

    public void Dispose()
    {
        ChatEvent -= OnChat;
    }
}