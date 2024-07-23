namespace TTSPlogon.Clients;

public interface ISpeechClient
{
    public PluginConfig.Provider Provider { get; }
    public Task Say(EventHandler.ActorInfo? actor, string text, float speed, float volume);
}