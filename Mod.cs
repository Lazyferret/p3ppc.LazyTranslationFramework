namespace p3ppc.LazyTranslationFramework.Configuration;
using p3ppc.LazyTranslationFramework.Template;
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
using Reloaded.Memory;
using Reloaded.Memory.Sigscan;
using Reloaded.Memory.Sigscan.Definitions.Structs;
using Reloaded.Memory.Sigscan.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Microsoft.VisualBasic;
using System.Text.Json.Serialization;
using System.Globalization;
using global::p3ppc.LazyTranslationFramework.Template;


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
    
    public static Memory _memory;
    private List<nuint> _allocatedPointers = new List<nuint>();
    private List<Reloaded.Memory.Structs.MemoryAllocation> _allocatedMemory = new List<Reloaded.Memory.Structs.MemoryAllocation>();

    private Language* _language;

    private Dictionary<Language, Encoding> _encodings;
    public class Item
    {
    [JsonPropertyName("text")]
    public string Text { get; set; }
    [JsonPropertyName("original")]
    public string Original { get; set; }

    [JsonPropertyName("occurrences")]
    public List<string> Occurrences { get; set; }
    }
    public class FileItem
    {
    [JsonPropertyName("file")]
    public string File { get; set; }

    [JsonPropertyName("occurrences")]
    public List<string> Occurrences { get; set; }
    }
    public class FileItemRaw
    {
    [JsonPropertyName("offset")]
    public int Offset { get; set; }
    
    [JsonPropertyName("file")]
    public string File { get; set; }

    }
    public class UlongItem
    {
    [JsonPropertyName("text")]
    public string Text { get; set; }
    [JsonPropertyName("original")]
    public string Original { get; set; }

    [JsonPropertyName("occurrences")]
    public List<ulong> Occurrences { get; set; }
    }
    public class UlongFileItem
    {
    [JsonPropertyName("file")]
    public string File { get; set; }

    [JsonPropertyName("occurrences")]
    public List<ulong> Occurrences { get; set; }
    }
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
    /*static Dictionary<ulong, UlongItem> DeserializeJsonPointermapToDictionary(string jsonString)
    {

        Dictionary<string, Item> items = JsonSerializer.Deserialize<Dictionary<string, Item>>(jsonString);
        Dictionary<ulong, UlongItem> pointermap = new Dictionary<ulong, UlongItem>();

        foreach (var kvp in items)
        {
            // Convert the key from hex string to ulong
            ulong key = Convert.ToUInt64(kvp.Key, 16);
            List<ulong> occurences = new List<ulong>();
            foreach (var occurrence in kvp.Value.Occurrences)
            {
                occurences.Add(Convert.ToUInt64(occurrence, 16));
            }
            UlongItem tempItem = new UlongItem
            {
                Text = kvp.Value.Text,
                Occurrences = occurences
            };
            pointermap.Add(key, tempItem);
        }
        return pointermap;
    }*/
    static Dictionary<ulong, UlongItem> DeserializeJsonPointermapToDictionary(string jsonString)
    {
        // Изменяем тип Dictionary для десериализации, чтобы использовать класс UlongItem с новым свойством Original
        Dictionary<string, Item> items = JsonSerializer.Deserialize<Dictionary<string, Item>>(jsonString);
        Dictionary<ulong, UlongItem> pointermap = new Dictionary<ulong, UlongItem>();

        foreach (var kvp in items)
        {
            ulong key = Convert.ToUInt64(kvp.Key, 16);
            List<ulong> occurrences = new List<ulong>();

            if (kvp.Value.Occurrences != null)
            {
                foreach (var occurrence in kvp.Value.Occurrences)
                {
                    occurrences.Add(Convert.ToUInt64(occurrence, 16));
                }
            }
            else
            {
                Utils.LogError($"Хуйня {key:X}");
                Utils.LogError($"Хуйня {kvp.Value.Text}");
                Utils.LogError($"Хуйня {kvp.Value.Occurrences}");
                
            }

            UlongItem tempItem = new UlongItem
            {
                Text = kvp.Value.Text,
                Original = kvp.Value.Original,
                Occurrences = occurrences
            };
            pointermap.Add(key, tempItem);
        }

        return pointermap;
    }
    static Dictionary<ulong, UlongFileItem> DeserializeJsonEmbededFilesListToDictionary(string jsonString)
    {

        // Десериализуем JSON в словарь с ключами типа string
        Dictionary<string, FileItem> items = JsonSerializer.Deserialize<Dictionary<string, FileItem>>(jsonString);
        Dictionary<ulong, UlongFileItem> EmbededFilesList = new Dictionary<ulong, UlongFileItem>();

        foreach (var kvp in items)
        {
            // Convert the key from hex string to ulong
            ulong key = Convert.ToUInt64(kvp.Key, 16);
            List<ulong> occurences = new List<ulong>();
            foreach (var occurrence in kvp.Value.Occurrences)
            {
                occurences.Add(Convert.ToUInt64(occurrence, 16));
            }
            UlongFileItem tempItem = new UlongFileItem
            {
                File = kvp.Value.File,
                Occurrences = occurences
            };
            EmbededFilesList.Add(key, tempItem);
        }
        return EmbededFilesList;
    }
    static Dictionary<string, FileItemRaw> DeserializeJsonEmbededFilesListRawToDictionary(string jsonString)
    {
        // Десериализуем JSON в словарь с ключами типа string
        Dictionary<string, FileItemRaw> EmbededFilesList = JsonSerializer.Deserialize<Dictionary<string, FileItemRaw>>(jsonString);
        return EmbededFilesList;
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
    public Dictionary<ulong, UlongItem>Pointermap;
    public Dictionary<ulong, UlongFileItem>EmbededFileList;
    public Dictionary<string, FileItemRaw>EmbededFileListRaw;
    
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr LoadLibrary(string lpLibFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
    
    public string ModDirectory;


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
            ModDirectory = _modLoader.GetDirectoryForModId(config.ModId);
            Utils.LogError($"Mod dir {ModDirectory}");
            AddCustomEncoding(ModDirectory, "CustomEncoding.json");
            AddPointermap(ModDirectory, "Pointermap.json");
            //AddNamesFromDir(_modLoader.GetDirectoryForModId(config.ModId));
            //Utils.LogError(Pointermap[0x1405fcc78]);
            WritePointermap();
            AddEmbededFilesList(ModDirectory, "EmbededFiles.json");
            WriteEmbededFiles();
            AddEmbededFilesListRaw(ModDirectory, "EmbededFilesRawReplace.json");
            WriteEmbededFilesRaw();
            
        }
            
    }

    public void AddCustomEncoding(string dir, string nameFile)
    {
        var encPath = Path.Combine(dir, nameFile);
        if (!File.Exists(encPath)) return;
        var json = File.ReadAllText(encPath, Encoding.UTF8);
        CustomEncoding = DeserializeJsonEncodingToDictionary(json);
    }
    public void AddPointermap(string dir, string nameFile)
    {
        var encPath = Path.Combine(dir, nameFile);
        if (!File.Exists(encPath)) return;
        var json = File.ReadAllText(encPath, Encoding.UTF8);
        Pointermap = DeserializeJsonPointermapToDictionary(json);
    }
    public void AddEmbededFilesList(string dir, string nameFile)
    {
        var encPath = Path.Combine(dir, nameFile);
        if (!File.Exists(encPath)) return;
        var json = File.ReadAllText(encPath, Encoding.UTF8);
        EmbededFileList = DeserializeJsonEmbededFilesListToDictionary(json);
    }
    public void AddEmbededFilesListRaw(string dir, string nameFile)
    {
        var encPath = Path.Combine(dir, nameFile);
        if (!File.Exists(encPath)) return;
        var json = File.ReadAllText(encPath, Encoding.UTF8);
        EmbededFileListRaw = DeserializeJsonEmbededFilesListRawToDictionary(json);
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

    public nuint WriteString(string text, Language language)
    {
        
        byte[] bytes = new byte[0];
        //Utils.Log(Convert.ToString(CustomEncoding));
        if (CustomEncoding!=null)
        {
            bytes = GetBytesCustomEnc(text);
        }
        else
        {
            bytes = _encodings[language].GetBytes(text);
        }
        var allocated =_memory.Allocate((nuint)bytes.Length); 
        var address = allocated.Address;
        //_memory.WriteRaw(address, bytes);
        Utils.LogError($"{text} 0x{address:X}");
        _memory.SafeWrite(address, bytes);
        _allocatedPointers.Add(address);
        _allocatedMemory.Add(allocated);
        return address;
    }
    public nuint WriteFile(nuint address, byte[] file, int offset = 0)
    {
        Utils.LogError($"file {address:X}");
        nuint finalAddress = address + (nuint)offset;
        //_memory.ChangeProtectionRaw(finalAddress, file.Length, 64);
        //_memory.WriteRaw(finalAddress, file);
        _memory.SafeWrite(finalAddress, file);
        return finalAddress;
    }
    public nuint WriteFileAlloc(byte[] file)
    {
        var allocated =_memory.Allocate((nuint)file.Length); 
        var address = allocated.Address;
        Utils.LogError($"file {address:X}");
        _memory.SafeWrite(address, file);
         _allocatedPointers.Add(address);
        _allocatedMemory.Add(allocated);
        return address;
    }


    public void WritePointermap()
    {
        foreach (var item in Pointermap)
        {
            //Utils.LogError(Pointermap[key]);
            
            //Utils.LogError(string.Join(" ", BitConverter.GetBytes(key))); 
            
            
            //Utils.LogError(string.Join(" ", BitConverter.GetBytes(WriteString(Pointermap[key], (Language)1))));
            //Utils.LogError(string.Join(" ",BitConverter.GetBytes(key)));
            //var toSearchPattern = BitConverter.ToString(BitConverter.GetBytes(key)).Replace("-", " ");
            //Utils.LogError(toSearchPattern);
            //Utils.SigScan()
            /*Utils._startupScanner.AddMainModuleScan(toSearchPattern, (result) =>
            {
                if (!result.Found)
                {
                    Utils.LogError($"Unable to find {toSearchPattern}, stuff won't work :(");
                    return;
                }
                RecursiveReplace(toSearchPattern, Pointermap[key], result);
            });
            */
            /*
            Utils.SigScan(toSearchPattern, "WritePointermap", address => 
            {
                Utils.LogError($"found {address:X} {Pointermap[key]}");
                _memory.SafeWrite((nuint)address, BitConverter.GetBytes(WriteString(Pointermap[key], Language.English)));
                
            });
            */
            Utils.Log($"0x{item.Key:X} {item.Value.Text} Occurrences: {string.Join(", ", item.Value.Occurrences.Select(x => $"0x{x:X}"))}");
            var address = WriteString(item.Value.Text, Language.English);
            Span<byte> spanAddress;

            // Use unsafe code to create a Span<byte> from the nuint
            unsafe
            {
                spanAddress = new Span<byte>(&address, sizeof(nuint));
            }
            foreach (var occurence in item.Value.Occurrences)
            {
                _memory.SafeWrite((nuint)occurence, spanAddress);

            }

            
        }    
        
    }
    public void WriteEmbededFiles()
    {
        
        string directoryPath = ModDirectory + "\\EmbededFiles";
        foreach (var fileEntry in EmbededFileList)
        {
            Utils.Log($"0x{fileEntry.Key:X} {fileEntry.Value.File} Occurrences: {string.Join(", ", fileEntry.Value.Occurrences.Select(addr => $"0x{addr:X}"))}");
            ulong fileKey = fileEntry.Key;
            string fileName = fileEntry.Value.File;
            string filePath = Path.Combine(directoryPath, fileName);

            if (File.Exists(filePath))
            {
                // Чтение файла как массива байтов
                byte[] fileData = File.ReadAllBytes(filePath);
                var address = WriteFileAlloc(fileData);
                Span<byte> spanAddress;

                // Use unsafe code to create a Span<byte> from the nuint
                unsafe
                {
                    spanAddress = new Span<byte>(&address, sizeof(nuint));
                }
                foreach (var occurence in fileEntry.Value.Occurrences)
                {
                    _memory.SafeWrite((nuint)occurence, spanAddress);
                }

                Utils.LogError($"Файл {fileName} успешно загружен.");
            }
            else
            {
                Utils.LogError($"Файл {fileName} не найден по пути: {filePath}");
            }
        }
        
    }
    public void WriteEmbededFilesRaw()
    {
        
        string directoryPath = ModDirectory + "\\EmbededFiles";
        foreach (var fileEntry in EmbededFileListRaw)
        {
            Utils.Log($"0x{fileEntry.Key:X} {fileEntry.Value.File} Occurrences: {string.Join(", ", fileEntry.Value)}:X");
            string fileSignature = fileEntry.Key;
            string fileName = fileEntry.Value.File;
            string filePath = Path.Combine(directoryPath, fileName);
            int fileoffset = fileEntry.Value.Offset;
            

            if (File.Exists(filePath))
            {
                // Чтение файла как массива байтов
                byte[] fileData = File.ReadAllBytes(filePath);
                Utils.SigScan(fileSignature, fileName, address => 
                {
                    Utils.LogError($"found {address:X} {fileName}");
                    WriteFile((nuint)address, fileData, fileoffset);
                });
                
                //var address = Utils.SigScan(, (int)Utils.BaseAddress);
                //WriteFile((nuint)address.Offset, fileData, fileoffset);
                //Utils.LogError($"хуйня? {address.Offset}");
                
                
                Utils.LogError($"Файл {fileName} успешно загружен.");
            }
            else
            {
                Utils.LogError($"Файл {fileName} не найден по пути: {filePath}");
            }
        }
        
    }
    public void RecursiveReplace(string pattern, string text, PatternScanResult result, int memOffset=0)
    {   
        Utils.LogError($"{result.Offset + Utils.BaseAddress:X}");
        _memory.SafeWrite((nuint)(result.Offset + Utils.BaseAddress), BitConverter.GetBytes(WriteString(text, Language.English)));
        memOffset = result.Offset + pattern.Replace(" ", "").Length / 2;
        result = Utils.ScanPattern(pattern, memOffset);
        if (result.Found)
        {
            //RecursiveReplace(pattern, text, result, memOffset);
        }
    
    }


    [Function(CallingConventions.Microsoft)]
    private delegate nuint GetNameDelegate(short id);

    [Function(CallingConventions.Microsoft)]
    private delegate nuint GetTextDelegate(int major, int minor);

    public enum Language : int
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