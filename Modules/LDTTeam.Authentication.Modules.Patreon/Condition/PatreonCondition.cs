using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Modules.Api.Rewards;
using LDTTeam.Authentication.Modules.Patreon.Data;
using LDTTeam.Authentication.Modules.Patreon.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LDTTeam.Authentication.Modules.Patreon.Condition
{
    public class PatreonCondition : ICondition
    {
        public string ModuleName => "Patreon";
        public string Name => "Default";

        private record PatreonMember(string Id, long Monthly, long Lifetime);
        
        public async Task<bool> ExecuteAsync(IServiceScope scope, ConditionInstance instance, string userId,
            CancellationToken cancellationToken = default)
        {
            PatreonDatabaseContext db = scope.ServiceProvider.GetRequiredService<PatreonDatabaseContext>();
            
            UserManager<ApplicationUser> userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            ApplicationUser user = await userManager.FindByIdAsync(userId);
            IList<UserLoginInfo> logins = await userManager.GetLoginsAsync(user);

            UserLoginInfo? login = logins.FirstOrDefault(x => x.LoginProvider.ToLower() == "patreon");
            if (login == null)
                return false;

            DbPatreonMember? member = await db.PatreonMembers.FirstOrDefaultAsync(x => x.Id == login.ProviderKey, cancellationToken: cancellationToken);

            if (member == null)
                return false;

            ParsingConfig config = new()
            {
                IsCaseSensitive = false
            };
            Expression<Func<PatreonMember, bool>> expression =
                DynamicExpressionParser.ParseLambda<PatreonMember, bool>(config, true, instance.LambdaString);

            return expression.Compile().Invoke(new PatreonMember(member.Id, member.Monthly, member.Lifetime));
        }
    }
}