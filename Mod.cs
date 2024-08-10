using p3ppc.unhardcodedNames.Configuration;
using p3ppc.unhardcodedNames.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Memory;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;
using System.Collections.Generic;
using Reloaded.Memory.Interfaces;
using Reloaded.Memory.Utilities;
using System;
using System.Runtime.InteropServices;

namespace p3ppc.unhardcodedNames;

/// <summary>
/// Your mod logic goes here.
/// </summary>
public unsafe class Mod : ModBase // <= Do not Remove.
{
    /// <summary>
    /// Provides access to the mod loader API.
    /// </summary>
    private readonly IModLoader _modLoader;

    /// <summary>
    /// Provides access to the Reloaded.Hooks API.
    /// </summary>
    /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
    private readonly IReloadedHooks? _hooks;

    /// <summary>
    /// Provides access to the Reloaded logger.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Entry point into the mod, instance that created this class.
    /// </summary>
    private readonly IMod _owner;

    /// <summary>
    /// Provides access to this mod's configuration.
    /// </summary>
    private Config _configuration;

    /// <summary>
    /// The configuration of the currently executing mod.
    /// </summary>
    private readonly IModConfig _modConfig;
    
    private Memory _memory;

    private IHook<GetNameDelegate> _getItemNameHook;
    private IHook<GetNameDelegate> _getCharacterFullNameHook;
    private IHook<GetNameDelegate> _getCharacterFirstNameHook;
    private IHook<GetNameDelegate> _getSLinkNameHook;
    private IHook<GetTextDelegate> _getTextHook;
    private IHook<GetTextDelegate> _getGlossaryTextHook;
    private Language* _language;

    private Dictionary<Language, Encoding> _encodings;
    static byte[] HexStringToByteArray(string hexString)
    {
        hexString = hexString.Replace("\\x", ""); // Removing "\x" from the string
        int length = hexString.Length / 2;
        byte[] byteArray = new byte[length];

        for (int i = 0; i < length; i++)
        {
            byteArray[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
        }

        return byteArray;
    }
    static Dictionary<string, byte[]> DeserializeJsonEncodingToDictionary(string jsonString)
    {
        Dictionary<string, string> stringDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);
        Dictionary<string, byte[]> byteDictionary = new Dictionary<string, byte[]>();
        foreach (var kvp in stringDictionary)
        {
            byteDictionary.Add(kvp.Key, HexStringToByteArray(kvp.Value));
        }

        return byteDictionary;
    }
    static Dictionary<ulong, string> DeserializeJsonPointermapToDictionary(string jsonString)
    {
        Dictionary<string, string> stringDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);
        Dictionary<ulong, string> Dictionary = new Dictionary<ulong, string>();
        foreach (var kvp in stringDictionary)
        {
            Dictionary.Add(Convert.ToUInt64(kvp.Key , 16), kvp.Value);
        }

