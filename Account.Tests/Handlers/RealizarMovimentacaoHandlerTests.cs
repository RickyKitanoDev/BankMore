using Account.API.Application.Commands;
using Account.API.Application.Handlers;
using Account.API.Domain.Entities;
using Account.API.Domain.Exceptions;
using Account.API.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace Account.Tests.Handlers;

public class RealizarMovimentacaoHandlerTests
{
    private readonly Mock<IMovimentoRepository> _movimentoRepositoryMock;
    private readonly Mock<IContaRepository> _contaRepositoryMock;
    private readonly RealizarMovimentacaoHandler _handler;

    public RealizarMovimentacaoHandlerTests()
    {
        _movimentoRepositoryMock = new Mock<IMovimentoRepository>();
        _contaRepositoryMock = new Mock<IContaRepository>();

        _handler = new RealizarMovimentacaoHandler(
            _contaRepositoryMock.Object,
            _movimentoRepositoryMock.Object
        );
    }

    [Fact]
    public async Task Handle_DeveRealizarCreditoComSucesso()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var command = new RealizarMovimentacaoCommand("cred-123", 100.00m, 'C')
        {
            ContaOrigemId = contaId,
            NumeroConta = 12345
        };

        var conta = new ContaCorrente(contaId, 12345, "12345678900", "João Silva", "hash", true);

        _contaRepositoryMock
            .Setup(x => x.ObterPorNumeroAsync(12345))
            .ReturnsAsync(conta);

        _movimentoRepositoryMock
            .Setup(x => x.ExistePorIdentificacao("cred-123"))
            .ReturnsAsync(false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _movimentoRepositoryMock.Verify(x => x.Adicionar("cred-123", conta.Id, 100.00m, 'C'), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveVerificarSaldoParaDebito()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var command = new RealizarMovimentacaoCommand("deb-123", 50.00m, 'D')
        {
            ContaOrigemId = contaId,
            NumeroConta = 12345
        };

        var conta = new ContaCorrente(contaId, 12345, "12345678900", "João Silva", "hash", true);

        _contaRepositoryMock
            .Setup(x => x.ObterPorNumeroAsync(12345))
            .ReturnsAsync(conta);

        _movimentoRepositoryMock
            .Setup(x => x.ExistePorIdentificacao("deb-123"))
            .ReturnsAsync(false);

        _movimentoRepositoryMock
            .Setup(x => x.ObterSaldo(conta.Id))
            .ReturnsAsync(100.00m);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Verifica que a movimentação foi adicionada
        _movimentoRepositoryMock.Verify(x => x.Adicionar("deb-123", conta.Id, 50.00m, 'D'), Times.Once);
    }

    [Fact]
    public async Task Handle_DevePermitirDebito_MesmoComSaldoInsuficiente()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var command = new RealizarMovimentacaoCommand("deb-456", 150.00m, 'D')
        {
            ContaOrigemId = contaId,
            NumeroConta = 12345
        };

        var conta = new ContaCorrente(contaId, 12345, "12345678900", "João Silva", "hash", true);

        _contaRepositoryMock
            .Setup(x => x.ObterPorNumeroAsync(12345))
            .ReturnsAsync(conta);

        _movimentoRepositoryMock
            .Setup(x => x.ExistePorIdentificacao("deb-456"))
            .ReturnsAsync(false);

        _movimentoRepositoryMock
            .Setup(x => x.ObterSaldo(conta.Id))
            .ReturnsAsync(100.00m);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Deve permitir saldo negativo
        _movimentoRepositoryMock.Verify(x => x.Adicionar("deb-456", conta.Id, 150.00m, 'D'), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveRespeitarIdempotencia()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var command = new RealizarMovimentacaoCommand("duplicado-123", 100.00m, 'C')
        {
            ContaOrigemId = contaId,
            NumeroConta = 12345
        };

        var conta = new ContaCorrente(contaId, 12345, "12345678900", "João Silva", "hash", true);

        _contaRepositoryMock
            .Setup(x => x.ObterPorNumeroAsync(12345))
            .ReturnsAsync(conta);

        _movimentoRepositoryMock
            .Setup(x => x.ExistePorIdentificacao("duplicado-123"))
            .ReturnsAsync(true);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _movimentoRepositoryMock.Verify(x => x.Adicionar(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<char>()), Times.Never);
    }
}
