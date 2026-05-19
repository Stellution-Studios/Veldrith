using System;

namespace Veldrith;

/// <summary>
/// Describes a <see cref="CommandList" />, for creation using a <see cref="ResourceFactory" />.
/// </summary>
public struct CommandListDescription : IEquatable<CommandListDescription> {

    /// <summary>
    /// Executes the Equals operation.
    /// </summary>
    /// <param name="other">Specifies the value of <paramref name="other" />.</param>
    /// <returns>Returns the result produced by the Equals operation.</returns>
    public bool Equals(CommandListDescription other) {
        return true;
    }
}