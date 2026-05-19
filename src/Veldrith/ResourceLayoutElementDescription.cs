using System;

namespace Veldrith;

/// <summary>
/// Describes an individual resource element in a <see cref="ResourceLayout" />.
/// </summary>
public struct ResourceLayoutElementDescription : IEquatable<ResourceLayoutElementDescription> {

    /// <summary>
    /// The name of the element.
    /// </summary>
    public string Name;

    /// <summary>
    /// The kind of resource.
    /// </summary>
    public ResourceKind Kind;

    /// <summary>
    /// The <see cref="ShaderStages" /> in which this element is used.
    /// </summary>
    public ShaderStages Stages;

    /// <summary>
    /// Miscellaneous resource options for this element.
    /// </summary>
    public ResourceLayoutElementOptions Options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceLayoutElementDescription" /> type.
    /// </summary>
    /// <param name="name">The value of name.</param>
    /// <param name="kind">The value of kind.</param>
    /// <param name="stages">The value of stages.</param>
    public ResourceLayoutElementDescription(string name, ResourceKind kind, ShaderStages stages) {
        this.Name = name;
        this.Kind = kind;
        this.Stages = stages;
        this.Options = ResourceLayoutElementOptions.None;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceLayoutElementDescription" /> type.
    /// </summary>
    /// <param name="name">The value of name.</param>
    /// <param name="kind">The value of kind.</param>
    /// <param name="stages">The value of stages.</param>
    /// <param name="options">The value of options.</param>
    public ResourceLayoutElementDescription(string name, ResourceKind kind, ShaderStages stages, ResourceLayoutElementOptions options) {
        this.Name = name;
        this.Kind = kind;
        this.Stages = stages;
        this.Options = options;
    }

    /// <summary>
    /// Performs the Equals operation.
    /// </summary>
    /// <param name="other">The value of other.</param>
    /// <returns>The result of the Equals operation.</returns>
    public bool Equals(ResourceLayoutElementDescription other) {
        return this.Name == other.Name && this.Kind == other.Kind && this.Stages == other.Stages && this.Options == other.Options;
    }

    /// <summary>
    /// Performs the GetHashCode operation.
    /// </summary>
    /// <returns>The result of the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.Name.GetHashCode(), (int)this.Kind, (int)this.Stages, (int)this.Options);
    }
}

/// <summary>
/// Miscellaneous options for an element in a <see cref="ResourceLayout" />.
/// </summary>
[Flags]
public enum ResourceLayoutElementOptions {

    /// <summary>
    /// No special options.
    /// </summary>
    None,

    /// <summary>
    /// Can be applied to a buffer type resource (<see cref="ResourceKind.StructuredBufferReadOnly" />,
    /// <see cref="ResourceKind.StructuredBufferReadWrite" />, or <see cref="ResourceKind.UniformBuffer" />), allowing it
    /// to be
    /// bound with a dynamic offset using <see cref="CommandList.SetGraphicsResourceSet(uint, ResourceSet, uint[])" />.
    /// Offsets specified this way must be a multiple of <see cref="GraphicsDevice.UniformBufferMinOffsetAlignment" /> or
    /// <see cref="GraphicsDevice.StructuredBufferMinOffsetAlignment" />.
    /// </summary>
    DynamicBinding = 1 << 0
}