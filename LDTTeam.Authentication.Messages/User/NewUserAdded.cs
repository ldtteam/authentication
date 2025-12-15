using Wolverine;

namespace LDTTeam.Authentication.Messages.User;

public record NewUserAdded(
    Guid Id,
    string UserName
) : IMessage;