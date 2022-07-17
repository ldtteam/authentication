using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api.Logging;
using LDTTeam.Authentication.Modules.Patreon.Data;
using LDTTeam.Authentication.Modules.Patreon.Data.Models;
using LDTTeam.Authentication.Modules.Patreon.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Objects;

namespace LDTTeam.Authentication.Modules.Patreon.EventHandlers
{
    public class PatreonRefreshEventHandler
    {
        private readonly PatreonService _patreonService;
        private readonly PatreonDatabaseContext _db;
        private readonly ILoggingQueue _loggingQueue;
        private readonly ILogger<PatreonRefreshEventHandler> _logger;

        private static readonly SemaphoreSlim Semaphore = new(1, 1);

        public PatreonRefreshEventHandler(PatreonService patreonService, PatreonDatabaseContext db,
            ILogger<PatreonRefreshEventHandler> logger, ILoggingQueue loggingQueue)
        {
            _patreonService = patreonService;
            _db = db;
            _logger = logger;
            _loggingQueue = loggingQueue;
        }

        public async Task ExecuteAsync()
        {
            await Semaphore.WaitAsync();
            try
            {
                List<DbPatreonMember> members = await _db.PatreonMembers.ToListAsync();
                List<string> memberIds = new();

                await foreach ((PatreonService.MemberAttributes memberAttributes,
                    PatreonService.MemberRelationships memberRelationships) in _patreonService.RequestMembers())
                {
                    if (memberIds.Contains(memberRelationships.User.Data.Id))
                        continue;

                    memberIds.Add(memberRelationships.User.Data.Id);

                    DbPatreonMember? member = members.FirstOrDefault(x => x.Id == memberRelationships.User.Data.Id);
                    if (member != null)
                    {
                        member.Lifetime = memberAttributes.LifetimeCents;
                        member.Monthly = memberAttributes.CurrentMonthlyCents;
                        continue;
                    }

                    List<EmbedField> fields = new()
                    {
                        new EmbedField("Id", memberRelationships.User.Data.Id, false),
                        new EmbedField("Lifetime", memberAttributes.LifetimeCents.ToString(), true),
                        new EmbedField("Monthly", memberAttributes.CurrentMonthlyCents.ToString(), true)
                    };

                    await _loggingQueue.QueueBackgroundWorkItemAsync(new Embed
                    {
                        Title = "Patreon Added",
                        Description = "Patreon detected and added to database",
                        Colour = Color.Green,
                        Fields = fields
                    });

                    await _db.PatreonMembers.AddAsync(new DbPatreonMember(memberRelationships.User.Data.Id,
                        memberAttributes.LifetimeCents, memberAttributes.CurrentMonthlyCents));
                }

                foreach (DbPatreonMember member in members.Where(member => memberIds.All(x => x != member.Id)))
                {
                    List<EmbedField> fields = new()
                    {
                        new EmbedField("Id", member.Id, false),
                        new EmbedField("Lifetime", member.Lifetime.ToString(), true),
                        new EmbedField("Monthly", member.Monthly.ToString(), true)
                    };
                    
                    await _loggingQueue.QueueBackgroundWorkItemAsync(new Embed
                    {
                        Title = "Patreon Removed",
                        Description = "Patreon not detected and removed from database",
                        Colour = Color.Red,
                        Fields = fields
                    });

                    _db.Remove(member);
                }

                await _db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Semaphore.Release();
                
                string message = e.Message;
                if (message.Length >= 1024)
                    message = $"{message[..1021]}...";
                
                string? stacktrace = e.StackTrace;
                if (stacktrace?.Length >= 1024)
                    stacktrace = $"{stacktrace[..1021]}...";

                List<EmbedField> fields = new()
                {
                    new EmbedField("Exception Type", nameof(e), false),
                    new EmbedField("Message", message, false),
                    new EmbedField("Stacktrace", stacktrace ?? "null", false)
                };
                
                await _loggingQueue.QueueBackgroundWorkItemAsync(new Embed
                {
                    Title = "Patreon Check Failed!",
                    Description = "Patreon refresh failed to execute!",
                    Colour = Color.Red,
                    Fields = fields
                });

                _logger.LogCritical(e, "Patreon Refresh Failed!");
            }
        }
    }
}