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
    /// <param name="name">The name used by this operation.</param>
    /// <param name="kind">The kind value used by this operation.</param>
    /// <param name="stages">The stages value used by this operation.</param>
    public ResourceLayoutElementDescription(string name, ResourceKind kind, ShaderStages stages) {
        this.Name = name;
        this.Kind = kind;
        this.Stages = stages;
        this.Options = ResourceLayoutElementOptions.None;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceLayoutElementDescription" /> type.
    /// </summary>
    /// <param name="name">The name used by this operation.</param>
    /// <param name="kind">The kind value used by this operation.</param>
    /// <param name="stages">The stages value used by this operation.</param>
    /// <param name="options">The options used to configure this operation.</param>
    public ResourceLayoutElementDescription(string name, ResourceKind kind, ShaderStages stages, ResourceLayoutElementOptions options) {
        this.Name = name;
        this.Kind = kind;
        this.Stages = stages;
        this.Options = options;
    }

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool Equals(ResourceLayoutElementDescription other) {
        return this.Name == other.Name && this.Kind == other.Kind && this.Stages == other.Stages && this.Options == other.Options;
    }

    /// <summary>
    /// Computes a hash code for this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
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
    /// </summary>
    DynamicBinding = 1 << 0
}