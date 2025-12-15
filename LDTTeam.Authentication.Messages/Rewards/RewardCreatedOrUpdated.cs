using LDTTeam.Authentication.Models.App.Rewards;
using Wolverine;

namespace LDTTeam.Authentication.Messages.Rewards;

public record RewardCreatedOrUpdated(string Reward, RewardType Type, string Lambda) : IMessage;