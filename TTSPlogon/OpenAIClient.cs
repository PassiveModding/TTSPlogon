using System.Text;
using System.Text.Json;
using Dalamud.Plugin.Services;

namespace TTSPlogon;

public class OpenAiClient
{
    private const string UrlBase = "https://api.openai.com";

    public static readonly IReadOnlySet<string> Models = new HashSet<string>
    {
        "tts-1",
        "tts-1-hd"
    };
    
    public static readonly IReadOnlySet<string> Voices = new HashSet<string>
    {
        "alloy",
        "echo",
        "fable",
        "onyx",
        "nova",
        "shimmer"
    };

    private readonly HttpClient client;
    private readonly PluginConfig _config;
    private readonly IPluginLog _log;
    private readonly SoundQueue soundQueue;
    private readonly SemaphoreSlim semaphore = new(1, 1);

    public OpenAiClient(SoundQueue soundQueue, HttpClient http, PluginConfig config, IPluginLog log)
    {
        this.soundQueue = soundQueue;
        client = http;
        _config = config;
        _log = log;
    }


    private void AddAuthorization(HttpRequestMessage req)
    {
        req.Headers.Add("Authorization", $"Bearer {_config.OpenApiConfig.ApiKey}");
    }

    public async Task Say(string? voice, float? speed, float volume, string text)
    {
        await semaphore.WaitAsync();
        try
        {
            var uriBuilder = new UriBuilder(UrlBase) {Path = "/v1/audio/speech"};
            using var req = new HttpRequestMessage(HttpMethod.Post, uriBuilder.Uri);
            AddAuthorization(req);

            var args = new
            {
                model = "tts-1",
                input = text,
                voice = voice?.ToLower() ?? "alloy",
                response_format = "mp3",
                speed = speed ?? 1.0f
            };

            var json = JsonSerializer.Serialize(args);
            _log.Information(json);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            req.Content = content;

            var res = await client.SendAsync(req);
            EnsureSuccessStatusCode(res);

            var mp3Stream = new MemoryStream();
            var responseStream = await res.Content.ReadAsStreamAsync();
            await responseStream.CopyToAsync(mp3Stream);
            mp3Stream.Seek(0, SeekOrigin.Begin);

            soundQueue.EnqueueSound(mp3Stream, volume);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static void EnsureSuccessStatusCode(HttpResponseMessage res)
    {
        if (!res.IsSuccessStatusCode)
            throw new HttpRequestException($"Request failed with status code {res.StatusCode}.");
    }
}

public class OpenApiConfig
{
    public string? ApiKey { get; set; }
    public float BaseSpeed { get; set; } = 1.0f; // multiplied with specific voice speed
    public float BaseVolume { get; set; } = 0.5f; // multiplied with specific voice volume
}