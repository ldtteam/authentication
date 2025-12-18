using Wolverine;

namespace LDTTeam.Authentication.Messages.User;

public record NewUserCreatedOrUpdated(
    Guid Id,
    string UserName
) : IMessage;