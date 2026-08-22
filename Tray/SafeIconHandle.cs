using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace PulseMon.Tray;

internal sealed class SafeIconHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafeIconHandle(nint handle)
        : base(true)
    {
        SetHandle(handle);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(nint hIcon);

    protected override bool ReleaseHandle()
    {
        return DestroyIcon(handle);
    }
}
