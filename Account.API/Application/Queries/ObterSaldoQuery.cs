using MediatR;
using Account.API.Application.DTOs;

namespace Account.API.Application.Queries;

public record ObterSaldoQuery(Guid ContaId) : IRequest<SaldoResult>;
