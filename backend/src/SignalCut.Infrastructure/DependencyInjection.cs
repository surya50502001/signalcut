using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SignalCut.Application.Interfaces;
using SignalCut.Application.Services;
using SignalCut.Infrastructure.Persistence;
using SignalCut.Infrastructure.Providers.LLM;
using SignalCut.Infrastructure.Providers.Payments;
using SignalCut.Infrastructure.Providers.Publishing;
using SignalCut.Infrastructure.Providers.Search;
using SignalCut.Infrastructure.Providers.Storage;
using SignalCut.Infrastructure.Providers.Transcription;
using SignalCut.Infrastructure.Providers.Video;
using SignalCut.Infrastructure.Security;

namespace SignalCut.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Database Configuration
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? configuration["DATABASE_URL"];
        if (!string.IsNullOrEmpty(connectionString) && !connectionString.Contains("InMemory"))
        {
            services.AddDbContext<SignalCutDbContext>(options =>
                options.UseNpgsql(connectionString, b => b.MigrationsAssembly(typeof(SignalCutDbContext).Assembly.FullName)));
        }
        else
        {
            services.AddDbContext<SignalCutDbContext>(options =>
                options.UseInMemoryDatabase("SignalCutDb"));
        }

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<SignalCutDbContext>());

        // 2. HttpClient
        services.AddHttpClient();

        // 3. Security & Auth
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<ITokenEncryptionService, AesTokenEncryptionService>();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddScoped<IAuthService, AuthService>();

        // 4. Search Providers
        services.AddScoped<ISourceProvider, YouTubeSourceProvider>();
        services.AddScoped<ISourceProvider, PodcastSourceProvider>();
        services.AddScoped<ISourceProvider, WebVideoSourceProvider>();
        services.AddScoped<ISourceProvider, UserUploadMediaProvider>();
        services.AddScoped<ISearchProvider, AggregatedSearchProvider>();

        // 5. Transcripts & Media Security
        services.AddScoped<ITranscriptProvider, TranscriptProvider>();
        services.AddScoped<ISpeechToTextProvider, SpeechToTextProvider>();
        services.AddScoped<IMediaProvider, MediaSecurityValidator>();

        // 6. LLM & AI Analysis
        services.AddScoped<ILanguageModelProvider, CompositeLanguageModelProvider>();

        // 7. Video Rendering Engine
        services.AddScoped<IVideoProcessor, CompositeVideoProcessor>();

        // 8. Storage
        services.AddScoped<IObjectStorage, LocalStorageProvider>();

        // 9. Payments (Stripe & Razorpay)
        services.AddScoped<IPaymentProvider, StripePaymentProvider>();
        services.AddScoped<IPaymentProvider, RazorpayPaymentProvider>();

        // 10. Social Publishing Providers
        services.AddScoped<IPublishingProvider, YouTubePublishingProvider>();
        services.AddScoped<IPublishingProvider, LinkedInPublishingProvider>();
        services.AddScoped<IPublishingProvider, InstagramPublishingProvider>();
        services.AddScoped<IPublishingProvider, TikTokPublishingProvider>();
        services.AddScoped<IPublishingProvider, XPublishingProvider>();

        // 11. Core Application Domain Services
        services.AddScoped<ICreditWalletService, CreditWalletService>();
        services.AddScoped<IRightsAuthorizationService, RightsAuthorizationService>();
        services.AddScoped<ISearchDiscoveryService, SearchDiscoveryService>();
        services.AddScoped<IMomentService, MomentService>();
        services.AddScoped<IClipService, ClipService>();
        services.AddScoped<IJobService, JobService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IPublishingService, PublishingService>();
        services.AddScoped<IAdminService, AnalyticsAndAdminService>();
        services.AddScoped<IAnalyticsService, AnalyticsAndAdminService>();

        return services;
    }
}
