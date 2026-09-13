using System.Buffers.Binary;
using System.Text;
using Microsoft.Extensions.Logging;
using TkSharp.Core;

namespace Tkmm.Core.WiiXLaunch;

public readonly record struct TkWiiXLaunchModule(string ModId, ushort FormatVersion)
{
    public const ushort SupportedFormatVersion = 1;

    private static ReadOnlySpan<byte> Magic => "MLXW"u8;

    private const int ModIdOffset = 0x0C;
    private const int ModIdLength = 16;
    private const int PrologueLength = ModIdOffset + ModIdLength;

    public static bool TryRead(Stream input, string sourcePath, out TkWiiXLaunchModule result)
    {
        result = default;

        Span<byte> prologue = stackalloc byte[PrologueLength];
        if (input.ReadAtLeast(prologue, PrologueLength, throwOnEndOfStream: false) < PrologueLength)
        {
            TkLog.Instance.LogWarning("'{Path}' is too short to be a WiiXLaunch module.", sourcePath);
            return false;
        }

        if (!prologue[..Magic.Length].SequenceEqual(Magic))
        {
            TkLog.Instance.LogWarning("'{Path}' is not a WiiXLaunch module (bad magic)", sourcePath);
            return false;
        }

        var formatVersion = BinaryPrimitives.ReadUInt16LittleEndian(prologue[0x04..]);
        if (formatVersion > SupportedFormatVersion)
        {
            TkLog.Instance.LogWarning("'{Path}' declares format version {FormatVersion}; this build of TKMM only supports up to {Supported}. Please update TKMM",
                sourcePath, 
                formatVersion, 
                SupportedFormatVersion);
            return false;
        }

        var modId = prologue.Slice(ModIdOffset, ModIdLength);
        if (modId.IndexOf((byte)0) is var terminator and >= 0)
        {
            modId = modId[..terminator];
        }

        if (modId.IsEmpty)
        {
            TkLog.Instance.LogWarning("'{Path}' declares an empty mod id.",
                sourcePath);
            return false;
        }

        if (modId[0] is (byte)'_')
        {
            TkLog.Instance.LogWarning("'{Path}' declares the reserved mod id '{ModId}'; ids starting with '_' belong to the host.",
                sourcePath, Encoding.ASCII.GetString(modId));
            return false;
        }

        foreach (var value in modId)
        {
            if (value is <= 0x20 or >= 0x7F)
            {
                TkLog.Instance.LogWarning("'{Path}' declares a mod id with unsupported bytes.",
                    sourcePath);
                return false;
            }
        }
        
        var id = Encoding.ASCII.GetString(modId);
        if (id is "." or ".." || id.AsSpan().IndexOfAny('/', '\\', ':') >= 0)
        {
            TkLog.Instance.LogWarning("'{Path}' declares the unusable mod id '{ModId}'",
                sourcePath, id);
            return false;
        }

        result = new TkWiiXLaunchModule(id, formatVersion);
        return true;
    }
}
