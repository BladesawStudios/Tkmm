using TkSharp.Core;

namespace Tkmm.Core.Helpers;

public static class TkSdCardTargets
{
    public static IEnumerable<string> EnumerateWriteRoots(string? ephemeralSdCardRootPath = null)
    {
        if (SWITCH)
        {
            yield return "/flash";
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(Config.Shared.EmulatorPath)
                && TkEmulatorHelper.GetSdPath(Config.Shared.EmulatorPath) is { } emulatorSdPath
                && !string.IsNullOrWhiteSpace(emulatorSdPath))
            {
                yield return emulatorSdPath;
            }

            if (!string.IsNullOrWhiteSpace(TkConfig.Shared.SdCardRootPath))
            {
                yield return TkConfig.Shared.SdCardRootPath;
            } 
            else if (!string.IsNullOrWhiteSpace(ephemeralSdCardRootPath))
            {
                yield return ephemeralSdCardRootPath;
            }
        }
    }
    
    public static bool HasWriteRoot(string? ephemeralSdCardRootPath = null) => EnumerateWriteRoots(ephemeralSdCardRootPath).Any();
}