using Dalamud.Interface.Windowing;
using ImGuiNET;
using TTSPlogon.Clients;
using TTSPlogon.Clients.OpenAi;
using TTSPlogon.Clients.SpeechSynthesisClient;
using TTSPlogon.Providers;
using TTSPlogon.Utils;

namespace TTSPlogon;

public class MainWindow : Window, IDisposable
{
    private readonly PluginConfig _config;
    private readonly LexiconHandler _lexiconHandler;
    private readonly EventHandler _handler;

    public MainWindow(PluginConfig config, LexiconHandler lexiconHandler, EventHandler handler) : base("TTSPlogon")
    {
        _config = config;
        _lexiconHandler = lexiconHandler;
        _handler = handler;
    }

    private string testMsg = "test";
    public override void Draw()
    {
        ImGui.Text("TTSPlogon");

        // speed and volume sliders
        var speed = _config.BaseSpeed;
        if (ImGui.InputFloat("Speed", ref speed, 0.1f))
        {
            _config.BaseSpeed = speed;
            _config.Save();
        }
                
        var volume = _config.BaseVolume;
        if (ImGui.InputFloat("Volume", ref volume, 0.01f))
        {
            _config.BaseVolume = volume;
            _config.Save();
        }
        
        var beginWithSpeaker = _config.BeginFirstMessageWithSpeakerName;
        if (ImGui.Checkbox("Begin with speaker name", ref beginWithSpeaker))
        {
            _config.BeginFirstMessageWithSpeakerName = beginWithSpeaker;
            _config.Save();
        }
        
        var enabled = _config.Enabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
        {
            _config.Enabled = enabled;
            _config.Save();
        }

        if (ImGui.Button("Save"))
        {
            _config.Save();
        }
        
        ImGui.Separator();
        // dropdown for provider
        var provider = _config.TtsProvider;
        if (ImGui.BeginCombo("Provider", provider.ToString()))
        {
            foreach (var p in Enum.GetValues<PluginConfig.Provider>())
            {
                var selected = p == provider;
                if (ImGui.Selectable(p.ToString(), selected))
                {
                    _config.TtsProvider = p;
                    _config.Save();
                }
            }
            ImGui.EndCombo();
        }
        
        if (_config.TtsProvider == PluginConfig.Provider.OpenAi)
        {
            ImGui.Text("OpenAI provider selected");
            var openApiConfig = _config.OpenApiConfig;
            var key = openApiConfig.ApiKey ?? "";
            if (ImGui.InputText("##openai-api-key", ref key, 100, ImGuiInputTextFlags.Password))
            {
                openApiConfig.ApiKey = key;
                _config.Save();
            }
            
            if (ImGui.CollapsingHeader("Voices"))
            {
                var voiceCacheCopy = new Dictionary<string, string>(_config.OpenApiConfig.VoiceCache);
                foreach (var (entity, voice) in voiceCacheCopy)
                {
                    ImGui.PushID(entity);
                    if (ImGui.Button("Remove"))
                    {
                        _config.OpenApiConfig.VoiceCache.Remove(entity);
                        _config.Save();
                        ImGui.PopID();
                        continue;
                    }
                    ImGui.SameLine();
                    
                    if (ImGui.BeginCombo(entity, voice))
                    {
                        foreach (var v in OpenAiClient.Voices)
                        {
                            var selected = v == voice;
                            if (!ImGui.Selectable(v, selected)) continue;
                            _config.OpenApiConfig.VoiceCache[entity] = v;
                            _config.Save();
                        }

                        ImGui.EndCombo();
                    }

                    ImGui.PopID();
                }
            }
        }
        else if (_config.TtsProvider == PluginConfig.Provider.SpeechSynthesis)
        {
            ImGui.Text("SpeechSynthesis provider selected");
            if (ImGui.CollapsingHeader("Voices"))
            {
                var voiceCacheCopy = new Dictionary<string, string>(_config.SpeechSynthesisConfig.VoiceCache);
                foreach (var (entity, voice) in voiceCacheCopy)
                {
                    ImGui.PushID(entity);
                    if (ImGui.Button("Remove"))
                    {
                        _config.SpeechSynthesisConfig.VoiceCache.Remove(entity);
                        _config.Save();
                        ImGui.PopID();
                        continue;
                    }
                    ImGui.SameLine();

                    if (ImGui.BeginCombo(entity, voice))
                    {
                        foreach (var v in SpeechSynthesisClient.InstalledVoiceNames)
                        {
                            var selected = v == voice;
                            if (!ImGui.Selectable(v, selected)) continue;
                            _config.SpeechSynthesisConfig.VoiceCache[entity] = v;
                            _config.Save();
                        }

                        ImGui.EndCombo();
                    }

                    ImGui.PopID();
                }
            }
        }
        else
        {
            ImGui.Text("Unknown provider selected");
        }
        
        if (ImGui.CollapsingHeader("Lexicons"))
        {
            // menu to mess with lexicons
            var lexicons = _lexiconHandler.Lexicons.Select(x => x.Name).ToList();
            foreach (var lexicon in lexicons)
            {
                var lexiconEnabled = _config.EnabledLexicons.Contains(lexicon);
                if (ImGui.Checkbox(lexicon, ref lexiconEnabled))
                {
                    if (lexiconEnabled)
                    {
                        _config.EnabledLexicons.Add(lexicon);
                    }
                    else
                    {
                        _config.EnabledLexicons.Remove(lexicon);
                    }
                    _config.Save();
                }
            }
        }

        if (ImGui.CollapsingHeader("Test##testdd"))
        {
            ImGui.InputText("##test", ref testMsg, 100);
            if (ImGui.Button("Test"))
            {
                _handler.InvokeChatEvent(EventHandler.ChatSource.Custom, "", testMsg, null);
            }
        }
    }

    public void Dispose()
    {
        
    }
}