using Wolverine;

namespace LDTTeam.Authentication.Messages.User;

public record UserLifetimeContributionIncreased(
    Guid UserId,
    decimal AdditionalContributionAmount) : IMessage;