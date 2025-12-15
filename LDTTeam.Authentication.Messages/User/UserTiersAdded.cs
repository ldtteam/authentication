using Wolverine;

namespace LDTTeam.Authentication.Messages.User;

public record UserTiersAdded(
    string UserId,
    List<string> Tiers
    ) : IMessage;