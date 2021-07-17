using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api.Webhook;
using LDTTeam.Authentication.Modules.Patreon.Data;
using LDTTeam.Authentication.Modules.Patreon.Data.Models;
using LDTTeam.Authentication.Modules.Patreon.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LDTTeam.Authentication.Modules.Patreon.EventHandlers
{
    public class PatreonRefreshEventHandler
    {
        private readonly PatreonService _patreonService;
        private readonly PatreonDatabaseContext _db;
        private readonly IWebhookQueue _webhookQueue;
        private readonly ILogger<PatreonRefreshEventHandler> _logger;

        public PatreonRefreshEventHandler(PatreonService patreonService, PatreonDatabaseContext db,
            IWebhookQueue webhookQueue, ILogger<PatreonRefreshEventHandler> logger)
        {
            _patreonService = patreonService;
            _db = db;
            _webhookQueue = webhookQueue;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            try
            {
                List<DbPatreonMember> members = await _db.PatreonMembers.ToListAsync();
                List<string> memberIds = new();

                await foreach ((PatreonService.MemberAttributes memberAttributes,
                    PatreonService.MemberRelationships memberRelationships) in _patreonService.RequestMembers())
                {
                    memberIds.Add(memberRelationships.User.Data.Id);

                    DbPatreonMember? member = members.FirstOrDefault(x => x.Id == memberRelationships.User.Data.Id);
                    if (member != null)
                    {
                        member.Lifetime = memberAttributes.LifetimeCents;
                        member.Monthly = memberAttributes.CurrentMonthlyCents;
                        continue;
                    }

                    await _webhookQueue.QueueBackgroundWorkItemAsync(new Embed
                    {
                        Title = "Patreon Added",
                        Description = "Patreon detected and added to database",
                        Color = 3135592,
                        Fields = new List<Embed.Field>
                        {
                            new()
                            {
                                Name = "Id",
                                Value = memberRelationships.User.Data.Id,
                                Inline = false
                            },
                            new()
                            {
                                Name = "Lifetime",
                                Value = memberAttributes.LifetimeCents.ToString(),
                                Inline = true
                            },
                            new()
                            {
                                Name = "Monthly",
                                Value = memberAttributes.CurrentMonthlyCents.ToString(),
                                Inline = true
                            }
                        }
                    });

                    await _db.PatreonMembers.AddAsync(new DbPatreonMember(memberRelationships.User.Data.Id,
                        memberAttributes.LifetimeCents, memberAttributes.CurrentMonthlyCents));
                }

                foreach (DbPatreonMember member in members.Where(member => memberIds.All(x => x != member.Id)))
                {
                    await _webhookQueue.QueueBackgroundWorkItemAsync(new Embed
                    {
                        Title = "Patreon Removed",
                        Description = "Patreon not detected and removed from database",
                        Color = 12788224,
                        Fields = new List<Embed.Field>
                        {
                            new()
                            {
                                Name = "Id",
                                Value = member.Id,
                                Inline = false
                            },
                            new()
                            {
                                Name = "Lifetime",
                                Value = member.Lifetime.ToString(),
                                Inline = true
                            },
                            new()
                            {
                                Name = "Monthly",
                                Value = member.Monthly.ToString(),
                                Inline = true
                            }
                        }
                    });

                    _db.Remove(member);
                }

                await _db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                await _webhookQueue.QueueBackgroundWorkItemAsync(new Embed
                {
                    Title = "Patreon Check Failed!",
                    Description = "Patreon refresh failed to execute!",
                    Color = 16711680,
                    Fields = new List<Embed.Field>
                    {
                        new()
                        {
                            Name = "Exception Type",
                            Value = nameof(e),
                            Inline = true
                        },
                        new()
                        {
                            Name = "Message",
                            Value = e.Message,
                            Inline = true
                        },
                        new()
                        {
                            Name = "Stacktrace",
                            Value = e.StackTrace ?? "null",
                            Inline = false
                        }
                    }
                });
                _logger.LogCritical(e, "Patreon Refresh Failed!");
            }
        }
    }
}