using DataLake.Application.Abstractions;
using DataLake.Domain.Entities;
using DataLake.Domain.Enums;

namespace DataLake.Classification.Rules;

/// <summary>
/// Placeholder until stored-procedure classification logic is provided.
/// Add rules by implementing IClassificationRule and registering via DI.
/// </summary>
public sealed class RuleBasedClassificationEngine(IEnumerable<IClassificationRule> rules) : IClassificationEngine
{
    public (TransactionType Type, string? Metadata) Classify(Transaction transaction)
    {
        foreach (var rule in rules)
        {
            if (rule.TryMatch(transaction, out var type, out var metadata))
                return (type, metadata);
        }
        return (TransactionType.Standard, null);
    }
}

public interface IClassificationRule
{
    bool TryMatch(Transaction transaction, out TransactionType type, out string? metadata);
}
