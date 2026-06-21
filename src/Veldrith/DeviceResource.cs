namespace Veldrith;

/// <summary>
/// A resource owned by a <see cref="GraphicsDevice" />, which can be given a string identifier for debugging and
/// </summary>
public interface IDeviceResource {

    /// <summary>
    /// A string identifying this instance. Can be used to differentiate between objects in graphics debuggers and other
    /// </summary>
    string Name { get; set; }
}