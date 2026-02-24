using Account.API.Application.Handlers;
using Account.API.Application.Queries;
using Account.API.Domain.Entities;
using Account.API.Domain.Exceptions;
using Account.API.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace Account.Tests.Handlers;

public class ObterSaldoHandlerTests
{
    private readonly Mock<IContaRepository> _contaRepositoryMock;
    private readonly Mock<IMovimentoRepository> _movimentoRepositoryMock;
    private readonly ObterSaldoHandler _handler;

    public ObterSaldoHandlerTests()
    {
        _contaRepositoryMock = new Mock<IContaRepository>();
        _movimentoRepositoryMock = new Mock<IMovimentoRepository>();

        _handler = new ObterSaldoHandler(
            _contaRepositoryMock.Object,
            _movimentoRepositoryMock.Object
        );
    }

    [Fact]
    public async Task Handle_DeveRetornarSaldoComSucesso()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var query = new ObterSaldoQuery(contaId);
        var saldo = 1500.50m;

        var conta = new ContaCorrente(contaId, 12345, "12345678900", "João Silva", "hash", true);

        _contaRepositoryMock
            .Setup(x => x.ObterPorIdAsync(contaId))
            .ReturnsAsync(conta);

        _movimentoRepositoryMock
            .Setup(x => x.ObterSaldo(contaId))
            .ReturnsAsync(saldo);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.NumeroConta.Should().Be(conta.NumeroConta);
        result.Saldo.Should().Be(saldo);
    }

    [Fact]
    public async Task Handle_DeveLancarExcecao_QuandoContaNaoExiste()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var query = new ObterSaldoQuery(contaId);

        _contaRepositoryMock
            .Setup(x => x.ObterPorIdAsync(contaId))
            .ReturnsAsync((ContaCorrente?)null);

        // Act & Assert
        await Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DeveRetornarSaldoZero_QuandoNaoHaMovimentacoes()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var query = new ObterSaldoQuery(contaId);

        var conta = new ContaCorrente(contaId, 12345, "12345678900", "João Silva", "hash", true);

        _contaRepositoryMock
            .Setup(x => x.ObterPorIdAsync(contaId))
            .ReturnsAsync(conta);

        _movimentoRepositoryMock
            .Setup(x => x.ObterSaldo(contaId))
            .ReturnsAsync(0m);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Saldo.Should().Be(0m);
    }
}
