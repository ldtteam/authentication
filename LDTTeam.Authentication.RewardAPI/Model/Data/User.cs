using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;

namespace LDTTeam.Authentication.RewardAPI.Model.Data;

[PrimaryKey(nameof(UserId))]
public class User
{
    public required Guid UserId { get; set; }
    public required string Username { get; set; }
    
    public IEnumerable<AssignedReward> Rewards { get; set;  } = Array.Empty<AssignedReward>();
    public IEnumerable<ProviderLogin> Logins { get; set;  } = Array.Empty<ProviderLogin>();
}