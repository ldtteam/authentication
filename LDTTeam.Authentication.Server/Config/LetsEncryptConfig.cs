using Certes;

namespace LDTTeam.Authentication.Server.Config
{
    public class LetsEncryptConfig
    {
        public bool Enabled { get; set; }
        
        public string Email { get; set; }
        
        public bool Staging { get; set; }
        
        public string Domain { get; set; }
        
        public CsrInfo Csr { get; set; }
    }
}