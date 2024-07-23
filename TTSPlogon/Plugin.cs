using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;
using TTSPlogon.Clients;
using TTSPlogon.Clients.OpenAi;
using TTSPlogon.Clients.SpeechSynthesisClient;
using TTSPlogon.Providers;
using TTSPlogon.Queue;
using TTSPlogon.Sound;
using TTSPlogon.Utils;

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
        [PluginService] public ISigScanner SigScanner { get; set; } = null!;
        [PluginService] public IGameInteropProvider Interop { get; set; } = null!;
        [PluginService] public IObjectTable ObjectTable { get; set; } = null!;
        
        public void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton(Framework);
            services.AddSingleton(PluginInterface);
            services.AddSingleton(CommandManager);
            services.AddSingleton(Log);
            services.AddSingleton(ChatGui);
            services.AddSingleton(GameGui);
            services.AddSingleton(ClientState);
            services.AddSingleton(SigScanner);
            services.AddSingleton(Interop);
            services.AddSingleton(ObjectTable);
        }
    }

    

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        var config = pluginInterface.GetPluginConfig() as PluginConfig ?? new PluginConfig();
        PluginConfig.PluginInterface = pluginInterface;
        
        pluginInterface.Inject(services);
        
        var serviceCollection = new ServiceCollection()
            .AddSingleton<MainWindow>()
            .AddSingleton<SoundQueue>()
            .AddSingleton<HttpClient>()
            .AddSingleton<PluginConfig>()
            .AddSingleton<EventHandler>()
            .AddSingleton<QueueProcessor>()
            .AddSingleton<SoundHandler>()
            .AddSingleton<LexiconHandler>()
            .AddSingleton(config);
        
        // get ISpeechClient types
        var speechClients = typeof(ISpeechClient).Assembly.GetTypes()
            .Where(x => x is {IsClass: true, IsAbstract: false} && x.GetInterfaces().Contains(typeof(ISpeechClient)));
        // add as collection
        foreach (var client in speechClients)
        {
            serviceCollection.AddSingleton(typeof(ISpeechClient), client);
        }
        var eventProviders = typeof(IEventProvider).Assembly.GetTypes()
            .Where(x => x is {IsClass: true, IsAbstract: false} && x.GetInterfaces().Contains(typeof(IEventProvider)));
        foreach (var provider in eventProviders)
        {
            serviceCollection.AddSingleton(typeof(IEventProvider), provider);
        }
        
        
        services.RegisterServices(serviceCollection);
        serviceProvider = serviceCollection.BuildServiceProvider();
        
        // lazy init
        var qp = serviceProvider.GetRequiredService<QueueProcessor>();
        var eh = serviceProvider.GetRequiredService<EventHandler>();
        var sh = serviceProvider.GetRequiredService<SoundHandler>();
        var epTypes = serviceProvider.GetServices<IEventProvider>();
        
        services.CommandManager.AddHandler("/ttsplogon", new CommandInfo((command, args) =>
        {
            switch (args)
            {
                case "toggle":
                    config.Enabled = !config.Enabled;
                    config.Save();
                    services.ChatGui.Print($"TTSPlogon is now {(config.Enabled ? "enabled" : "disabled")}");
                    break;
                case "on" or "enable":
                    config.Enabled = true;
                    config.Save();
                    services.ChatGui.Print("TTSPlogon is now enabled");
                    break;
                case "off" or "disable":
                    config.Enabled = false;
                    config.Save();
                    services.ChatGui.Print("TTSPlogon is now disabled");
                    break;
                default:
                    OpenUi();
                    break;
            }
        })
        {
            HelpMessage = "Open the menu"
        });
        
        WindowSystem.AddWindow(serviceProvider.GetRequiredService<MainWindow>());

        services.PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        services.PluginInterface.UiBuilder.DisableCutsceneUiHide = true;
        services.PluginInterface.UiBuilder.OpenMainUi += OpenUi;
        services.PluginInterface.UiBuilder.OpenConfigUi += OpenUi;
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