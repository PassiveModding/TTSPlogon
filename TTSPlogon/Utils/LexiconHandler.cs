using System.Text;
using System.Xml.Linq;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace TTSPlogon.Utils;

public class LexiconHandler
{
    private readonly PluginConfig _config;

    public LexiconHandler(IPluginLog log, IDalamudPluginInterface pi, PluginConfig config)
    {
        _config = config;
        // load all files in Lexicons folder
        var pluginFolder = pi.AssemblyLocation.DirectoryName!;
        var lexPath = Path.Combine(pluginFolder, "Lexicons");
        log.Info($"Loading lexicons from {lexPath}");
        var lexiconFiles = Directory.GetFiles(lexPath, "*.pls");
        foreach (var lexiconFile in lexiconFiles)
        {
            var content = File.ReadAllText(lexiconFile);
            try
            {        
                var name = Path.GetFileNameWithoutExtension(lexiconFile);
                var lexicon = HandleLexicon(content, name);
                Lexicons.Add(lexicon);
                log.Info($"Loaded lexicon: {name}");
            }
            catch (Exception e)
            {
                log.Error(e, $"Failed to parse lexicon file: {lexiconFile}");
            }
        }
    }

    private Lexicon HandleLexicon(string xmlStr, string lexiconId)
    {
        var xml = XDocument.Parse(xmlStr);
        if (xml.Root == null)
        {
            throw new Exception("Failed to parse lexicon");
        }
        
        var nsPrefix = xml.Root.GetDefaultNamespace();
        
        // Create the lexicon
        var lexemes = xml.Root.Descendants(nsPrefix + "lexeme").SelectMany(lexeme =>
        {
            var graphemes = lexeme.Descendants(nsPrefix + "grapheme").Select(x => x.Value).OrderBy(x => x.Length).ToArray();
            var phoneme = lexeme.Descendants(nsPrefix + "phoneme").FirstOrDefault()?.Value ?? "";
            var alias = lexeme.Descendants(nsPrefix + "alias").FirstOrDefault()?.Value ?? "";
            
            // https://github.com/karashiiro/TextToTalk/issues/37#issuecomment-899733701
            // There are some weird incompatibilities in the SSML reader that this helps to fix.
            phoneme = phoneme.Replace(":", "ː")
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("ʤ", "d͡ʒ");

            var lexemeSet = new List<Lexeme>();
            foreach (var grapheme in graphemes)
            {
                lexemeSet.Add(new Lexeme(grapheme, phoneme, alias));
            }
            
            return lexemeSet;
        }).OrderByDescending(x => x.Grapheme.Length).ToArray();
        
        var lexicon = new Lexicon(lexiconId, lexemes);
        return lexicon;
    }
    
    public List<Lexicon> Lexicons { get; } = new();
    
    // method for a given string, perform replacements based on lexicons
    public string AsSsml(string input)
    {
        input = " " + input + " ";
        foreach (var lexicon in Lexicons.Where(x => _config.EnabledLexicons.Contains(x.Name)))
        {
            foreach (var lexeme in lexicon.Lexemes)
            {
                if (!string.IsNullOrEmpty(lexeme.Alias))
                {
                    input = input.Replace(lexeme.Grapheme, lexeme.Alias);
                    break;
                }

                if (!string.IsNullOrEmpty(lexeme.Phoneme))
                {
                    var graphemeReadable = lexeme.Grapheme
                        .Replace("'", "")
                        .Replace("\"", "");
                    
                    // basic xml doc
                    var phonemeXml = new XElement("phoneme",
                        new XAttribute("ph", lexeme.Phoneme),
                        graphemeReadable);
                    
                    var phonemeNode = phonemeXml.ToString();
                    
                    input = ReplaceGrapheme(input, lexeme.Grapheme, phonemeNode);
                }
            }
        }
        
        var speakTag = $"<speak xml:lang=\"en\" version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\">{input}</speak>";
        return speakTag;
    }
    
    internal static string ReplaceGrapheme(string text, string oldValue, string newValue)
    {
        var xIdx = text.IndexOf(oldValue, StringComparison.InvariantCulture);
        if (xIdx == -1)
        {
            return text;
        }

        // Ensure we're not surrounding something that was already surrounded.
        // We build an array in which open tags (<phoneme>) are represented by 1,
        // and and close tags (</phoneme>) are represented by 2.
        var curMarker = (byte)1;
        var tags = new byte[text.Length];
        var inTag = false;
        var lastCharWasLeftBracket = false;
        for (var i = 0; i < text.Length; i++)
        {
            if (lastCharWasLeftBracket)
            {
                lastCharWasLeftBracket = false;
                curMarker = text[i] == '/' ? (byte)2 : (byte)1;
                tags[i - 1] = curMarker;
            }

            if (inTag)
            {
                tags[i] = curMarker;
            }

            if (!inTag && text[i] == '<')
            {
                inTag = true;
                lastCharWasLeftBracket = true;
                tags[i] = curMarker;
            }

            if (text[i] == '>')
            {
                inTag = false;
                tags[i] = curMarker;
            }
        }

        // Starting from the index of the text we want to replace, we move right
        // and ensure that we do not encounter a 2 before we encounter a 1.
        for (var i = xIdx; i < text.Length; i++)
        {
            if (tags[i] == 1)
            {
                // A-OK
                break;
            }

            if (tags[i] == 2)
            {
                // Not A-OK, return early
                return text;
            }
        }

        return text[..xIdx] + newValue + ReplaceGrapheme(text[(xIdx + oldValue.Length)..], oldValue, newValue);
    }
    
    public record Lexeme(string Grapheme, string Phoneme, string Alias);
    public record Lexicon(string Name, Lexeme[] Lexemes);
}