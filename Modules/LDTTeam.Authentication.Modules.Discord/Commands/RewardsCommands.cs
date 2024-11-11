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
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace LDTTeam.Authentication.Modules.Discord.Commands
{
    [Group("rewards")]
    public class RewardsCommands : CommandGroup
    {
        private readonly InteractionContext _context;
        private readonly IFeedbackService _feedbackService;
        private readonly IConditionService _conditionService;
        private readonly IRewardService _rewardService;

        public RewardsCommands(InteractionContext context, IFeedbackService feedbackService,
            IConditionService conditionService, IRewardService rewardService)
        {
            _context = context;
            _feedbackService = feedbackService;
            _conditionService = conditionService;
            _rewardService = rewardService;
        }

        [Command("user")]
        [Description("Lists a user's LDTTeam Auth rewards")]
        public async Task<IResult> UserRewardsCommand([Description("User to get rewards for")]
            IUser user)
        {
            var member = _context.Interaction.Member;
            if (!member.HasValue)
            {
                return await _feedbackService.SendContextualErrorAsync(
                    "Command needs a user to run"
                );
            }
            
            var permissions = member.Value.Permissions;
            if (!permissions.HasValue)
            {
                return await _feedbackService.SendContextualErrorAsync(
                    "Command needs a user to run"
                );
            }
            
            Result<IMessage> reply;
            if (!permissions.Value.HasPermission(DiscordPermission.Administrator))
            {
                reply = await _feedbackService.SendContextualEmbedAsync(
                    new Embed
                    {
                        Title = "No Permission",
                        Description =
                            "You require Administrator permissions for this command",
                        Colour = Color.DarkRed
                    });
            }
            else
            {
                Dictionary<string, bool>? rewards =
                    await _conditionService.GetRewardsForUser("discord", user.ID.ToString(), CancellationToken);

                if (rewards == null)
                {
                    reply = await _feedbackService.SendContextualEmbedAsync(
                        new Embed
                        {
                            Title = "User not found",
                            Description =
                                $"User {user.Username} was not found in our system, are you sure they've signed up?",
                            Colour = Color.Red
                        });
                }
                else
                {
                    List<IEmbedField> fields = new();

                    foreach ((string reward, bool has) in rewards)
                    {
                        fields.Add(new EmbedField(reward, has.ToString(), true));
                    }

                    reply = await _feedbackService.SendContextualEmbedAsync(
                        new Embed
                        {
                            Title = $"{user.Username}'s rewards",
                            Colour = Color.Green,
                            Fields = fields
                        });
                }
            }

            return !reply.IsSuccess
                ? Result.FromError(reply)
                : Result.FromSuccess();
        }

        [Command("add")]
        [Description("Adds new reward")]
        public async Task<IResult> AddRewardCommand([Description("The new reward's ID")] string rewardId)
        {
            var member = _context.Interaction.Member;
            if (!member.HasValue)
            {
                return await _feedbackService.SendContextualErrorAsync(
                    "Command needs a user to run"
                );
            }
            
            var permissions = member.Value.Permissions;
            if (!permissions.HasValue)
            {
                return await _feedbackService.SendContextualErrorAsync(
                    "Command needs a user to run"
                );
            }
            
            Result<IMessage> reply;
            if (!permissions.Value.HasPermission(DiscordPermission.Administrator))
            {
                reply = await _feedbackService.SendContextualEmbedAsync(
                    new Embed
                    {
                        Title = "No Permission",
                        Description =
                            "You require Administrator permissions for this command",
                        Colour = Color.DarkRed
                    });
            }
            else
            {
                if (await _rewardService.GetReward(rewardId) != null)
                {
                    reply = await _feedbackService.SendContextualEmbedAsync(
                        new Embed
                        {
                            Title = "Existing Reward",
                            Description =
                                $"Reward {rewardId} already exists in our system",
                            Colour = Color.Red
                        });
                }
                else
                {
                    await _rewardService.AddReward(rewardId);

                    reply = await _feedbackService.SendContextualEmbedAsync(
                        new Embed
                        {
                            Title = "Reward Added",
                            Description =
                                $"Reward {rewardId} has been added to our system",
                            Colour = Color.Green
                        });
                }
            }

            return !reply.IsSuccess
                ? Result.FromError(reply)
                : Result.FromSuccess();
        }

        [Command("get")]
        [Description("Gets all rewards or a single reward")]
        public async Task<IResult> GetRewardsCommand([Description("An optional reward ID")] string? rewardId = null)
        {
            var member = _context.Interaction.Member;
            if (!member.HasValue)
            {
                return await _feedbackService.SendContextualErrorAsync(
                    "Command needs a user to run"
                );
            }
            
            var permissions = member.Value.Permissions;
            if (!permissions.HasValue)
            {
                return await _feedbackService.SendContextualErrorAsync(
                    "Command needs a user to run"
                );
            }
            
            Result<IMessage> reply;
            if (!permissions.Value.HasPermission(DiscordPermission.Administrator))
            {
                reply = await _feedbackService.SendContextualEmbedAsync(
                    new Embed
                    {
                        Title = "No Permission",
                        Description =
                            "You require Administrator permissions for this command",
                        Colour = Color.DarkRed
                    });
            }
            else
            {
                if (rewardId != null)
                {
                    Reward? reward = await _rewardService.GetReward(rewardId);

                    if (reward == null)
                    {
                        reply =  await _feedbackService.SendContextualEmbedAsync(
                            new Embed
                            {
                                Title = "Missing Reward",
                                Description =
                                    $"Reward {rewardId} does not exist in our system",
                                Colour = Color.Red
                            });
                    }
                    else
                    {
                        List<IEmbedField> fields =
                        [
                            new EmbedField("Module", "\u200b", true),
                            new EmbedField("Condition", "\u200b", true),
                            new EmbedField("Lambda", "\u200b", true)
                        ];

                        foreach (ConditionInstance condition in reward.Conditions)
                        {
                            fields.Add(new EmbedField("\u200b", condition.ModuleName, true));
                            fields.Add(new EmbedField("\u200b", condition.ConditionName, true));
                            fields.Add(new EmbedField("\u200b", condition.LambdaString, true));
                        }

                        reply = await _feedbackService.SendContextualEmbedAsync(
                            new Embed
                            {
                                Title = $"Reward {rewardId}",
                                Fields = fields,
                                Colour = Color.Green
                            });
                    }
                }
                else
                {
                    List<IEmbedField> fields =
                        (from reward in await _rewardService.GetRewards()
                            select new EmbedField("\u200b", reward.Id, true))
                        .Cast<IEmbedField>().ToList();

                    reply = await _feedbackService.SendContextualEmbedAsync(
                        new Embed
                        {
                            Title = "Rewards",
                            Fields = fields,
                            Colour = Color.Green
                        });
                }
            }

            return !reply.IsSuccess
                ? Result.FromError(reply)
                : Result.FromSuccess();
        }

        [Command("remove")]
        [Description("Removes a reward")]
        public async Task<IResult> RemoveRewardCommand([Description("The to be removed reward's ID")]
            string rewardId)
        {
            var member = _context.Interaction.Member;
            if (!member.HasValue)
            {
                return await _feedbackService.SendContextualErrorAsync(
                    "Command needs a user to run"
                );
            }
            
            var permissions = member.Value.Permissions;
            if (!permissions.HasValue)
            {
                return await _feedbackService.SendContextualErrorAsync(
                    "Command needs a user to run"
                );
            }
            
            Result<IMessage> reply;
            if (!permissions.Value.HasPermission(DiscordPermission.Administrator))
            {
                reply = await _feedbackService.SendContextualEmbedAsync(
                    new Embed
                    {
                        Title = "No Permission",
                        Description =
                            "You require Administrator permissions for this command",
                        Colour = Color.DarkRed
                    });
            }
            else
            {
                if (await _rewardService.GetReward(rewardId) == null)
                {
                    reply = await _feedbackService.SendContextualEmbedAsync(
                        new Embed
                        {
                            Title = "Missing Reward",
                            Description =
                                $"Reward {rewardId} does not exist in our system",
                            Colour = Color.Red
                        });
                }
                else
                {
                    await _rewardService.AddReward(rewardId);

                    reply = await _feedbackService.SendContextualEmbedAsync(
                        new Embed
                        {
                            Title = "Reward Removed",
                            Description =
                                $"Reward {rewardId} has been removed from our system",
                            Colour = Color.Green
                        });
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
            private readonly IFeedbackService _feedbackService;
            private readonly IConditionService _conditionService;
            private readonly IRewardService _rewardService;

            public RewardConditionsCommands(InteractionContext context, IFeedbackService feedbackService,
                IConditionService conditionService, IRewardService rewardService)
            {
                _context = context;
                _feedbackService = feedbackService;
                _conditionService = conditionService;
                _rewardService = rewardService;
            }

            [Command("add")]
            [Description("Adds condition to reward")]
            public async Task<IResult> AddRewardConditionCommand(
                [Description("Reward to add condition to")]
                string rewardId,
                [Description("Module Name for condition")]
                string moduleName,
                [Description("Module's Condition Name")]
                string conditionName,
                [Description("Module Lambda")] string lambda)
            {
                var member = _context.Interaction.Member;
                if (!member.HasValue)
                {
                    return await _feedbackService.SendContextualErrorAsync(
                        "Command needs a user to run"
                    );
                }
            
                var permissions = member.Value.Permissions;
                if (!permissions.HasValue)
                {
                    return await _feedbackService.SendContextualErrorAsync(
                        "Command needs a user to run"
                    );
                }
                    
                
                Result<IMessage> reply;
                if (!permissions.Value.HasPermission(DiscordPermission.Administrator))
                {
                    reply = await _feedbackService.SendContextualEmbedAsync(
                        new Embed
                        {
                            Title = "No Permission",
                            Description =
                                "You require Administrator permissions for this command",
                            Colour = Color.DarkRed
                        });
                }
                else
                {
                    Reward? reward = await _rewardService.GetReward(rewardId);

                    if (reward == null)
                    {
                        reply = await _feedbackService.SendContextualEmbedAsync(
                            new Embed
                            {
                                Title = "Missing Reward",
                                Description =
                                    $"Reward {rewardId} does not exist in our system",
                                Colour = Color.Red
                            });
                    }
                    else
                    {
                        try
                        {
                            await _conditionService.AddConditionToReward(rewardId, moduleName, conditionName, lambda, CancellationToken);
                            
                            reply = await _feedbackService.SendContextualEmbedAsync(
                                new Embed
                                {
                                    Title = "Condition Added",
                                    Description = "Condition instance was successfully added to reward"
                                });
                        }
                        catch (AddConditionException e)
                        {
                            reply = await _feedbackService.SendContextualEmbedAsync(
                                new Embed
                                {
                                    Title = "Add Failed",
                                    Description = e.Message,
                                    Colour = Color.Red
                                });
                        }
                    }
                }
                
                return !reply.IsSuccess
                    ? Result.FromError(reply)
                    : Result.FromSuccess();
            }
            
            [Command("remove")]
            [Description("Removes condition from reward")]
            public async Task<IResult> RemoveRewardConditionCommand(
                [Description("Reward to remove condition from")]
                string rewardId,
                [Description("Module Name for condition")]
                string moduleName,
                [Description("Module's Condition Name")]
                string conditionName)
            {
                var member = _context.Interaction.Member;
                if (!member.HasValue)
                {
                    return await _feedbackService.SendContextualErrorAsync(
                        "Command needs a user to run"
                    );
                }
            
                var permissions = member.Value.Permissions;
                if (!permissions.HasValue)
                {
                    return await _feedbackService.SendContextualErrorAsync(
                        "Command needs a user to run"
                    );
                }
                    
                
                Result<IMessage> reply;
                if (!permissions.Value.HasPermission(DiscordPermission.Administrator))
                {
                    reply = await _feedbackService.SendContextualEmbedAsync(
                        new Embed
                        {
                            Title = "No Permission",
                            Description =
                                "You require Administrator permissions for this command",
                            Colour = Color.DarkRed
                        });
                }
                else
                {
                    Reward? reward = await _rewardService.GetReward(rewardId);

                    if (reward == null)
                    {
                        reply = await _feedbackService.SendContextualEmbedAsync(
                            new Embed
                            {
                                Title = "Missing Reward",
                                Description =
                                    $"Reward {rewardId} does not exist in our system",
                                Colour = Color.Red
                            });
                    }
                    else
                    {
                        try
                        {
                            await _conditionService.RemoveConditionFromReward(rewardId, moduleName, conditionName, CancellationToken);
                            
                            reply = await _feedbackService.SendContextualEmbedAsync(
                                new Embed
                                {
                                    Title = "Condition Removed",
                                    Description = "Condition instance was successfully removed from reward"
                                });
                        }
                        catch (RemoveConditionException e)
                        {
                            reply = await _feedbackService.SendContextualEmbedAsync(
                                new Embed
                                {
                                    Title = "Remove Failed",
                                    Description = e.Message,
                                    Colour = Color.Red
                                });
                        }
                    }
                }
                
                return !reply.IsSuccess
                    ? Result.FromError(reply)
                    : Result.FromSuccess();
            }
        }
    }
}