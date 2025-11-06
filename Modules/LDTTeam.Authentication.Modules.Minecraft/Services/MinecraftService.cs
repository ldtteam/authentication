#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
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
        public async Task<string?> GetUuidFromUsername(string username, CancellationToken token)
        {
            //_httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");

            HttpResponseMessage response = await _httpClient.GetAsync(new Uri($"https://api.mojang.com/users/profiles/minecraft/{WebUtility.UrlEncode(username)}"), cancellationToken: token);
            if (!response.IsSuccessStatusCode) return null;
            var body = await response.Content.ReadAsStringAsync(token);
            try
            {
                List<UuidResponseDto>? elements = JsonSerializer.Deserialize<List<UuidResponseDto>?>(body);
                return elements?.FirstOrDefault()?.Id;
            }
            catch (Exception ex)
            {
                try
                {
                    UuidResponseDto? elements = JsonSerializer.Deserialize<UuidResponseDto?>(body);
                    return elements?.Id;
                }
                catch (Exception ex2)
                {
                    throw new InvalidDataException("Failed to deserialize Mojang response", new AggregateException(ex, ex2));
                }
            }
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