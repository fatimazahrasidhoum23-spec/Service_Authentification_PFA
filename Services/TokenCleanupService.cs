using AuthService.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Services
{
    public class TokenCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TokenCleanupService> _logger;

        public TokenCleanupService(IServiceScopeFactory scopeFactory, ILogger<TokenCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

                // Supprime uniquement les tokens expirés
                var expiredTokens = await context.Tokens
                    .Where(t => t.RefreshTokenExpiryTime <= DateTime.UtcNow)
                    .ToListAsync(stoppingToken);

                if (expiredTokens.Any())
                {
                    context.Tokens.RemoveRange(expiredTokens);
                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation($"{expiredTokens.Count} token(s) expirés supprimés.");
                }

                // Tourne toutes les 24h
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}