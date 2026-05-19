using System;
using System.Runtime.InteropServices;

namespace Veldrith.Android;

internal static class AndroidRuntime {

    /// <summary>
    /// Represents the _lib_name field.
    /// </summary>
    private const string _lib_name = "android.so";

    [DllImport(_lib_name)]

    /// <summary>
    /// Executes ANativeWindow_fromSurface.
    /// </summary>
    public static extern IntPtr ANativeWindow_fromSurface(IntPtr jniEnv, IntPtr surface);

    [DllImport(_lib_name)]

    /// <summary>
    /// Executes ANativeWindow_setBuffersGeometry.
    /// </summary>
    public static extern int ANativeWindow_setBuffersGeometry(IntPtr aNativeWindow, int width, int height, int format);

    [DllImport(_lib_name)]

    /// <summary>
    /// Executes ANativeWindow_release.
    /// </summary>
    public static extern void ANativeWindow_release(IntPtr aNativeWindow);
}