        return Dictionary;
    }
    private void SetupEncodings()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _encodings = new()
        {
            { Language.English, Encoding.UTF8 },
            { Language.French, Encoding.UTF8 },
            { Language.German, Encoding.UTF8 },
            { Language.Italian, Encoding.UTF8 },
            { Language.Japanese, Encoding.Unicode },
            { Language.Korean, Encoding.Unicode },
            { Language.SimplifiedChinese, Encoding.Unicode },
            { Language.TraditionalChinese, Encoding.Unicode },
            { Language.Spanish, Encoding.UTF8 },
            //{ Language.Custom, Encoding.UTF8 },
        };
    }
    public Dictionary<string, byte[]>CustomEncoding;
    public Dictionary<ulong, string>RawPointermap;
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr LoadLibrary(string lpLibFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr GetKeyboardLayout(uint idThread);
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _configuration = context.Configuration;
        _modConfig = context.ModConfig;
        _memory = Memory.Instance;

        if (!Utils.Initialise(_logger, _configuration, _modLoader))
            return;

        SetupEncodings();

        _modLoader.ModLoading += OnModLoading;
        
        /*foreach (var mod in _modLoader.GetActiveMods().Where(x => x.Generic.ModDependencies.Contains(_modConfig.ModIcon))){
            AddNamesFromDir(_modLoader.GetDirectoryForModId(mod.Generic.ModId));
        }*/
    }

    public void OnModLoading(IModV1 mod, IModConfigV1 config)
    {
        if (config.ModDependencies.Contains(_modConfig.ModId))
        {
            AddCustomEncoding(_modLoader.GetDirectoryForModId(config.ModId), "CustomEncoding.json");
            AddRawPointermap(_modLoader.GetDirectoryForModId(config.ModId), "RawPointermap.json");
            //AddNamesFromDir(_modLoader.GetDirectoryForModId(config.ModId));
            //Utils.LogError(RawPointermap[0x1405fcc78]);
            WriteRawPointermap();
            
        }
            
    }

    private void DumpText()
    {

    }

    public void AddCustomEncoding(string dir, string nameFile)
    {
        var encPath = Path.Combine(dir, nameFile);
        if (!File.Exists(encPath)) return;
        var json = File.ReadAllText(encPath, Encoding.UTF8);
        CustomEncoding = DeserializeJsonEncodingToDictionary(json);
    }
    public void AddRawPointermap(string dir, string nameFile)
    {
        var encPath = Path.Combine(dir, nameFile);
        if (!File.Exists(encPath)) return;
        var json = File.ReadAllText(encPath, Encoding.UTF8);
        RawPointermap = DeserializeJsonPointermapToDictionary(json);
    }

    private void AddNamesFromDir<T1, T2>(string dir, Dictionary<int, nuint[]> namesDict, string nameFile, Action<object, Dictionary<int, nuint[]>, int, int> WriteName)
        where T1 : IName<T2>
    {
        var namesPath = Path.Combine(dir, nameFile);
        if (!File.Exists(namesPath)) return;

        var json = File.ReadAllText(namesPath, Encoding.UTF8);
        var names = JsonSerializer.Deserialize<List<T1>>(json);
        if (names == null)
        {
            Utils.LogError($"Error parsing names from {namesPath}");
            return;
        }

        foreach (var name in names)
        {
            var id = name.Id;
            var languages = Enum.GetNames(typeof(Language));

            if (!namesDict.ContainsKey(id))
                namesDict[id] = new nuint[languages.Length];

            for (int i = 0; i < languages.Length; i++)
            {
                var langName = name.GetType().GetProperty(languages[i]).GetValue(name);
                if (langName == null && name.All != null)
                    langName = name.All;
                if (langName != null)
                {
                    WriteName(langName, namesDict, id, i);
                }
            }
        }
    }

    private void WriteGenericName(object langName, Dictionary<int, nuint[]> namesDict, int id, int lang)
    {
        var address = WriteString((string)langName, (Language)lang);
        namesDict[id][lang] = address;
    }

    private byte[] GetBytesCustomEnc(string text)
    {
        
        List<byte> byteList  = new List<byte>();

        // go through each character in the text
        foreach (char symbol in text)
        {
            string key = symbol.ToString();

            // Check if there is a character in the dictionary
            if (CustomEncoding.ContainsKey(key))
            {
                // If there is, add its value to the byte array
                byteList.AddRange(CustomEncoding[key]);
            }
            else
            {
                // If not, add the utf-8 encrypted character
                byteList.AddRange(Encoding.UTF8.GetBytes(key));
            }
        }
        byte[] byteArray = byteList.ToArray();
        return byteArray;
    }

    private nuint WriteString(string text, Language language)
    {
        
        byte[] bytes = new byte[0];
        Utils.Log(Convert.ToString(CustomEncoding));
        if (CustomEncoding!=null)
        {
            bytes = GetBytesCustomEnc(text);
        }
        else
        {
            bytes = _encodings[language].GetBytes(text);
        }
        var address = _memory.Allocate((nuint)bytes.Length).Address;
        _memory.WriteRaw(address, bytes);
        return address;
    }
    public void WriteRawPointermap()
    {
        foreach (var key in RawPointermap.Keys)
        {
            //Utils.LogError(RawPointermap[key]);
            
            //Utils.LogError(string.Join(" ", BitConverter.GetBytes(key))); 
            
            
            //Utils.LogError(string.Join(" ", BitConverter.GetBytes(WriteString(RawPointermap[key], (Language)1))));
            //Utils.LogError(string.Join(" ",BitConverter.GetBytes(key)));
            var toSearchPattern = BitConverter.ToString(BitConverter.GetBytes(key)).Replace("-", " ");
            //Utils.LogError(toSearchPattern);
            //Utils.SigScan()
            Utils.SigScan(toSearchPattern, "WriteRawPointermap", address => 
            {
                
                //Utils.LogError($"found{address}");
                _memory.SafeWrite((nuint)address, BitConverter.GetBytes(WriteString(RawPointermap[key], (Language)1)));
                
            });
        }    
        return;
    }


    [Function(CallingConventions.Microsoft)]
    private delegate nuint GetNameDelegate(short id);

    [Function(CallingConventions.Microsoft)]
    private delegate nuint GetTextDelegate(int major, int minor);

    private enum Language : int
    {
        Japanese,
        English,
        Korean,
        TraditionalChinese,
        SimplifiedChinese,
        French,
        German,
        Italian,
        Spanish
        //Custom
    }

    #region Standard Overrides
    public override void ConfigurationUpdated(Config configuration)
    {
        // Apply settings from configuration.
        // ... your code here.
        _configuration = configuration;
        _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
    }
    #endregion

    #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod() { }
#pragma warning restore CS8618
    #endregion
}