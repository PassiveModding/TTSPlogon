using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace TTSPlogon;

public class EventProvider : IDisposable
{
    private readonly IClientState _clientState;
    private readonly IFramework _framework;
    private readonly IGameGui _gui;
    private readonly IPluginLog _log;
    private readonly IChatGui _chatGui;

    public EventProvider(IChatGui chatGui, IClientState clientState, IFramework framework, IGameGui gui, IPluginLog log)
    {
        _chatGui = chatGui;
        _clientState = clientState;
        _framework = framework;
        _gui = gui;
        _log = log;
        _chatGui.ChatMessage += HandleChatMessage;
        _framework.Update += OnFrameworkUpdate;
    }
    
    public event Action<string, string>? ChatEvent;

    private void HandleChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool ishandled)
    {
        if (type != XivChatType.Echo)
        {
            return;
        }
        
        var senderName = sender.TextValue;
        var text = message.TextValue;
        ChatEvent?.Invoke(senderName, text);
    }
    
    public void CustomMessage(string text)
    {
        ChatEvent?.Invoke("", text);
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
        if (speaker == LastTalk.speaker && text == LastTalk.text)
        {
            return;
        }
        
        LastTalk = (speaker, text);
        ChatEvent?.Invoke(speaker, text);
    }
    
    private (string speaker, string text) LastTalk = (string.Empty, string.Empty);
    

    public void Dispose()
    {
        _chatGui.ChatMessage -= HandleChatMessage;
        _framework.Update -= OnFrameworkUpdate;
    }
}