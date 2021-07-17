using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Modules.Api.Rewards;
using LDTTeam.Authentication.Modules.GitHub.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LDTTeam.Authentication.Modules.GitHub.Condition
{
    public class GitHubCondition : ICondition
    {
        public string ModuleName => "GitHub";

        public string Name => "Default";
        
        public async Task<bool> ExecuteAsync(IServiceScope scope, ConditionInstance instance, string userId, CancellationToken cancellationToken = default)
        {
            GitHubDatabaseContext db = scope.ServiceProvider.GetRequiredService<GitHubDatabaseContext>();

            UserManager<ApplicationUser> userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            ApplicationUser user = await userManager.FindByIdAsync(userId);
            IList<UserLoginInfo> logins = await userManager.GetLoginsAsync(user);

            UserLoginInfo? login = logins.FirstOrDefault(x => x.LoginProvider.ToLower() == "github");
            if (login == null)
                return false;

            // ReSharper disable once SpecifyStringComparison
            IReadOnlyList<string> userTeams = await db.Teams
                .Where(x => x.UserRelationships.Any(y => login.ProviderKey.ToLower() == y.UserId.ToString().ToLower()))
                .Select(x => x.Slug)
                .ToListAsync(cancellationToken);

            ParsingConfig config = new()
            {
                IsCaseSensitive = false
            };
            Expression<Func<IReadOnlyList<string>, bool>> expression =
                DynamicExpressionParser.ParseLambda<IReadOnlyList<string>, bool>(config, true, instance.LambdaString);

            return expression.Compile().Invoke(userTeams);
        }
    }
}