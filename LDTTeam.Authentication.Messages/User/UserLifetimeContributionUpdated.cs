using Wolverine;

namespace LDTTeam.Authentication.Messages.User;

public record UserLifetimeContributionUpdated(
    Guid UserId,
    decimal NewLifetimeContributionAmount) : IMessage;