using LDTTeam.Authentication.Models.App.User;
using Wolverine;

namespace LDTTeam.Authentication.Messages.User;

public record UserLifetimeContributionIncreased(
    Guid UserId,
    AccountProvider Provider,
    decimal AdditionalContributionAmount) : IMessage;