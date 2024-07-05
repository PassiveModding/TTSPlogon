using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;

namespace TTSPlogon;

public sealed class Plugin : IDalamudPlugin
{
    private static readonly WindowSystem WindowSystem = new("TTSPlogon");
    private ServiceProvider serviceProvider;
    private Service services = new();
    public class Service
    {
        [PluginService] public IFramework Framework { get; set; } = null!;
        [PluginService] public IDalamudPluginInterface PluginInterface { get; set; } = null!;
        [PluginService] public ICommandManager CommandManager { get; set; } = null!;
        [PluginService] public IPluginLog Log { get; set; } = null!;
        [PluginService] public IChatGui ChatGui { get; set; } = null!;
        [PluginService] public IGameGui GameGui { get; set; } = null!;
        [PluginService] public IClientState ClientState { get; set; } = null!;
        
        public void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton(Framework);
            services.AddSingleton(PluginInterface);
            services.AddSingleton(CommandManager);
            services.AddSingleton(Log);
            services.AddSingleton(ChatGui);
            services.AddSingleton(GameGui);
            services.AddSingleton(ClientState);
            
        }
    }

    

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        var config = pluginInterface.GetPluginConfig() as PluginConfig ?? new PluginConfig(pluginInterface);
        
        pluginInterface.Inject(services);
        
        var serviceCollection = new ServiceCollection()
            .AddSingleton<MainWindow>()
            .AddSingleton<SoundQueue>()
            .AddSingleton<HttpClient>()
            .AddSingleton<OpenAiClient>()
            .AddSingleton<PluginConfig>()
            .AddSingleton<EventHandler>()
            .AddSingleton<EventProvider>()
            .AddSingleton<QueueProcessor>()
            .AddSingleton(config);
        services.RegisterServices(serviceCollection);
        serviceProvider = serviceCollection.BuildServiceProvider();
        
        // lazy init
        var qp = serviceProvider.GetRequiredService<QueueProcessor>();
        var ep = serviceProvider.GetRequiredService<EventProvider>();
        var eh = serviceProvider.GetRequiredService<EventHandler>();
        
        services.CommandManager.AddHandler("/ttsplogon", new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the menu"
        });
        
        WindowSystem.AddWindow(serviceProvider.GetRequiredService<MainWindow>());

        services.PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        services.PluginInterface.UiBuilder.OpenMainUi += OpenUi;
        services.PluginInterface.UiBuilder.OpenConfigUi += OpenUi;
    }

    private void OnCommand(string command, string args)
    {
        OpenUi();
    }
    
    private void OpenUi()
    {
        WindowSystem.Windows[0].IsOpen = true;
        WindowSystem.Windows[0].BringToFront();
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        services.CommandManager.RemoveHandler("/ttsplogon");

        services.PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        services.PluginInterface.UiBuilder.OpenConfigUi -= OpenUi;
        services.PluginInterface.UiBuilder.OpenMainUi -= OpenUi;
        serviceProvider.Dispose();
    }
}