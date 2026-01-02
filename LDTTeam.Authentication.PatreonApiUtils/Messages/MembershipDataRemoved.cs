using Wolverine;

namespace LDTTeam.Authentication.PatreonApiUtils.Messages;

public record MembershipDataRemoved(Guid OldMembershipId, Guid UserId) : IMessage;