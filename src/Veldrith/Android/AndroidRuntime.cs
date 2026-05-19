using System;
using System.Runtime.InteropServices;

namespace Veldrith.Android;

/// <summary>
/// Represents the AndroidRuntime class.
/// </summary>
internal static class AndroidRuntime {

    /// <summary>
    /// Represents the _lib_name field.
    /// </summary>
    private const string _lib_name = "android.so";

    [DllImport(_lib_name)]

    /// <summary>
    /// Performs the ANativeWindow_fromSurface operation.
    /// </summary>
    /// <param name="jniEnv">The value of jniEnv.</param>
    /// <param name="surface">The value of surface.</param>
    /// <returns>The result of the ANativeWindow_fromSurface operation.</returns>
    public static extern IntPtr ANativeWindow_fromSurface(IntPtr jniEnv, IntPtr surface);

    [DllImport(_lib_name)]

    /// <summary>
    /// Performs the ANativeWindow_setBuffersGeometry operation.
    /// </summary>
    /// <param name="aNativeWindow">The value of aNativeWindow.</param>
    /// <param name="width">The value of width.</param>
    /// <param name="height">The value of height.</param>
    /// <param name="format">The value of format.</param>
    /// <returns>The result of the ANativeWindow_setBuffersGeometry operation.</returns>
    public static extern int ANativeWindow_setBuffersGeometry(IntPtr aNativeWindow, int width, int height, int format);

    [DllImport(_lib_name)]

    /// <summary>
    /// Performs the ANativeWindow_release operation.
    /// </summary>
    /// <param name="aNativeWindow">The value of aNativeWindow.</param>
    public static extern void ANativeWindow_release(IntPtr aNativeWindow);
}