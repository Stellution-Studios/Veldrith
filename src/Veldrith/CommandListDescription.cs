using System;

namespace Veldrith;

/// <summary>
/// Describes a <see cref="CommandList" />, for creation using a <see cref="ResourceFactory" />.
/// </summary>
public struct CommandListDescription : IEquatable<CommandListDescription> {

    /// <summary>
    /// Performs the Equals operation.
    /// </summary>
    /// <param name="other">The value of other.</param>
    /// <returns>The result of the Equals operation.</returns>
    public bool Equals(CommandListDescription other) {
        return true;
    }
}