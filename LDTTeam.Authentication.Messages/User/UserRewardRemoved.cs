using LDTTeam.Authentication.Models.App.Rewards;
using Wolverine;

namespace LDTTeam.Authentication.Messages.User;

public record UserRewardRemoved(
    Guid UserId,
    RewardType RewardType,
    string Reward
    ) : IMessage;