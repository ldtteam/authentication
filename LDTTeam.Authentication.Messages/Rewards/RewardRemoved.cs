using LDTTeam.Authentication.Models.App.Rewards;
using Wolverine;

namespace LDTTeam.Authentication.Messages.Rewards;

public record RewardRemoved(string Reward, RewardType Type) : IMessage;