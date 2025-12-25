using Wolverine;

namespace LDTTeam.Authentication.Messages.User;

public record UserDeleted(Guid Id) : IMessage;