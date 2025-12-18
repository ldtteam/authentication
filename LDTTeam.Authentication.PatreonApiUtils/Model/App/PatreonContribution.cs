namespace LDTTeam.Authentication.PatreonApiUtils.Model.App;

public struct PatreonContribution
{
    public required string PatreonId { get; init; }
    public required Guid MembershipId { get; init; }
    public required long LifetimeCents { get; init; }
    public required bool IsGifted { get; init; }
    public required DateTime LastChargeDate { get; init; }
    public required bool LastChargeSuccessful { get; init; }
    public required IEnumerable<string> Tiers { get; init; }
}
