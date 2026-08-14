using LifeOS.Core.Entities;

namespace LifeOS.Core.Abstractions;

public sealed class XpAwardCommitResult
{
    public XpAwardCommitStatus Status { get; init; }

    public XpTransaction? Transaction { get; init; }

    public UserProgression? Progression { get; init; }
}
