using Account.API.Application.Commands;
using Account.API.Application.Handlers;
using Account.API.Domain.Entities;
using Account.API.Domain.Exceptions;
using Account.API.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Account.Tests.Handlers;

public class LoginHandlerTests
{
    private readonly Mock<IContaRepository> _repositoryMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IPasswordHasher> _hasherMock;
    private readonly Mock<ILogger<LoginHandler>> _loggerMock;
    private readonly LoginHandler _handler;

    public LoginHandlerTests()
    {
        _repositoryMock = new Mock<IContaRepository>();
        _jwtServiceMock = new Mock<IJwtService>();
        _hasherMock = new Mock<IPasswordHasher>();
        _loggerMock = new Mock<ILogger<LoginHandler>>();

        _handler = new LoginHandler(
            _repositoryMock.Object,
            _jwtServiceMock.Object,
            _hasherMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task Handle_DeveFazerLoginComSucesso()
    {
        // Arrange
        var command = new LoginCommand("12345678900", "senha123");
        var token = "jwt_token_gerado";

        var conta = new ContaCorrente(
            Guid.NewGuid(),
            12345,
            "12345678900",
            "João Silva",
            "hashed_password",
            true
        );

        _repositoryMock
            .Setup(x => x.ObterPorNumeroOuCpfAsync(command.NumeroOuCpf))
            .ReturnsAsync(conta);

        _hasherMock
            .Setup(x => x.Verify(command.Senha, conta.SenhaHash))
            .Returns(true);

        _jwtServiceMock
            .Setup(x => x.GenerateToken(conta.Id, conta.NumeroConta))
            .Returns(token);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(token);
        _repositoryMock.Verify(x => x.ObterPorNumeroOuCpfAsync(command.NumeroOuCpf), Times.Once);
        _hasherMock.Verify(x => x.Verify(command.Senha, conta.SenhaHash), Times.Once);
        _jwtServiceMock.Verify(x => x.GenerateToken(conta.Id, conta.NumeroConta), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveLancarExcecao_QuandoUsuarioNaoExiste()
    {
        // Arrange
        var command = new LoginCommand("12345678900", "senha123");

        _repositoryMock
            .Setup(x => x.ObterPorNumeroOuCpfAsync(command.NumeroOuCpf))
            .ReturnsAsync((ContaCorrente?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessException>();
        _repositoryMock.Verify(x => x.ObterPorNumeroOuCpfAsync(command.NumeroOuCpf), Times.Once);
        _hasherMock.Verify(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveLancarExcecao_QuandoSenhaIncorreta()
    {
        // Arrange
        var command = new LoginCommand("12345678900", "senha_errada");

        var conta = new ContaCorrente(
            Guid.NewGuid(),
            12345,
            "12345678900",
            "João Silva",
            "hashed_password",
            true
        );

        _repositoryMock
            .Setup(x => x.ObterPorNumeroOuCpfAsync(command.NumeroOuCpf))
            .ReturnsAsync(conta);

        _hasherMock
            .Setup(x => x.Verify(command.Senha, conta.SenhaHash))
            .Returns(false);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessException>();
        _repositoryMock.Verify(x => x.ObterPorNumeroOuCpfAsync(command.NumeroOuCpf), Times.Once);
        _hasherMock.Verify(x => x.Verify(command.Senha, conta.SenhaHash), Times.Once);
    }
}
