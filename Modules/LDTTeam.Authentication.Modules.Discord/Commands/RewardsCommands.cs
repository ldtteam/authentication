using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Modules.Api.Rewards;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;

namespace LDTTeam.Authentication.Modules.Discord.Commands
{
    [Group("rewards")]
    public class RewardsCommands : CommandGroup
    {
        private readonly InteractionContext _context;
        private readonly IDiscordRestWebhookAPI _channelApi;
        private readonly IConditionService _conditionService;
        private readonly IRewardService _rewardService;

        public RewardsCommands(InteractionContext context, IDiscordRestWebhookAPI channelApi,
            IConditionService conditionService, IRewardService rewardService)
        {
            _context = context;
            _channelApi = channelApi;
            _conditionService = conditionService;
            _rewardService = rewardService;
        }

        [Command("user")]
        [Description("Lists a user's LDTTeam Auth rewards")]
        public async Task<Result> UserRewardsCommand([Description("User to get rewards for")]
            IUser user)
        {
            Result<IMessage> reply;
            if (!_context.Member.Value.Permissions.Value.HasPermission(DiscordPermission.Administrator))
            {
                reply = await Reply(new Embed
                {
                    Title = "No Permission",
                    Description =
                        "You require Administrator permissions for this command",
                    Colour = Color.DarkRed
                }, new Optional<IReadOnlyList<IMessageComponent>>());
            }
            else
            {
                Dictionary<string, bool>? rewards =
                    await _conditionService.GetRewardsForUser("discord", user.ID.ToString());

                if (rewards == null)
                {
                    reply = await Reply(new Embed
                    {
                        Title = "User not found",
                        Description =
                            $"User {user.Username} was not found in our system, are you sure they've signed up?",
                        Colour = Color.Red
                    }, new Optional<IReadOnlyList<IMessageComponent>>());
                }
                else
                {
                    List<IEmbedField> fields = new();

                    foreach ((string reward, bool has) in rewards)
                    {
                        fields.Add(new EmbedField(reward, has.ToString(), true));
                    }

                    Embed embed = new()
                    {
                        Title = $"{user.Username}'s rewards",
                        Colour = Color.Green,
                        Fields = fields
                    };

                    reply = await Reply(embed, new Optional<IReadOnlyList<IMessageComponent>>());
                }
            }

            return !reply.IsSuccess
                ? Result.FromError(reply)
                : Result.FromSuccess();
        }

        [Command("add")]
        [Description("Adds new reward")]
        public async Task<Result> AddRewardCommand([Description("The new reward's ID")] string rewardId)
        {
            Result<IMessage> reply;
            if (!_context.Member.Value.Permissions.Value.HasPermission(DiscordPermission.Administrator))
            {
                reply = await Reply(new Embed
                {
                    Title = "No Permission",
                    Description =
                        "You require Administrator permissions for this command",
                    Colour = Color.DarkRed
                }, new Optional<IReadOnlyList<IMessageComponent>>());
            }
            else
            {
                if (await _rewardService.GetReward(rewardId) != null)
                {
                    reply = await Reply(new Embed
                    {
                        Title = "Duplicate Reward",
                        Description =
                            $"Reward {rewardId} already exists in our system",
                        Colour = Color.Red
                    }, new Optional<IReadOnlyList<IMessageComponent>>());
                }
                else
                {
                    await _rewardService.AddReward(rewardId);

                    reply = await Reply(new Embed
                    {
                        Title = "New Reward Added",
                        Description =
                            $"Reward {rewardId} has been added to our system",
                        Colour = Color.Green
                    }, new Optional<IReadOnlyList<IMessageComponent>>());
                }
            }

            return !reply.IsSuccess
                ? Result.FromError(reply)
                : Result.FromSuccess();
        }

