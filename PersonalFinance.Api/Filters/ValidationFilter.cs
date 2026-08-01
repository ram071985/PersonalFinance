using System.ComponentModel.DataAnnotations;

namespace PersonalFinance.Api.Filters;

/// <summary>
/// Endpoint filter that validates the first argument of type T using
/// System.ComponentModel.DataAnnotations (no third-party packages).
/// Returns a standard ValidationProblem (400) when rules fail.
/// </summary>
public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
            return await next(context);

        var results = new List<ValidationResult>();
        var validationContext = new ValidationContext(argument);

        // Validates [Required], [Range], [MaxLength], etc. + IValidatableObject.Validate()
        var isValid = Validator.TryValidateObject(
            argument,
            validationContext,
            results,
            validateAllProperties: true);

        if (isValid)
            return await next(context);

        var errors = results
            .SelectMany(r =>
                (r.MemberNames.Any() ? r.MemberNames : new[] { string.Empty })
                .Select(member => (Member: member, Message: r.ErrorMessage ?? "Invalid value")))
            .GroupBy(x => x.Member)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Message).Distinct().ToArray());

        return Results.ValidationProblem(errors);
    }
}

public static class ValidationFilterExtensions
{
    public static RouteHandlerBuilder Validate<T>(this RouteHandlerBuilder builder) where T : class =>
        builder.AddEndpointFilter<ValidationFilter<T>>();
}