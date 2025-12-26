using Microsoft.EntityFrameworkCore;

namespace LDTTeam.Authentication.PatreonApiUtils.Model.Data;

[PrimaryKey(nameof(MembershipId))]
public class Membership
{
    public required Guid MembershipId { get; set; }
    
    public required long LifetimeCents { get; set; }
    
    public required bool IsGifted { get; set; }
    
    public required DateTime? LastChargeDate { get; set; }
    
    public required bool LastChargeSuccessful { get; set; }

    public required IEnumerable<TierMembership> Tiers { get; set; }
}