        [Command("get")]
        [Description("Gets all rewards or a single reward")]
        public async Task<Result> GetRewardsCommand([Description("An optional reward ID")] string? rewardId = null)
        {
            Result<IMessage> reply;
            if (!_context.Member.Value.Permissions.Value.HasPermission(DiscordPermission.Administrator))
            {
                reply = await Reply(new Embed
                {
                    Title = "No Permission",
                    Description =
                        "You require Administrator permissions for this command",
                    Colour = Color.DarkRed
                }, new Optional<IReadOnlyList<IMessageComponent>>());
            }
            else
            {
                if (rewardId != null)
                {
                    Reward? reward = await _rewardService.GetReward(rewardId);

                    if (reward == null)
                    {
                        reply = await Reply(new Embed
                        {
                            Title = "Missing Reward",
                            Description =
                                $"Reward {rewardId} does not exist in our system",
                            Colour = Color.Red
                        }, new Optional<IReadOnlyList<IMessageComponent>>());
                    }
                    else
                    {
                        List<IEmbedField> fields = new()
                        {
                            new EmbedField("Module", "\u200b", true),
                            new EmbedField("Condition", "\u200b", true),
                            new EmbedField("Lambda", "\u200b", true),
                        };

                        foreach (ConditionInstance condition in reward.Conditions)
                        {
                            fields.Add(new EmbedField("\u200b", condition.ModuleName, true));
                            fields.Add(new EmbedField("\u200b", condition.ConditionName, true));
                            fields.Add(new EmbedField("\u200b", condition.LambdaString, true));
                        }

                        reply = await Reply(new Embed
                        {
                            Title = $"Reward {reward}",
                            Fields = fields,
                            Colour = Color.Green
                        }, new Optional<IReadOnlyList<IMessageComponent>>());
                    }
                }
                else
                {
                    List<IEmbedField> fields =
                        (from reward in await _rewardService.GetRewards()
                            select new EmbedField("\u200b", reward.Id, true))
                        .Cast<IEmbedField>().ToList();

                    reply = await Reply(new Embed
                    {
                        Title = "Rewards",
                        Fields = fields,
                        Colour = Color.Green
                    }, new Optional<IReadOnlyList<IMessageComponent>>());
                }
            }

            return !reply.IsSuccess
                ? Result.FromError(reply)
                : Result.FromSuccess();
        }

        [Command("remove")]
        [Description("Removes a reward")]
        public async Task<Result> RemoveRewardCommand([Description("The to be removed reward's ID")]
            string rewardId)
        {
            Result<IMessage> reply;
            if (!_context.Member.Value.Permissions.Value.HasPermission(DiscordPermission.Administrator))
            {
                reply = await Reply(new Embed
                {
                    Title = "No Permission",
                    Description =
                        "You require Administrator permissions for this command",
                    Colour = Color.DarkRed
                }, new Optional<IReadOnlyList<IMessageComponent>>());
            }
            else
            {
                if (await _rewardService.GetReward(rewardId) == null)
                {
                    reply = await Reply(new Embed
                    {
                        Title = "Missing Reward",
                        Description =
                            $"Reward {rewardId} does not exist in our system",
                        Colour = Color.Red
                    }, new Optional<IReadOnlyList<IMessageComponent>>());
                }
                else
                {
                    await _rewardService.AddReward(rewardId);

                    reply = await Reply(new Embed
                    {
                        Title = "Reward Removed",
                        Description =
                            $"Reward {rewardId} has been removed from our system",
                        Colour = Color.Green
                    }, new Optional<IReadOnlyList<IMessageComponent>>());
                }
            }

            return !reply.IsSuccess
                ? Result.FromError(reply)
                : Result.FromSuccess();
        }

        [Group("conditions")]
        public class RewardConditionsCommands : CommandGroup
        {
            private readonly InteractionContext _context;
            private readonly IDiscordRestWebhookAPI _channelApi;
            private readonly IConditionService _conditionService;
            private readonly IRewardService _rewardService;

            public RewardConditionsCommands(InteractionContext context, IDiscordRestWebhookAPI channelApi,
                IConditionService conditionService, IRewardService rewardService)
            {
                _context = context;
                _channelApi = channelApi;
                _conditionService = conditionService;
                _rewardService = rewardService;
            }

