using System.ComponentModel.DataAnnotations.Schema;
using LDTTeam.Authentication.Models.App.User;
using Microsoft.EntityFrameworkCore;

namespace LDTTeam.Authentication.RewardAPI.Model.Data;

[PrimaryKey(nameof(UserId), nameof(Provider), nameof(ProviderUserId))]
public class ProviderLogin
{
    public required Guid UserId { get; set; }
    public required AccountProvider Provider { get; set; }
    public required string ProviderUserId { get; set; }
    
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}