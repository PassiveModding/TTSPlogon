using System.Text.RegularExpressions;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;

namespace TTSPlogon.Utils;

public partial class GenderUtils
{
    public enum Gender : sbyte
    {
        None = -1,
        Male,
        Female
    }

    [GeneratedRegex(@"\p{L}+|\p{M}+|\p{N}+|\s+", RegexOptions.Compiled)]
    public static partial Regex SpeakableRegex();

    public static bool GetObjectString(SeString input, out string output)
    {
        output = string.Join("", SpeakableRegex().Matches(input.TextValue));
        foreach (var payload in input.Payloads)
        {
            if (payload is PlayerPayload pp)
            {
                output = pp.PlayerName;
                return true;
            }
        }
        return !string.IsNullOrEmpty(output);
    }
    
    public static IGameObject? GetGameObjectByName(IObjectTable objects, SeString? name)
    {
        // Names are complicated; the name SeString can come from chat, meaning it can
        // include the cross-world icon or friend group icons or whatever else.
        if (name is null) return null;
        if (!GetObjectString(name, out var parsedName)) return null;
        if (string.IsNullOrEmpty(name.TextValue)) return null;
        return objects.FirstOrDefault(gObj =>
            GetObjectString(gObj.Name, out var gObjName) && gObjName == parsedName);
    }
    
    public static unsafe Gender GetCharacterGender(IGameObject? gObj)
    {
        if (gObj == null || gObj.Address == nint.Zero)
        {
            return Gender.None;
        }
        
        var charaStruct = (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)gObj.Address;
        
        var actorGender = (Gender)charaStruct->DrawData.CustomizeData.Sex;
        
        return actorGender;
    }
}