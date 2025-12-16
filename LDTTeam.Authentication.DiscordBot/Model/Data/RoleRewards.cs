using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;

namespace LDTTeam.Authentication.DiscordBot.Model.Data;

[PrimaryKey(nameof(Reward), nameof(Role), nameof(Server))]
public class RoleRewards
{
    public required string Reward { get; set; }
    public required Snowflake Role { get; set; }
    public required Snowflake Server { get; set; }
}