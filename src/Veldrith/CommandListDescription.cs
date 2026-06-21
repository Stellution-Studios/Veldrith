using System;

namespace Veldrith;

/// <summary>
/// Describes a <see cref="CommandList" />, for creation using a <see cref="ResourceFactory" />.
/// </summary>
public struct CommandListDescription : IEquatable<CommandListDescription> {

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool Equals(CommandListDescription other) {
        return true;
    }
}