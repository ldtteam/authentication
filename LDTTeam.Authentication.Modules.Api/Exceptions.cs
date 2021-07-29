using System;

namespace LDTTeam.Authentication.Modules.Api
{
    public class AddConditionException : Exception
    {
        public AddConditionException(string? message) : base(message)
        {
        }
    }
    
    public class RemoveConditionException : Exception
    {
        public RemoveConditionException(string? message) : base(message)
        {
        }
    }
}