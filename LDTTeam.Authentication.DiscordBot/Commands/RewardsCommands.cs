using System.ComponentModel;
using System.Drawing;
using JetBrains.Annotations;
using LDTTeam.Authentication.DiscordBot.Extensions;
using LDTTeam.Authentication.DiscordBot.Service;
using LDTTeam.Authentication.Messages.Rewards;
using LDTTeam.Authentication.Models.App.Rewards;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;
using Wolverine;
using IResult = Remora.Results.IResult;

namespace LDTTeam.Authentication.DiscordBot.Commands
{
    [Group("rewards")]
    [UsedImplicitly]
    public class RewardsCommands(
        IInteractionContext interactionContext,
        IMessageBus bus,
        IFeedbackService feedbackService) : CommandGroup
    {
        [Group("user")]
        public class UserRewardsCommands(
            IInteractionContext interactionContext,
            IUserRepository userRepository,
            IAssignedRewardRepository assignedRewardRepository,
            IFeedbackService feedbackService) : CommandGroup
        {
            [Command("list")]
            [Description("List the rewards a user has")]
            [UsedImplicitly]
            public async Task<IResult> ListRewardsForSpecificUser(
                [Description("User to get rewards for")]
                IUser? user)
            {
                var executor = interactionContext.Interaction.Member;
                var executingUser = executor.FlatMap(m => m.User);
                var permissions = executor.FlatMap(m => m.Permissions);
                if (!permissions.HasValue &&
                    user != null &&
                    !(executingUser.HasValue && executingUser.Value.ID.Equals(user.ID)))
                {
                    return await feedbackService.SendContextualErrorAsync(
                        "You are not authorized to run this command for other users"
                    );
                }

                if (permissions.HasValue && executingUser.HasValue
                                         && user != null
                                         && !executingUser.Value.ID.Equals(user.ID)
                                         && !permissions.Value.HasPermission(DiscordPermission.Administrator))
                {
                    return Result.FromError(await feedbackService.SendContextualEmbedAsync(
                        new Embed
                        {
                            Title = "No permission",
                            Description =
                                "You require Administrator permissions to run this command for other users",
                            Colour = Color.Red
                        }));
                }

                if (user == null && !executingUser.HasValue)
                {
                    return Result.FromError(await feedbackService.SendContextualEmbedAsync(
                        new Embed
                        {
                            Title = "No user provided",
                            Description =
                                "You need to supply a user to get rewards for",
                            Colour = Color.Red
                        }));
                }

                if (user == null && executingUser.HasValue)
                {
                    user = executingUser.Value;
                }

                return await ListRewardsFor(user!.ID, user.Username);
            }

            private async Task<IResult> ListRewardsFor(Snowflake discordUserId, string userName)
            {
                var user = await userRepository.GetBySnowflakeAsync(discordUserId);
                if (user == null)
                {
                    return Result.FromError(await feedbackService.SendContextualEmbedAsync(
                        new Embed
                        {
                            Title = "Unknown User",
                            Description =
                                $"The user {userName} is not registered in the authentication system. Ensure they have linked their Discord account.",
                            Colour = Color.Red
                        }));
                }

                var rewards = await assignedRewardRepository.GetForUserAsync(user.UserId);
                var rewardsList = rewards.ToList();

                if (rewardsList.Count == 0)
                {
                    await feedbackService.SendContextualEmbedAsync(
                        new Embed
                        {
                            Title = "No Rewards",
                            Description =
                                $"The user {userName} has no active rewards.",
                            Colour = Color.OrangeRed
                        });
                    return Result.FromSuccess();
                }

                await feedbackService.SendContextualEmbedAsync(
                    new Embed
                    {
                        Title = "Active Rewards",
                        Description =
                            $"The user {userName} has the following rewards active:",
                        Colour = Color.Green,
                        Fields = rewardsList.Select(reward => new EmbedField(
                            reward.Type.ToString(),
                            reward.Reward
                        )).ToList()
                    });
                return Result.FromSuccess();
            }
        }

        [Group("roles")]
        public class RoleRewardsCommands(
            IInteractionContext interactionContext,
            IDiscordRestGuildAPI guildApi,
            IFeedbackService feedbackService,
            IRoleRewardRepository roleRewardRepository,
            DiscordRoleAssignmentService roleAssignmentService) : CommandGroup
        {
            [Command("list")]
            [Description("List the roles associated with a reward")]
            [UsedImplicitly]
            public async Task<IResult> ListRolesForReward(
                [Description("The name of the reward.")]
                string reward)
            {
                var executor = interactionContext.Interaction.Member;
                var permissions = executor.FlatMap(m => m.Permissions);
                if (!permissions.HasValue)
                {
                    return await feedbackService.SendContextualErrorAsync(
                        "This command requires elevated permissions to run"
                    );
                }

                if (permissions.HasValue && !permissions.Value.HasPermission(DiscordPermission.Administrator))
                {
                    return Result.FromError(await feedbackService.SendContextualEmbedAsync(
                        new Embed
                        {
                            Title = "No permission",
                            Description =
                                "You require Administrator permissions to run this command for other users",
                            Colour = Color.Red
                        }));
                }

                if (!interactionContext.Interaction.GuildID.HasValue)
                {
                    return Result.FromError(await feedbackService.SendContextualEmbedAsync(
                        new Embed
                        {
                            Title = "Not in server",
                            Description =
                                "This command can only be run in a server",
                            Colour = Color.Red
                        }));
                }

                var serverId = interactionContext.Interaction.GuildID.Value;
                var roles = await roleRewardRepository.GetRoleForRewardAsync(reward);
                var rolesList = roles
                    .Where(roleAndServer => roleAndServer.Server == serverId)
                    .Select(roleAndServer => roleAndServer.Role)
                    .ToList();

                if (rolesList.Count == 0)
                {
                    await feedbackService.SendContextualEmbedAsync(
                        new Embed
                        {
                            Title = "No Roles",
                            Description =
                                $"The reward '{reward}' of has no roles associated with it.",
                            Colour = Color.OrangeRed
                        });
                    return Result.FromSuccess();
                }

                var rolesRequest = await guildApi.GetGuildRolesAsync(serverId);
                if (!rolesRequest.IsSuccess)
                {
                    return Result.FromError(await feedbackService.SendContextualEmbedAsync(
                        new Embed
                        {
                            Title = "Failed to get roles",
                            Description =
                                $"Could not retrieve roles from the server.",
                            Colour = Color.Red
                        }));
                }

                var roleNames = rolesList
                    .Select(roleId =>
                        rolesRequest.Entity.FirstOrDefault(r => r.ID == roleId)?.Name ?? $"Unknown Role ({roleId})"
                    )
                    .ToList();

                await feedbackService.SendContextualEmbedAsync(
                    new Embed
                    {
                        Title = "Associated Roles",
                        Description =
                            $"The reward '{reward}' of has the following roles associated with it in this server:",
                        Colour = Color.Green,
                        Fields =
                            roleNames
                                .Select(role => new EmbedField(
                                    "Role:",
                                    role.ToString()
                                )).ToList()
                    });
                return Result.FromSuccess();
            }

            [Command("add")]
            [Description("Associate a Discord role with a reward")]
            [UsedImplicitly]
            public async Task<IResult> AddRoleToReward(
                [Description("The name of the reward.")]
                string reward,
                [Description("The Discord role to associate with the reward.")]
                IRole role)
            {
                var executor = interactionContext.Interaction.Member;
                var permissions = executor.FlatMap(m => m.Permissions);
                if (!permissions.HasValue)
                {
                    return await feedbackService.SendContextualErrorAsync(
                        "This command requires elevated permissions to run"
                    );
                }

                if (permissions.HasValue && !permissions.Value.HasPermission(DiscordPermission.Administrator))
                {
                    return Result.FromError(await feedbackService.SendContextualEmbedAsync(
                        new Embed
                        {
                            Title = "No permission",
                            Description =
                                "You require Administrator permissions to run this command for other users",
                            Colour = Color.Red
                        }));
                }

                if (!interactionContext.Interaction.GuildID.HasValue)
                {
                    return Result.FromError(await feedbackService.SendContextualEmbedAsync(
                        new Embed
                        {
                            Title = "Not in server",
                            Description =
                                "This command can only be run in a server",
                            Colour = Color.Red
                        }));
                }

                var serverId = interactionContext.Interaction.GuildID.Value;
                await roleRewardRepository.UpsertAsync(
                    new Model.Data.RoleRewards()
                    {
                        Reward = reward,
                        Role = role.ID,
                        Server = serverId
                    }
                );
                
                await feedbackService.SendContextualEmbedAsync(
                    new Embed
                    {
                        Title = "Role Associated. Now assigning roles to users...",
                        Description =
                            $"The role '{role.Name}' has been associated with the reward '{reward}' in this server.",
                        Colour = Color.YellowGreen
                    });

                var allAssigner = await roleAssignmentService.ForAllMembers();
                await allAssigner.UpdateAllRewards();
                
                await feedbackService.SendContextualEmbedAsync(
                    new Embed
                    {
                        Title = "Role Associated and Assigned",
                        Description =
                            $"The role '{role.Name}' has been associated with the reward '{reward}' in this server.",
                        Colour = Color.Green
                    });
                
                return Result.FromSuccess();
            }

            [Command("remove")]
            [Description("Remove association of a Discord role with a reward")]
            [UsedImplicitly]
            public async Task<IResult> RemoveRoleFromReward(
                [Description("The name of the reward.")]
                string reward,
                [Description("The Discord role to remove association from the reward.")]
                IRole role)
            {
                var executor = interactionContext.Interaction.Member;
                var permissions = executor.FlatMap(m => m.Permissions);
                if (!permissions.HasValue)
                {
                    return await feedbackService.SendContextualErrorAsync(
                        "This command requires elevated permissions to run"
                    );
                }

                if (permissions.HasValue && !permissions.Value.HasPermission(DiscordPermission.Administrator))
                {
                    return Result.FromError(await feedbackService.SendContextualEmbedAsync(
                        new Embed
                        {
                            Title = "No permission",
                            Description =
                                "You require Administrator permissions to run this command for other users",
                            Colour = Color.Red
                        }));
                }

                if (!interactionContext.Interaction.GuildID.HasValue)
                {
                    return Result.FromError(await feedbackService.SendContextualEmbedAsync(
                        new Embed
                        {
                            Title = "Not in server",
                            Description =
                                "This command can only be run in a server",
                            Colour = Color.Red
                        }));
                }

                var serverId = interactionContext.Interaction.GuildID.Value;
                await roleRewardRepository.RemoveAsync(
                    reward,
                    role.ID,
                    serverId
                );
                
                await feedbackService.SendContextualEmbedAsync(
                    new Embed
                    {
                        Title = "Role Association Removed. Now updating roles for users...",
                        Description =
                            $"The role '{role.Name}' has been removed from the reward '{reward}' in this server.",
                        Colour = Color.YellowGreen
                    });

                var allAssigner = await roleAssignmentService.ForAllMembers();
                await allAssigner.UpdateAllRewards();
                
                await feedbackService.SendContextualEmbedAsync(
                    new Embed
                    {
                        Title = "Role Association Removed and Roles Updated",
                        Description =
                            $"The role '{role.Name}' has been removed from the reward '{reward}' in this server.",
                        Colour = Color.Green
                    });
                
                return Result.FromSuccess();
            }
        }

        [Command("upsert")]
        [Description("Update or insert a new reward")]
        [UsedImplicitly]
        public async Task<IResult> UpsertReward(
            [Description("The type of the reward that is updated or added")]
            RewardType type,
            [Description("The name of the reward. Has to be unique within the rewards of the same type.")]
            string reward,
            [Description(
                "The evaluated lambda. Prefixed with \"(tiers, lifetime) => \". Returns true for awarding the reward.")]
            string lambda)
        {
            var executor = interactionContext.Interaction.Member;
            var permissions = executor.FlatMap(m => m.Permissions);
            if (!permissions.HasValue)
            {
                return await feedbackService.SendContextualErrorAsync(
                    "You are not authorized to run this command for other users"
                );
            }

            if (permissions.HasValue && !permissions.Value.HasPermission(DiscordPermission.Administrator))
            {
                return Result.FromError(await feedbackService.SendContextualEmbedAsync(
                    new Embed
                    {
                        Title = "No permission",
                        Description =
                            "You require Administrator permissions to run this command for other users",
                        Colour = Color.Red
                    }));
            }

            return await bus.ExecuteProtectedAsync(
                feedbackService,
                "Failed to upsert reward",
                async b =>
                {
                    await b.PublishAsync(new RewardCreatedOrUpdated(reward, type, lambda));

                    await feedbackService.SendContextualEmbedAsync(
                        new Embed
                        {
                            Title = "Reward Upserted",
                            Description =
                                $"The reward '{reward}' of type '{type}' has been upserted successfully. The changes may take a few minutes to propagate to all services.",
                            Colour = Color.Green
                        });

                    return Result.FromSuccess();
                });
        }

        [Command("remove")]
        [Description("Remove a reward")]
        [UsedImplicitly]
        public async Task<IResult> RemoveReward(
            [Description("The type of the reward that is removed")]
            RewardType type,
            [Description("The name of the reward. Has to be unique within the rewards of the same type.")]
            string reward)
        {
            var executor = interactionContext.Interaction.Member;
            var permissions = executor.FlatMap(m => m.Permissions);
            if (!permissions.HasValue)
            {
                return await feedbackService.SendContextualErrorAsync(
                    "You are not authorized to run this command for other users"
                );
            }

            if (permissions.HasValue && !permissions.Value.HasPermission(DiscordPermission.Administrator))
            {
                return Result.FromError(await feedbackService.SendContextualEmbedAsync(
                    new Embed
                    {
                        Title = "No permission",
                        Description =
                            "You require Administrator permissions to run this command for other users",
                        Colour = Color.Red
                    }));
            }

            return await bus.ExecuteProtectedAsync(
                feedbackService,
                "Failed to remove reward",
                async b =>
                {
                    await b.PublishAsync(new RewardRemoved(reward, type));

                    await feedbackService.SendContextualEmbedAsync(
                        new Embed
                        {
                            Title = "Reward Removed",
                            Description =
                                $"The reward '{reward}' of type '{type}' has been removed successfully. The changes may take a few minutes to propagate to all services.",
                            Colour = Color.Green
                        });

                    return Result.FromSuccess();
                });
        }
    }
}