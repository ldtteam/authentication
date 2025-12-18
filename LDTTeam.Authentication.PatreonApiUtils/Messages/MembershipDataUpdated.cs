using Wolverine;

namespace LDTTeam.Authentication.PatreonApiUtils.Messages;

public record MembershipDataUpdated(Guid MembershipId) : IMessage;