using System.Reflection;
using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Application.Behaviors;
using CrossLedgerWeb.Application.Payments;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CrossLedgerWeb.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        // Order matters: logging wraps everything; concurrency-retry wraps unit-of-work
        // so a retry re-runs the handler and SaveChanges together, not SaveChanges alone
        // against now-stale in-memory changes; unit-of-work wraps validation and
        // idempotency so the handler's ledger entries and IdempotencyBehavior's stored
        // response commit in one SaveChanges call; validation runs before idempotency
        // so a malformed request never occupies an idempotency key.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ConcurrencyRetryBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));

        services.AddScoped<IProviderQuoteScorer, ProviderQuoteScorer>();
        services.AddScoped<PaymentRoutingEngine>();
        services.AddScoped<ILoginTokenIssuer, LoginTokenIssuer>();

        return services;
    }
}
