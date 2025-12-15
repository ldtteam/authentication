using System.Drawing;
using System.Text;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using IResult = Remora.Results.IResult;

namespace LDTTeam.Authentication.DiscordBot.Extensions;

public static class ObjectExtensions
{
    
    public static async Task<IResult> ExecuteProtectedAsync<T>(this T obj, IFeedbackService feedbackService, string errorHeader, Func<T, Task<IResult>> action)
    {
        try
        {
            return await action(obj);
        }
        catch (Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine("The following exception occured:");
            sb.AppendLine($"```{ex.Message}```");
            if (ex.InnerException != null)
            {
                sb.AppendLine("Inner Exception:");
                sb.AppendLine($"```{ex.InnerException.Message}```");
            }

            return Result.FromError(await feedbackService.SendContextualEmbedAsync(
                new Embed
                {
                    Title = errorHeader,
                    Description = sb.ToString(),
                    Colour = Color.Red
                }));
        }
    }
}