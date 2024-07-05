using Dalamud.Configuration;
using Dalamud.Plugin;

namespace TTSPlogon;

public class PluginConfig : IPluginConfiguration
{
    private readonly IDalamudPluginInterface _pluginInterface;
    public int Version { get; set; }
    public PluginConfig(IDalamudPluginInterface pluginInterface)
    {
        _pluginInterface = pluginInterface;
        Version = 0;
    }
    
    public OpenApiConfig OpenApiConfig { get; set; } = new();
    
    public void Save()
    {
        _pluginInterface.SavePluginConfig(this);
    }
}