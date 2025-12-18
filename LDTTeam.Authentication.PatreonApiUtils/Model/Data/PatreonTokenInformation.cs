namespace LDTTeam.Authentication.PatreonApiUtils.Model.Data;

public class PatreonTokenInformation
{
    public required int Id { get; set; } = 0;
    public required State State { get; set; } = State.Acquiring;
    public required string AccessToken { get; set; } = string.Empty;
    public required string RefreshToken { get; set; } = string.Empty;
    public required DateTime ExpiresAt { get; set; } = DateTime.MinValue;
}

public enum State
{
    Acquiring,
    Valid,
    Invalid
}