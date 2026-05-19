using System;
using System.Runtime.InteropServices;

namespace Veldrith.Android;

/// <summary>
/// Defines the behavior and responsibilities of the AndroidRuntime class.
/// </summary>
internal static class AndroidRuntime {

    /// <summary>
    /// Stores the value associated with <c>_lib_name</c>.
    /// </summary>
    private const string _lib_name = "android.so";

    [DllImport(_lib_name)]

    /// <summary>
    /// Executes the ANativeWindow_fromSurface operation.
    /// </summary>
    /// <param name="jniEnv">Specifies the value of <paramref name="jniEnv" />.</param>
    /// <param name="surface">Specifies the value of <paramref name="surface" />.</param>
    /// <returns>Returns the result produced by the ANativeWindow_fromSurface operation.</returns>
    public static extern IntPtr ANativeWindow_fromSurface(IntPtr jniEnv, IntPtr surface);

    [DllImport(_lib_name)]

    /// <summary>
    /// Executes the ANativeWindow_setBuffersGeometry operation.
    /// </summary>
    /// <param name="aNativeWindow">Specifies the value of <paramref name="aNativeWindow" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    /// <param name="format">Specifies the value of <paramref name="format" />.</param>
    /// <returns>Returns the result produced by the ANativeWindow_setBuffersGeometry operation.</returns>
    public static extern int ANativeWindow_setBuffersGeometry(IntPtr aNativeWindow, int width, int height, int format);

    [DllImport(_lib_name)]

    /// <summary>
    /// Executes the ANativeWindow_release operation.
    /// </summary>
    /// <param name="aNativeWindow">Specifies the value of <paramref name="aNativeWindow" />.</param>
    public static extern void ANativeWindow_release(IntPtr aNativeWindow);
}