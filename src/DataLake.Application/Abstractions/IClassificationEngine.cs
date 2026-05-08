using DataLake.Domain.Entities;
using DataLake.Domain.Enums;

namespace DataLake.Application.Abstractions;

public interface IClassificationEngine
{
    (TransactionType Type, string? Metadata) Classify(Transaction transaction);
}
