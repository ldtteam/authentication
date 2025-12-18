using LDTTeam.Authentication.Models.App.User;
using Wolverine;

namespace LDTTeam.Authentication.Messages.User;

public record UserTiersRemoved(
    Guid UserId,
    AccountProvider Provider,
    List<string> Tiers
    ) : IMessage;