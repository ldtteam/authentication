using LDTTeam.Authentication.Models.App.Rewards;
using Wolverine;

namespace LDTTeam.Authentication.Messages.User;

public record UserRewardAdded(
    Guid UserId,
    RewardType RewardType,
    string Reward
    ) : IMessage;