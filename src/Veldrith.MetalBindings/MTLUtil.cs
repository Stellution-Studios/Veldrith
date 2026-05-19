using System.Text;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the behavior and responsibilities of the MTLUtil class.
/// </summary>
public static class MTLUtil {

    /// <summary>
    /// Executes the GetUtf8String operation.
    /// </summary>
    /// <param name="stringStart">Specifies the value of <paramref name="stringStart" />.</param>
    /// <returns>Returns the result produced by the GetUtf8String operation.</returns>
    public static unsafe string GetUtf8String(byte* stringStart) {
        int characters = 0;
        while (stringStart[characters] != 0) {
            characters++;
        }

        return Encoding.UTF8.GetString(stringStart, characters);
    }

    public static T AllocInit<T>(string typeName) where T : struct {
        ObjCClass cls = new(typeName);
        return cls.AllocInit<T>();
    }
}