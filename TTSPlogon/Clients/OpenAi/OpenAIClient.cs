using System.Text;
using System.Text.Json;
using Dalamud.Plugin.Services;
using TTSPlogon.Queue;
using TTSPlogon.Utils;

namespace TTSPlogon.Clients.OpenAi;

public class OpenAiClient(SoundQueue soundQueue, HttpClient http, PluginConfig config, IPluginLog log)
    : ISpeechClient
{
    public PluginConfig.Provider Provider => PluginConfig.Provider.OpenAi;
    
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
       
    
    private readonly List<string> _feminineVoices =
    [
        "alloy",
        "fable",
        "nova",
        "shimmer"
    ];
    
    private readonly List<string> _masculineVoices =
    [
        "echo",
        "onyx"
    ];

    private readonly SemaphoreSlim _semaphore = new(1, 1);


    private void AddAuthorization(HttpRequestMessage req)
    {
        req.Headers.Add("Authorization", $"Bearer {config.OpenApiConfig.ApiKey}");
    }
    
    public async Task Say(EventHandler.ActorInfo? actor, string text, float speed, float volume)
    {
        var defaultVoice = "alloy";
        if (actor?.Gender == GenderUtils.Gender.Male)
        {
            var randomMale = _masculineVoices.OrderBy(x => Random.Shared.Next()).First();
            defaultVoice = randomMale;
        }
        else if (actor?.Gender == GenderUtils.Gender.Female)
        {
            var randomFemale = _feminineVoices.OrderBy(x => Random.Shared.Next()).First();
            defaultVoice = randomFemale;
        }        
        
        var voice = config.OpenApiConfig.GetSpeakerVoiceOrDefault(actor?.Name ?? "Unknown", defaultVoice);
        
        await _semaphore.WaitAsync();
        try
        {
            var uriBuilder = new UriBuilder(UrlBase) {Path = "/v1/audio/speech"};
            using var req = new HttpRequestMessage(HttpMethod.Post, uriBuilder.Uri);
            AddAuthorization(req);

            var args = new
            {
                model = "tts-1",
                input = text,
                voice = voice.ToLower(),
                response_format = "mp3",
                speed = speed
            };

            var json = JsonSerializer.Serialize(args);
            log.Debug($"[OpenAI][{voice}] {json}");
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            req.Content = content;

            var res = await http.SendAsync(req);
            EnsureSuccessStatusCode(res);

            var mp3Stream = new MemoryStream();
            var responseStream = await res.Content.ReadAsStreamAsync();
            await responseStream.CopyToAsync(mp3Stream);
            mp3Stream.Seek(0, SeekOrigin.Begin);

            soundQueue.EnqueueSound(new SoundQueueItem
            {
                Data = mp3Stream,
                Volume = volume,
                StreamDataType = StreamDataType.Mp3
            });
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static void EnsureSuccessStatusCode(HttpResponseMessage res)
    {
        if (!res.IsSuccessStatusCode)
            throw new HttpRequestException($"Request failed with status code {res.StatusCode}.");
    }
}