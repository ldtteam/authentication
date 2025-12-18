using Microsoft.EntityFrameworkCore;

namespace LDTTeam.Authentication.PatreonApiUtils.Model.Data;

[PrimaryKey(nameof(UserId))]
[Index(nameof(PatreonId), IsUnique = true)]
public class User
{
    public required Guid UserId { get; set; }
    public required string? PatreonId { get; set; }
    public required Guid? MembershipId { get; set; }
    public required string Username { get; set; }
}