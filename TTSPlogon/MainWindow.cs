using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace TTSPlogon;

public class MainWindow : Window, IDisposable
{
    private readonly PluginConfig _config;
    private readonly EventProvider _eventProvider;

    public MainWindow(PluginConfig config, EventProvider eventProvider) : base("TTSPlogon")
    {
        _config = config;
        _eventProvider = eventProvider;
    }

    public override void Draw()
    {
        ImGui.Text("TTSPlogon");
            
        var openApiConfig = _config.OpenApiConfig;
        var key = openApiConfig.ApiKey ?? "";
        if (ImGui.InputText("##openai-api-key", ref key, 100))
        {
            openApiConfig.ApiKey = key;
            _config.Save();
        }
            
        // speed and volume sliders
        var speed = openApiConfig.BaseSpeed;
        if (ImGui.InputFloat("Speed", ref speed, 0.1f))
        {
            openApiConfig.BaseSpeed = speed;
            _config.Save();
        }
            
        var volume = openApiConfig.BaseVolume;
        if (ImGui.InputFloat("Volume", ref volume, 0.01f))
        {
            openApiConfig.BaseVolume = volume;
            _config.Save();
        }

        if (ImGui.Button("Save"))
        {
            _config.Save();
        }
        
        if (ImGui.Button("Queue"))
        {
            _eventProvider.CustomMessage("Hello, world!");
        }
    }

    public void Dispose()
    {
        
    }
}