using Microsoft.Extensions.Logging;
using Tkmm.Core.Helpers;
using TkSharp;
using TkSharp.Core;
using TkSharp.Core.Models;

namespace Tkmm.Core.WiiXLaunch;

public static class TkWiiXLaunchDeployer
{
    public const string TitleId = "0100F2C0115B6000";

    private const string ExtrasRoot = "extras";
    private const string ModuleExtension = ".wxlm";
    private const string ConfigFileName = "config.ini";
    private const string OptionsFileName = "Options.json";

    public static string GetModsFolder(string sdCardRoot) => Path.Combine(sdCardRoot, "WiiXLaunch", "mods", TitleId);

    public static void DeployToConfiguredTargets(TkProfile? profile = null)
    {
        profile ??= TKMM.ModManager.GetCurrentProfile();

        foreach (var sdCardRoot in TkSdCardTargets.EnumerateWriteRoots())
        {
            try
            {
                Deploy(profile, GetModsFolder(sdCardRoot));
            }
            catch (Exception ex)
            {
                TkLog.Instance.LogError(ex, "Failed to deploy WiiXLaunch modules to '{SdCardRoot}'.", 
                    sdCardRoot);
            }
        }
    }

    public static void Deploy(TkProfile profile, string destinationFolder, Func<string, bool>? targetHasFile = null,
        bool wipeModules = true)
    {
        Directory.CreateDirectory(destinationFolder);
        
        targetHasFile ??= relativePath => File.Exists(Path.Combine(destinationFolder, relativePath));
        
        if (wipeModules) {
            foreach (var stale in Directory.EnumerateFiles(destinationFolder, $"*{ModuleExtension}"))
            {
                File.Delete(stale);
            }
        }
        
        HashSet<string> seededThisRun = new (StringComparer.OrdinalIgnoreCase);

        foreach (var changelog in TkModManager.GetMergeTargets(profile))
        {
            if (changelog.Source is not { } source)
            {
                continue;
            }

            foreach (var relativePath in source.EnumerateFiles(ExtrasRoot))
            {
                if (!IsRootLevelModule(relativePath))
                {
                    continue;
                }
                
                DeployModule(source, relativePath, destinationFolder, targetHasFile, seededThisRun);
            }
        }
    }

    private static void DeployModule(ITkSystemSource source, string relativePath, string destinationFolder,
        Func<string, bool> targetHasFile, HashSet<string> seededThisRun)
    {
        TkWiiXLaunchModule module;

        using (var input = source.OpenRead(relativePath))
        {
            if (!TkWiiXLaunchModule.TryRead(input, relativePath, out module))
            {
                return;
            }
        }
        
        var fileName = Path.GetFileName(relativePath);

        CopyFile(source, relativePath, Path.Combine(destinationFolder, fileName));
        
        DeployResources(source, module.ModId, fileName, destinationFolder, targetHasFile, seededThisRun);
    }

    private static void DeployResources(ITkSystemSource source, string modId, string moduleFileName,
        string destinationFolder, Func<string, bool> targetHasFile, HashSet<string> seededThisRun)
    {
        var resourceRoot = $"{ExtrasRoot}/{modId}";
        var resources = source.EnumerateFiles(resourceRoot).ToArray();

        if (resources.Length == 0)
        {
            WarnOnMisnamedResourceFolder(source, modId, moduleFileName);
            return;
        }

        foreach (var relativePath in resources)
        {
            var name = Path.GetFileName(relativePath);

            if (name.Equals(OptionsFileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            
            var relativeToTarget = Path.Combine(modId, relativePath[(resourceRoot.Length + 1)..].Replace('/', Path.DirectorySeparatorChar));

            if (name.Equals(ConfigFileName, StringComparison.OrdinalIgnoreCase) &&
                !seededThisRun.Contains(relativeToTarget) && targetHasFile(relativeToTarget))
            {
                continue;
            }
            
            CopyFile(source, relativePath, Path.Combine(destinationFolder, relativeToTarget));

            if (name.Equals(ConfigFileName, StringComparison.OrdinalIgnoreCase))
            {
                seededThisRun.Add(relativeToTarget);
            }
        }
    }

    private static void WarnOnMisnamedResourceFolder(ITkSystemSource source, string modId, string moduleFileName)
    {
        var stem = Path.GetFileNameWithoutExtension(moduleFileName);
        
        if (stem.Equals(modId, StringComparison.Ordinal))
        {
            return;
        }
        
        if (source.EnumerateFiles($"{ExtrasRoot}/{stem}").Any()) {
            TkLog.Instance.LogWarning(
                "'{ModuleFileName}' declares the mod id '{ModId}', but its resources are in 'extras/{Stem}/'. " +
                "Rename that folder to 'extras/{ModId}/' or the mod will not find them.",
                moduleFileName, 
                modId, 
                stem, 
                modId);
        }
    }

    private static bool IsRootLevelModule(string relativePath)
    {
        return relativePath.Length > ExtrasRoot.Length + 1 && relativePath.EndsWith(ModuleExtension, StringComparison.OrdinalIgnoreCase) && relativePath.IndexOf('/', ExtrasRoot.Length + 1) < 0;
    }

    private static void CopyFile(ITkSystemSource source, string relativePath, string destination)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

        using var input = source.OpenRead(relativePath);
        using var output = File.Create(destination);
        input.CopyTo(output);
    }
    
}