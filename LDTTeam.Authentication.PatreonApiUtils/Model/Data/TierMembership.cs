using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LDTTeam.Authentication.PatreonApiUtils.Model.Data;

[PrimaryKey(nameof(MembershipId), nameof(Tier))]
public class TierMembership
{
    public Guid MembershipId { get; set; } = Guid.Empty;
    public string Tier { get; set; } = null!;
    
    [ForeignKey(nameof(MembershipId))]
    public Membership Membership { get; set; } = null!;
}