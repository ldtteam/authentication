using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LDTTeam.Authentication.PatreonApiUtils.Model.Data;

[PrimaryKey(nameof(MembershipId))]
public class Reward
{
    public required Guid MembershipId { get; set; }
    
    public required long LifetimeCents { get; set; }
    
    public required bool IsGifted { get; set; }
    
    public required DateTime LastSyncDate { get; set; }
    
    public required IEnumerable<RewardMembership> Tiers { get; set; }
 
    [ForeignKey(nameof(MembershipId))]
    public User User { get; set; }
}