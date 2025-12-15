using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;

namespace LDTTeam.Authentication.DiscordBot.Model.Data;

[PrimaryKey(nameof(UserId))]
[Index(nameof(Snowflake), IsUnique = true)]
public class User
{
    public required Guid UserId { get; set; }
    public required Snowflake? Snowflake { get; set; }
    public required string Username { get; set; }
}