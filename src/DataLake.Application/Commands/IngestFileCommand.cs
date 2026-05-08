using MediatR;

namespace DataLake.Application.Commands;

public sealed record IngestFileCommand(string FilePath, string FileName) : IRequest<IngestFileResult>;

public sealed record IngestFileResult(int RecordsIngested, int RecordsQuarantined);
