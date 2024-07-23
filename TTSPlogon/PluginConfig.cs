using Dalamud.Configuration;
using Dalamud.Plugin;
using TTSPlogon.Clients;
using TTSPlogon.Clients.OpenAi;
using TTSPlogon.Clients.SpeechSynthesisClient;

namespace TTSPlogon;

public class PluginConfig : IPluginConfiguration
{
    public static IDalamudPluginInterface? PluginInterface;
    public int Version { get; set; }
    public PluginConfig()
    {
        Version = 0;
    }

    public enum Provider
    {
        OpenAi,
        SpeechSynthesis
    }
    public Provider TtsProvider { get; set; } = Provider.SpeechSynthesis;
    public OpenApiConfig OpenApiConfig { get; set; } = new();
    public SpeechSynthesisConfig SpeechSynthesisConfig { get; set; } = new();
    public float BaseSpeed { get; set; } = 1.0f; // multiplied with specific voice speed
    public float BaseVolume { get; set; } = 0.5f; // multiplied with specific voice volume
    public bool Enabled { get; set; } = false;
    public bool BeginFirstMessageWithSpeakerName { get; set; } = false;
    public HashSet<string> EnabledLexicons { get; set; } = [];
    
    public void Save()
    {
        PluginInterface?.SavePluginConfig(this);
    }
}