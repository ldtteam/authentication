using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Modules.Api.Rewards;
using LDTTeam.Authentication.Modules.Discord.Config;
using Microsoft.Extensions.Configuration;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;

namespace LDTTeam.Authentication.Modules.Discord.Commands
{
    [Group("rewards")]
    public class RewardsCommands : CommandGroup
    {
        private readonly IInteractionContext _context;
        private readonly IFeedbackService    _feedbackService;
        private readonly IConditionService   _conditionService;
        private readonly IRewardService      _rewardService;
        private readonly IConfiguration      _configuration;

        public RewardsCommands(
            IInteractionContext context,
            IFeedbackService    feedbackService,
            IConditionService   conditionService,
            IRewardService      rewardService,
            IConfiguration      configuration
        )
        {
            _context = context;
            _feedbackService = feedbackService;
            _conditionService = conditionService;
            _rewardService = rewardService;
            _configuration = configuration;
        }

        [Command("test")]
        [Description("Test a user's LDTTeam Auth rewards")]
        public async Task<IResult> TestRewardsCommand([Description("User to get rewards for")] IUser user)
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
                try
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    Dictionary<string, List<string>>? rewards =
                        await _conditionService.GetRewardsForProvider("discord", CancellationToken);
                    stopwatch.Stop();

                    if (!rewards.Values.Any(x => x.Contains(user.ID.ToString())))
                    {
                        reply = await _feedbackService.SendContextualEmbedAsync(
                            new Embed
                            {
                                Title = "User not found",
                                Description =
                                    $"User {user.Username} was not found in rewards for provider",
                                Colour = Color.Red
                            });
                    }
                    else
                    {
                        IEnumerable<KeyValuePair<string, List<string>>> list = rewards
                            .Where(x => x.Value.Contains(user.ID.ToString()));

                        StringBuilder builder = new();
                        builder.Append($"Stopwatch db call: {stopwatch.ElapsedMilliseconds}ms\n");

                        foreach (KeyValuePair<string, List<string>> keyValuePair in list)
                        {
                            builder.Append($"TESTING: {keyValuePair.Key} | {keyValuePair.Value.Count}\n");
                            string? val = keyValuePair.Value.FirstOrDefault(x => x == user.ID.ToString());
                            builder.Append($"TESTING 2: {val}\n");
                        }

                        List<string> userRewards = rewards
                            .Where(x => x.Value.Contains(user.ID.ToString()))
                            .Select(x => x.Key)
                            .ToList();

                        foreach (string reward in userRewards)
                        {
                            builder.Append($"reward: {reward}\n");
                        }

                        DiscordConfig? discordConfig = _configuration.GetSection("discord").Get<DiscordConfig>();

                        Dictionary<string, List<Snowflake>> rewardRoles = discordConfig!
                            .RoleMappings[_context.Interaction.GuildID.Value.ToString()]
                            .ToDictionary(
                                x => x.Key,
                                x => x.Value.Select(y => new Snowflake(y)).ToList()
                            );

                        // roles to award
                        List<Snowflake> rewardedRoles = rewardRoles
                            .Where(x => userRewards.Contains(x.Key))
                            .SelectMany(x => x.Value)
                            .Distinct()
                            .Select(x => x)
                            .ToList();

                        foreach (Snowflake reward in rewardedRoles)
                        {
                            builder.Append($"rewarded roles: {reward.ToString()}\n");
                        }

                        // roles not rewarded less rewardedRoles
                        List<Snowflake> notRewardedRoles = rewardRoles
                            .Where(x => !userRewards.Contains(x.Key))
                            .SelectMany(x => x.Value)
                            .Where(x => !rewardedRoles.Contains(x))
                            .Distinct()
                            .Select(x => x)
                            .ToList();

                        foreach (Snowflake reward in notRewardedRoles)
                        {
                            builder.Append($"not rewarded roles: {reward.ToString()}\n");
                        }

                        reply = await _feedbackService.SendContextualEmbedAsync(
                            new Embed
                            {
                                Title = $"{user.Username}'s rewards",
                                Colour = Color.Green,
                                Description = builder.ToString()
                            });
                    }
                }
                catch (Exception e)
                {
                    reply = await _feedbackService.SendContextualEmbedAsync(
                        new Embed
                        {
                            Title = "User not found",
                            Description =
                                $"Comand received exception: {e.Message}",
                            Colour = Color.Red
                        });
                }
            }

            return !reply.IsSuccess
                ? Result.FromError(reply)
                : Result.FromSuccess();
        }

        [Command("user")]
        [Description("Lists a user's LDTTeam Auth rewards")]
        public async Task<IResult> UserRewardsCommand([Description("User to get rewards for")] IUser user)
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
        public async Task<IResult> RemoveRewardCommand([Description("The to be removed reward's ID")] string rewardId)
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
            private readonly IInteractionContext _context;
            private readonly IFeedbackService    _feedbackService;
            private readonly IConditionService   _conditionService;
            private readonly IRewardService      _rewardService;

            public RewardConditionsCommands(
                IInteractionContext context,
                IFeedbackService    feedbackService,
                IConditionService   conditionService,
                IRewardService      rewardService
            )
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
                [Description("Module Lambda")] string lambda
            )
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
                            await _conditionService.AddConditionToReward(rewardId, moduleName, conditionName, lambda,
                                CancellationToken);

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
                string conditionName
            )
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
                            await _conditionService.RemoveConditionFromReward(rewardId, moduleName, conditionName,
                                CancellationToken);

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