using Dalamud.Plugin.Services;

namespace TTSPlogon.Queue;

public class QueueProcessor : IDisposable
{
    private readonly SoundQueue _soundQueue;
    private readonly PluginConfig _config;
    private readonly IPluginLog _log;
    private readonly CancellationTokenSource _cts;

    public QueueProcessor(SoundQueue soundQueue, PluginConfig config, IPluginLog log)
    {
        _soundQueue = soundQueue;
        _config = config;
        _log = log;
        _cts = new CancellationTokenSource();
        Task.Run(async () => await ProcessChatQueue(_cts.Token), _cts.Token);
    }
    
    private async Task ProcessChatQueue(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                _soundQueue.Play();
            }
            catch (Exception e)
            {
                _log.Error(e, "Failed to play sound");
            }

            await Task.Delay(50, token);
        }
    }

    public void Dispose()
    {
        _cts.Dispose();
    }
}