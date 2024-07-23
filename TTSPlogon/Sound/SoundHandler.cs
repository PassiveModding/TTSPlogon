using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;

namespace TTSPlogon.Sound;

public class SoundHandler : IDisposable
{
    private const string LoadSoundFileSig = "E8 ?? ?? ?? ?? 48 85 C0 75 04 B0 F6";

    private const string PlaySpecificSoundSig =
        "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 33 F6 8B DA 48 8B F9 0F BA E2 0F";
    
    private delegate nint LoadSoundFileDelegate(nint resourceHandlePtr, uint arg2);
    
    private delegate nint PlaySpecificSoundDelegate(nint soundPtr, int arg2);
    
    private readonly Hook<LoadSoundFileDelegate>? loadSoundFileHook;
    private readonly Hook<PlaySpecificSoundDelegate>? playSpecificSoundHook;
    
    private static readonly int ResourceDataOffset = Marshal.SizeOf<ResourceHandle>();
    private static readonly int SoundDataOffset = Marshal.SizeOf<nint>();
    
    private const string SoundContainerFileNameSuffix = ".scd";
    
    
    private static readonly Regex IgnoredSoundFileNameRegex = new(
        @"^(bgcommon|music|sound/(battle|foot|instruments|strm|vfx|voice/Vo_Emote|zingle))/");
    private static readonly Regex VoiceLineFileNameRegex = new(@"^cut/.*/(vo_|voice)");
    private readonly HashSet<nint> knownVoiceLinePtrs = new();

    private readonly EventHandler _eventHandler;
    private readonly IPluginLog _log;

    public SoundHandler(EventHandler eventHandler, ISigScanner sigScanner, IGameInteropProvider gameInterop, IPluginLog log)
    {        
        _eventHandler = eventHandler;
        _log = log;

        if (sigScanner.TryScanText(LoadSoundFileSig, out var loadSoundFilePtr))
        {
            loadSoundFileHook = gameInterop.HookFromAddress<LoadSoundFileDelegate>(loadSoundFilePtr, LoadSoundFileDetour);
            loadSoundFileHook.Enable();
        }
        else
        {
            _log.Error("Failed to hook into LoadSoundFile");
        }

        if (sigScanner.TryScanText(PlaySpecificSoundSig, out var playSpecificSoundPtr))
        {
            playSpecificSoundHook = gameInterop.HookFromAddress<PlaySpecificSoundDelegate>(playSpecificSoundPtr, PlaySpecificSoundDetour);
            playSpecificSoundHook.Enable();
        }
        else
        {
            _log.Error("Failed to hook into PlaySpecificSound");
        }
        
    }
    
    private nint LoadSoundFileDetour(nint resourceHandlePtr, uint arg2)
    {
        var result = loadSoundFileHook!.Original(resourceHandlePtr, arg2);

        try
        {
            string fileName;
            unsafe
            {
                fileName = ((ResourceHandle*)resourceHandlePtr)->FileName.ToString();
            }

            if (fileName.EndsWith(SoundContainerFileNameSuffix))
            {
                var resourceDataPtr = Marshal.ReadIntPtr(resourceHandlePtr + ResourceDataOffset);
                if (resourceDataPtr != nint.Zero)
                {
                    var isVoiceLine = false;

                    if (!IgnoredSoundFileNameRegex.IsMatch(fileName))
                    {
                        _log.Debug($"Loaded sound: {fileName}");

                        if (VoiceLineFileNameRegex.IsMatch(fileName))
                        {
                            isVoiceLine = true;
                        }
                    }

                    if (isVoiceLine)
                    {
                        _log.Debug($"Discovered voice line at address {resourceDataPtr:x}");
                        this.knownVoiceLinePtrs.Add(resourceDataPtr);
                    }
                    else
                    {
                        // Addresses can be reused, so a non-voice-line sound may be loaded to an address previously
                        // occupied by a voice line.
                        if (this.knownVoiceLinePtrs.Remove(resourceDataPtr))
                        {
                            _log.Debug(
                                $"Cleared voice line from address {resourceDataPtr:x} (address reused by: {fileName})");
                        }
                    }
                }
            }
        }
        catch (Exception exc)
        {
            _log.Error(exc, "Error in LoadSoundFile detour");
        }

        return result;
    }
    
    private nint PlaySpecificSoundDetour(nint soundPtr, int arg2)
    {
        var result = playSpecificSoundHook!.Original(soundPtr, arg2);

        try
        {
            var soundDataPtr = Marshal.ReadIntPtr(soundPtr + SoundDataOffset);
            // Assume that a voice line will be played only once after it's loaded. Then the set can be pruned as voice
            // lines are played.
            if (knownVoiceLinePtrs.Remove(soundDataPtr))
            {
                _log.Debug($"Caught playback of known voice line at address {soundDataPtr:x}");
                _eventHandler.VoicedLineTime = DateTime.Now;
            }
        }
        catch (Exception exc)
        {
            _log.Error(exc, "Error in PlaySpecificSound detour");
        }

        return result;
    }

    public void Dispose()
    {
        loadSoundFileHook?.Dispose();
        playSpecificSoundHook?.Dispose();
        _eventHandler.Dispose();
    }
}