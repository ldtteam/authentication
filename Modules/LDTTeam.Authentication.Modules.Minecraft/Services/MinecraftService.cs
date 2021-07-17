#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace LDTTeam.Authentication.Modules.Minecraft.Services
{
    public class MinecraftService
    {
        private readonly HttpClient _httpClient;

        public MinecraftService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public record UuidResponseDto(string Id, string Name);

        // ReSharper disable once MemberCanBeMadeStatic.Global
        public async Task<string?> GetUuidFromUsername(string username)
        {
            //_httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");

            HttpResponseMessage response = await _httpClient.PostAsJsonAsync(new Uri("https://api.mojang.com/profiles/minecraft"), new[] {username});
            
            if (!response.IsSuccessStatusCode) return null;

            List<UuidResponseDto>? elements = await response.Content.ReadFromJsonAsync<List<UuidResponseDto>>();
            return elements?.FirstOrDefault()?.Id;
        }
        
        public record UsernameResponseDto(string Id, string Name);

        public async Task<string?> GetUsernameFromUuid(Guid uuid)
        {
            //_httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
            
            HttpResponseMessage response = await _httpClient.GetAsync(new Uri($"https://sessionserver.mojang.com/session/minecraft/profile/{uuid:N}"));

            if (!response.IsSuccessStatusCode) return null;

            UsernameResponseDto? element = await response.Content.ReadFromJsonAsync<UsernameResponseDto>();

            return element?.Name;
        }
    }

}