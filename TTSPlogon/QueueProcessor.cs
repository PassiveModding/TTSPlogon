namespace TTSPlogon;

public class QueueProcessor : IDisposable
{
    private readonly SoundQueue _soundQueue;
    private readonly CancellationTokenSource _cts;

    public QueueProcessor(SoundQueue soundQueue)
    {
        _soundQueue = soundQueue;
        _cts = new CancellationTokenSource();
        Task.Run(async () => await ProcessChatQueue(_cts.Token));
    }
    
    private async Task ProcessChatQueue(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            _soundQueue.Play();
            await Task.Delay(1000, token);
        }
    }

    public void Dispose()
    {
        _cts.Dispose();
    }
}