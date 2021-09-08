using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LDTTeam.Authentication.Server.Data;
using LDTTeam.Authentication.Server.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LDTTeam.Authentication.Server.Services
{
    public class MetricsHistoryService : BackgroundService
    {
        private readonly IServiceProvider _provider;

        public MetricsHistoryService(IServiceProvider provider)
        {
            _provider = provider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                DateTime now = DateTime.Now;
                DateTime time = DateTime.Today + new TimeSpan(now.Hour + 1, 0, 0);

                Console.WriteLine($"Recording history at: {time}");
                await Task.Delay(time - now, stoppingToken); // every hour on the hour

                using IServiceScope scope = _provider.CreateScope();
                await using DatabaseContext context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

                List<HistoricalEndpointMetric> historicalMetrics = (await context.Metrics.ToListAsync(stoppingToken))
                    .Select(metric => new HistoricalEndpointMetric
                        {
                            Metric = metric,
                            DateTime = DateTimeOffset.Now,
                            Count = metric.Count
                        }
                    ).ToList();

                await context.HistoricalMetrics.AddRangeAsync(historicalMetrics, stoppingToken);
                await context.SaveChangesAsync(stoppingToken);
            }
        }
    }
}