            [Command("add")]
            [Description("Adds condition to reward")]
            public async Task<Result> AddRewardConditionCommand(
                [Description("Reward to add condition to")]
                string rewardId,
                [Description("Module Name for condition")]
                string moduleName,
                [Description("Module's Condition Name")]
                string conditionName,
                [Description("Module Lambda")] string lambda)
            {
                Result<IMessage> reply;
                if (!_context.Member.Value.Permissions.Value.HasPermission(DiscordPermission.Administrator))
                {
                    reply = await Reply(new Embed
                    {
                        Title = "No Permission",
                        Description =
                            "You require Administrator permissions for this command",
                        Colour = Color.DarkRed
                    }, new Optional<IReadOnlyList<IMessageComponent>>());
                }
                else
                {
                    Reward? reward = await _rewardService.GetReward(rewardId);

                    if (reward == null)
                    {
                        reply = await Reply(new Embed
                        {
                            Title = "Missing Reward",
                            Description =
                                $"Reward {rewardId} does not exist in our system",
                            Colour = Color.Red
                        }, new Optional<IReadOnlyList<IMessageComponent>>());
                    }
                    else
                    {
                        try
                        {
                            await _conditionService.AddConditionToReward(rewardId, moduleName, conditionName, lambda);
                            
                            reply = await Reply(new Embed
                            {
                                Title = "Condition Added",
                                Description = "Condition instance was successfully added to reward"
                            }, new Optional<IReadOnlyList<IMessageComponent>>());
                        }
                        catch (AddConditionException e)
                        {
                            reply = await Reply(new Embed
                            {
                                Title = "Add Failed",
                                Description = e.Message,
                                Colour = Color.Red
                            }, new Optional<IReadOnlyList<IMessageComponent>>());
                        }
                    }
                }
                
                return !reply.IsSuccess
                    ? Result.FromError(reply)
                    : Result.FromSuccess();
            }
            
            [Command("remove")]
            [Description("Removes condition from reward")]
            public async Task<Result> RemoveRewardConditionCommand(
                [Description("Reward to remove condition from")]
                string rewardId,
                [Description("Module Name for condition")]
                string moduleName,
                [Description("Module's Condition Name")]
                string conditionName)
            {
                Result<IMessage> reply;
                if (!_context.Member.Value.Permissions.Value.HasPermission(DiscordPermission.Administrator))
                {
                    reply = await Reply(new Embed
                    {
                        Title = "No Permission",
                        Description =
                            "You require Administrator permissions for this command",
                        Colour = Color.DarkRed
                    }, new Optional<IReadOnlyList<IMessageComponent>>());
                }
                else
                {
                    Reward? reward = await _rewardService.GetReward(rewardId);

                    if (reward == null)
                    {
                        reply = await Reply(new Embed
                        {
                            Title = "Missing Reward",
                            Description =
                                $"Reward {rewardId} does not exist in our system",
                            Colour = Color.Red
                        }, new Optional<IReadOnlyList<IMessageComponent>>());
                    }
                    else
                    {
                        try
                        {
                            await _conditionService.RemoveConditionFromReward(rewardId, moduleName, conditionName);
                            
                            reply = await Reply(new Embed
                            {
                                Title = "Condition Remoed",
                                Description = "Condition instance was successfully removed from reward"
                            }, new Optional<IReadOnlyList<IMessageComponent>>());
                        }
                        catch (AddConditionException e)
                        {
                            reply = await Reply(new Embed
                            {
                                Title = "Remove Failed",
                                Description = e.Message,
                                Colour = Color.Red
                            }, new Optional<IReadOnlyList<IMessageComponent>>());
                        }
                    }
                }
                
                return !reply.IsSuccess
                    ? Result.FromError(reply)
                    : Result.FromSuccess();
            }

            private async Task<Result<IMessage>> Reply(Embed embed,
                Optional<IReadOnlyList<IMessageComponent>> components)
            {
                return await _channelApi.CreateFollowupMessageAsync
                (
                    _context.ApplicationID,
                    _context.Token,
                    embeds: new[] {embed},
                    components: components,
                    ct: CancellationToken
                );
            }
        }

        private async Task<Result<IMessage>> Reply(Embed embed, Optional<IReadOnlyList<IMessageComponent>> components)
        {
            return await _channelApi.CreateFollowupMessageAsync
            (
                _context.ApplicationID,
                _context.Token,
                embeds: new[] {embed},
                components: components,
                ct: CancellationToken
            );
        }
    }
}