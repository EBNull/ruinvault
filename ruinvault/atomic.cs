using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ruinvault;

[Flags]
enum MoveFileFlags
{
    MOVEFILE_REPLACE_EXISTING = 0x00000001,
    MOVEFILE_COPY_ALLOWED = 0x00000002,
    MOVEFILE_DELAY_UNTIL_REBOOT = 0x00000004,
    MOVEFILE_WRITE_THROUGH = 0x00000008,
    MOVEFILE_CREATE_HARDLINK = 0x00000010,
    MOVEFILE_FAIL_IF_NOT_TRACKABLE = 0x00000020
}

class Atomic
{
    [return: MarshalAs(UnmanagedType.Bool)]
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName,
       MoveFileFlags dwFlags);

    public static bool WriteFile(string filename, string contents)
    {
        var tf = PathUtil.PrefixBasename(filename, "_temp_");
        File.WriteAllText(tf, contents);
        return MoveFileEx(tf, filename, MoveFileFlags.MOVEFILE_REPLACE_EXISTING);
    }
}
