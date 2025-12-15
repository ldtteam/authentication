using LDTTeam.Authentication.Models.App.User;
using Wolverine;

namespace LDTTeam.Authentication.Messages.User;

public record ExternalLoginConnectedToUser(
    Guid UserId,
    AccountProvider Provider,
    string ProviderKey
    ) : IMessage;