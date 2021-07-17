using System;

namespace LDTTeam.Authentication.Modules.Patreon.Data.Models
{
    public class DbToken
    {
        public Guid Id { get; set; }
        
        public string RefreshToken { get; set; }
    }
}