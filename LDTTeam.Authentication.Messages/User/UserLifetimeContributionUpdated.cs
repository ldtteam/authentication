using LDTTeam.Authentication.Models.App.User;
using Wolverine;

namespace LDTTeam.Authentication.Messages.User;

public record UserLifetimeContributionUpdated(
    Guid UserId,
    AccountProvider Provider,
    decimal NewLifetimeContributionAmount) : IMessage;