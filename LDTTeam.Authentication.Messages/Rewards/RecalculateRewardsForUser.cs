using Wolverine;

namespace LDTTeam.Authentication.Messages.Rewards;

public record RecalculateRewardsForUser(Guid UserId) : IMessage;