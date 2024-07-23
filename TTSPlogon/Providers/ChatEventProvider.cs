using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using TTSPlogon.Utils;

namespace TTSPlogon.Providers;

public class ChatEventProvider : IEventProvider, IDisposable
{
    private readonly IChatGui _chatGui;
    private readonly IObjectTable _objectTable;
    private readonly IPluginLog _log;
    private readonly EventHandler _handler;

    public ChatEventProvider(IObjectTable objectTable, IChatGui chatGui, IPluginLog log, EventHandler handler)
    {
        _chatGui = chatGui;
        _objectTable = objectTable;
        _log = log;
        _handler = handler;
        _chatGui.ChatMessage += HandleChatMessage;
    }

    private void HandleChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message,
        ref bool ishandled)
    {
        if (type != XivChatType.Echo)
        {
            return;
        }

        var senderObj = GenderUtils.GetGameObjectByName(_objectTable, sender);
        var senderName = GenderUtils.GetObjectString(sender, out var name) ? name : sender.TextValue;
        var textOutput = string.Join("", GenderUtils.SpeakableRegex().Matches(message.TextValue));
        _log.Info($"Chat message from {senderName}: {textOutput}");

        _handler.InvokeChatEvent(EventHandler.ChatSource.ChatLog, senderName, textOutput, senderObj);
    }

    public void Dispose()
    {
        _chatGui.ChatMessage -= HandleChatMessage;
        _handler.Dispose();
    }
}