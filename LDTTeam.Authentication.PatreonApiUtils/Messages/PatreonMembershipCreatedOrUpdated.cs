using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace LDTTeam.Authentication.PatreonApiUtils.Messages;

public record PatreonMembershipCreatedOrUpdated(Guid UserId, Guid MembershipId) : IMessage;