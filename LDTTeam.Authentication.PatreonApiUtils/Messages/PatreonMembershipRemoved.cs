using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace LDTTeam.Authentication.PatreonApiUtils.Messages;

public record PatreonMembershipRemoved(Guid? MembershipId, Guid UserId) : IMessage;