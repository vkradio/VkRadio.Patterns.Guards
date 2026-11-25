using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace VkRadio.Patterns.Guards;

public interface IGuardClause
{
}

public interface IForbiddingGuardClause : IGuardClause
{
}

public interface IAllowingGuardClause : IGuardClause
{
}

public class GuardRule : IForbiddingGuardClause, IAllowingGuardClause
{
    private GuardRule()
    {
    }

    public static IForbiddingGuardClause Forbid { get; } = new GuardRule();

    public static IAllowingGuardClause Allow { get; } = new GuardRule();
}

public static class GuardRuleExtensions
{
    // SQL Server datetime range: Jan 1, 1753 to Dec 31, 9999
    private static readonly DateTime SqlMinDate = new(1753, 1, 1);
    private static readonly DateTime SqlMaxDate = new(9999, 12, 31, 23, 59, 59, 997);

    /// <summary>
    ///  Null checks
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_"></param>
    /// <param name="input"></param>
    /// <param name="parameterName"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static T Null<T>(
        this IForbiddingGuardClause _,
        [NotNull] T? input,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null,
        string? message = null
    )
    {
        if (input is null)
        {
            throw new ArgumentNullException(parameterName, message ?? $"Value cannot be null. (Parameter '{parameterName}')");
        }

        return input;
    }

    public static string NullOrEmpty(
        this IForbiddingGuardClause _,
        [NotNull] string? input,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null,
        string? message = null
    )
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentException(message ?? $"Required input {parameterName} was empty.", parameterName);
        }

#pragma warning disable CS8777 // Parameter must have a non-null value when exiting.
        return input!; // some compiler/analyzer glitch
#pragma warning restore CS8777 // Parameter must have a non-null value when exiting.
    }

    public static string NullOrWhiteSpace(
        this IForbiddingGuardClause _,
        [NotNull] string? input,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null,
        string? message = null
    )
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException(message ?? $"Required input {parameterName} was empty.", parameterName);
        }

#pragma warning disable CS8777 // Parameter must have a non-null value when exiting.
        return input!; // some compiler/analyzer glitch
#pragma warning restore CS8777 // Parameter must have a non-null value when exiting.
    }

    public static IEnumerable<T> NullOrEmpty<T>(
        this IForbiddingGuardClause _,
        [NotNull] IEnumerable<T>? input,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null,
        string? message = null
    )
    {
        if (input is null || !input.Any())
        {
            throw new ArgumentException(message ?? $"Required input {parameterName} was empty.", parameterName);
        }

        return input;
    }

    public static DateTime SqlDateRange(
        this IAllowingGuardClause _,
        DateTime input,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null,
        string? message = null
    )
    {
        if (input < SqlMinDate || input > SqlMaxDate)
        {
            throw new ArgumentOutOfRangeException(parameterName, message ?? $"Input {parameterName} was out of range.");
        }

        return input;
    }

    // Generic Range check (Works for int, decimal, double, DateTime, DateTimeOffset, TimeSpan, etc.)
    public static T Range<T>(
        this IAllowingGuardClause _,
        T input,
        T rangeFrom,
        T rangeTo,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null,
        string? message = null
    ) where T : IComparable<T>
    {
        if (rangeFrom.CompareTo(rangeTo) > 0)
        {
            throw new ArgumentException($"{nameof(rangeFrom)} should be less or equal than {nameof(rangeTo)}", nameof(rangeFrom));
        }

        if (input.CompareTo(rangeFrom) < 0 || input.CompareTo(rangeTo) > 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, message ?? $"Input {parameterName} was out of range.");
        }

        return input;
    }

    // Numeric checks (Compatible with .NET Standard 2.0 via IComparable<T> without INumber<T>)
    public static T Zero<T>(
        this IForbiddingGuardClause _,
        T input,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null,
        string? message = null
    ) where T : struct, IComparable<T>
    {
        if (input.CompareTo(default) == 0)
        {
            throw new ArgumentException(message ?? $"Required input {parameterName} cannot be zero.", parameterName);
        }

        return input;
    }

    public static T Negative<T>(
        this IForbiddingGuardClause _,
        T input,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null,
        string? message = null
    ) where T : struct, IComparable<T>
    {
        if (input.CompareTo(default) < 0)
        {
            throw new ArgumentException(message ?? $"Required input {parameterName} cannot be negative.", parameterName);
        }

        return input;
    }

    public static T NegativeOrZero<T>(
        this IForbiddingGuardClause _,
        T input,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null,
        string? message = null
    ) where T : struct, IComparable<T>
    {
        if (input.CompareTo(default) <= 0)
        {
            throw new ArgumentException(message ?? $"Required input {parameterName} cannot be zero or negative.", parameterName);
        }

        return input;
    }

    // Common Domain & Format checks
    public static T Default<T>(
        this IForbiddingGuardClause _,
        T input,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null,
        string? message = null
    )
    {
        if (EqualityComparer<T>.Default.Equals(input, default!))
        {
            throw new ArgumentException(message ?? $"Parameter [{parameterName}] is default value for type {typeof(T).Name}", parameterName);
        }

        return input;
    }

    public static string InvalidFormat(
        this IForbiddingGuardClause _,
        string input,
        string regexPattern,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null,
        string? message = null
    )
    {
        if (!Regex.IsMatch(input, regexPattern))
        {
            throw new ArgumentException(message ?? $"Input {parameterName} was not in required format.", parameterName);
        }

        return input;
    }

    public static T EnumRange<T>(
        this IAllowingGuardClause _,
        T input,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null,
        string? message = null
    ) where T : struct, Enum
    {
        if (!Enum.IsDefined(typeof(T), input))
        {
            throw new ArgumentOutOfRangeException(parameterName, message ?? $"Required enum value {input} is not defined in {typeof(T).Name}.");
        }

        return input;
    }

    public static T Expression<T>(
        this IForbiddingGuardClause _,
        Func<T, bool> func,
        T input,
        string message,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null)
    {
        if (func(input))
        {
            throw new ArgumentException(message, parameterName);
        }

        return input;
    }

    public static T Expression<T>(
        this IAllowingGuardClause _,
        Func<T, bool> func,
        T input,
        string message,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null)
    {
        if (!func(input))
        {
            throw new ArgumentException(message, parameterName);
        }

        return input;
    }
}
