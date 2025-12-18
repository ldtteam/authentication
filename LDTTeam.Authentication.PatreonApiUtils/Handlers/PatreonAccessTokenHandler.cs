using LDTTeam.Authentication.PatreonApiUtils.Messages;
using LDTTeam.Authentication.PatreonApiUtils.Service;

namespace LDTTeam.Authentication.PatreonApiUtils.Handlers;

public class PatreonAccessTokenHandler (IPatreonTokenService patreonTokenService)
{
    public async Task Handle(PatreonTokenUpdated message)
    {
        await patreonTokenService.ReloadTokenAsync();
    }
}