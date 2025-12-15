using Wolverine;

namespace LDTTeam.Authentication.Messages.User;

public record UserTiersRemoved(
    Guid UserId,
    List<string> Tiers
    ) : IMessage;