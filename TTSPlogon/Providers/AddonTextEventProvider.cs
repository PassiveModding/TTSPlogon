using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using TTSPlogon.Utils;

namespace TTSPlogon.Providers;

public class AddonTextEventProvider : IEventProvider, IDisposable
{
    private readonly IClientState _clientState;
    private readonly IFramework _framework;
    private readonly IGameGui _gui;
    private readonly EventHandler _handler;
    private readonly PluginConfig _config;
    private readonly IObjectTable _objectTable;
    
    public AddonTextEventProvider(PluginConfig config, IObjectTable objectTable, IClientState clientState, IFramework framework, IGameGui gui, EventHandler handler)
    {
        _config = config;
        _objectTable = objectTable;
        _clientState = clientState;
        _framework = framework;
        _gui = gui;
        _handler = handler;
        _framework.Update += OnFrameworkUpdate;
    }
    
    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!_clientState.IsLoggedIn)
        {
            // remove atk event handler
            return;
        }
        
        HandleTalk(_gui);
    }
    
    private unsafe void HandleTalk(IGameGui gui)
    {
        var talkAddress = gui.GetAddonByName("Talk");
        if (talkAddress == IntPtr.Zero)
        {
            return;
        }
        var talkAddon = (AddonTalk*) talkAddress.ToPointer();
        if (!talkAddon->AtkUnitBase.IsVisible)
        {
            return;
        }
        
        var (speaker, text) = TextUtils.ReadTalkAddon(talkAddon);
        if (speaker == _lastTalk.speaker && text == _lastTalk.text)
        {
            return;
        }
        
        var speakerObj = GenderUtils.GetGameObjectByName(_objectTable, speaker);

        var talkText = text;
        if (_lastTalk.speaker != speaker && _config.BeginFirstMessageWithSpeakerName && !string.IsNullOrWhiteSpace(speaker))
        {
            talkText = $"{speaker} says {text}";
        }
        
        _lastTalk = (speaker, text);
        _handler.InvokeChatEvent(EventHandler.ChatSource.TalkAddon, speaker, talkText, speakerObj);
    }
    
    private (string speaker, string text) _lastTalk = (string.Empty, string.Empty);

    public void Dispose()
    {
        _framework.Update -= OnFrameworkUpdate;
        _handler.Dispose();
    }
}