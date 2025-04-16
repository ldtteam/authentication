using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Modules.Api.Logging;
using Remora.Discord.API.Objects;

namespace LDTTeam.Authentication.Server.Tasks;

public class StartupAnnouncementTask(ILoggingQueue queue) : IStartupTask
{
    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Create the embed for the startup announcement
        Embed embed = new Embed
        {
            Title = "Patreon API Manager Started",
            Description = "Started version: " + Assembly.GetEntryAssembly()?.GetName().Version,
            Colour = Color.GreenYellow,
        };

        // Queue the embed for logging
        return queue.QueueBackgroundWorkItemAsync(embed);
    }
}