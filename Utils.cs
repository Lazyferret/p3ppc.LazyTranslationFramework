﻿using p3ppc.unhardcodedNames.Configuration;
using Reloaded.Memory.Interfaces;
using Reloaded.Memory.Sigscan;
using Reloaded.Memory.Sigscan.Definitions.Structs;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace p3ppc.unhardcodedNames;
internal class Utils
{
    private static ILogger _logger;
    private static Config _config;
    public static IStartupScanner _startupScanner;
    public static Scanner scanner;
    internal static nint BaseAddress { get; private set; }

    internal static bool Initialise(ILogger logger, Config config, IModLoader modLoader)
    {
        _logger = logger;
        _config = config;
        using var thisProcess = Process.GetCurrentProcess();
        BaseAddress = thisProcess.MainModule!.BaseAddress;
        scanner = new Scanner(thisProcess, thisProcess.MainModule);
        var startupScannerController = modLoader.GetController<IStartupScanner>();
        if (startupScannerController == null || !startupScannerController.TryGetTarget(out _startupScanner))
        {
            LogError($"Unable to get controller for Reloaded SigScan Library, stuff won't work :(");
            return false;
        }

        return true;

    }

    internal static void LogDebug(string message)
    {
        if (_config.DebugEnabled)
            _logger.WriteLine($"[Unhardcoded Names] {message}");
    }

    internal static void Log(string message)
    {
        _logger.WriteLine($"[Unhardcoded Names] {message}");
    }

    internal static void LogError(string message, Exception e)
    {
        _logger.WriteLine($"[Unhardcoded Names] {message}: {e.Message}", System.Drawing.Color.Red);
    }

    internal static void LogError(string message)
    {
        _logger.WriteLine($"[Unhardcoded Names] {message}", System.Drawing.Color.Red);
    }
    internal static PatternScanResult ScanPattern(string pattern, int offset)
    {
        return scanner.FindPattern(pattern, offset);
    }

    internal static void SigScan(string pattern, string name,Action<nint> action)
    {
        _startupScanner.AddMainModuleScan(pattern, result =>
        {
            if (!result.Found)
            {
                LogError($"Unable to find {name}, stuff won't work :(");
                return;
            }
            LogDebug($"Found {name} at 0x{result.Offset + BaseAddress:X}");
            action(result.Offset + BaseAddress);
        });
    }
    
    

    // Pushes the value of an xmm register to the stack, saving it so it can be restored with PopXmm
    public static string PushXmm(int xmmNum)
    {
        return // Save an xmm register 
            $"sub rsp, 16\n" + // allocate space on stack
            $"movdqu dqword [rsp], xmm{xmmNum}\n";
    }

    // Pushes all xmm registers (0-15) to the stack, saving them to be restored with PopXmm
    public static string PushXmm()
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < 16; i++)
        {
            sb.Append(PushXmm(i));
        }
        return sb.ToString();
    }

    // Pops the value of an xmm register to the stack, restoring it after being saved with PushXmm
    public static string PopXmm(int xmmNum)
    {
        return                 //Pop back the value from stack to xmm
            $"movdqu xmm{xmmNum}, dqword [rsp]\n" +
            $"add rsp, 16\n"; // re-align the stack
    }

    // Pops all xmm registers (0-7) from the stack, restoring them after being saved with PushXmm
    public static string PopXmm()
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 7; i >= 0; i--)
        {
            sb.Append(PopXmm(i));
        }
        return sb.ToString();
    }

    /// <summary>
    /// Gets the address of a global from something that references it
    /// </summary>
    /// <param name="ptrAddress">The address to the pointer to the global (like in a mov instruction or something)</param>
    /// <returns>The address of the global</returns>
    internal static unsafe nuint GetGlobalAddress(nint ptrAddress)
    {
        return (nuint)((*(int*)ptrAddress) + ptrAddress + 4);
    }
}
