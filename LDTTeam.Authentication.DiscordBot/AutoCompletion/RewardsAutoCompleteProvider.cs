using LDTTeam.Authentication.DiscordBot.Model.Data;
using LDTTeam.Authentication.DiscordBot.Service;
using LDTTeam.Authentication.Models.App.Rewards;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Autocomplete;

namespace LDTTeam.Authentication.DiscordBot.AutoCompletion;

public class RewardsAutoCompleteProvider(IRewardRepository repository) : IAutocompleteProvider<string>
{
    public const string ProviderIdentity = "RewardsAutoCompleteProvider";

    public virtual string Identity => ProviderIdentity;

    public async ValueTask<IReadOnlyList<IApplicationCommandOptionChoice>> GetSuggestionsAsync(IReadOnlyList<IApplicationCommandInteractionDataOption> options, string userInput, CancellationToken ct = new CancellationToken())
    {
        var rewards = await GetOptionsAsync(ct);
        var filteredRewards = rewards
            .Where(r => r.Name.Contains(userInput, StringComparison.OrdinalIgnoreCase))
            .Take(25)
            .Select(r => new ApplicationCommandOptionChoice(r.Name, r.Name))
            .ToList()
            .AsReadOnly();

        return filteredRewards;
    }

    protected virtual Task<IEnumerable<Reward>> GetOptionsAsync(CancellationToken ct)
    {
        return repository.GetAllAsync(ct);
    }
}

public class DiscordRoleRewardsAutoCompleteProvider(IRewardRepository repository) : RewardsAutoCompleteProvider(repository)
{
    public const string ProviderIdentity = "DiscordRoleRewardsAutoCompleteProvider";

    public override string Identity => ProviderIdentity;

    protected override async Task<IEnumerable<Reward>> GetOptionsAsync(CancellationToken ct)
    {
        var result = await base.GetOptionsAsync(ct);
        return result.Where(r => r.Type == RewardType.DiscordRole);
    }
}