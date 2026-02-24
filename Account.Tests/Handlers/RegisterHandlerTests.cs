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

public class RegisterHandlerTests
{
    private readonly Mock<IContaRepository> _repositoryMock;
    private readonly Mock<IPasswordHasher> _hasherMock;
    private readonly Mock<ILogger<RegisterHandler>> _loggerMock;
    private readonly RegisterHandler _handler;

    public RegisterHandlerTests()
    {
        _repositoryMock = new Mock<IContaRepository>();
        _hasherMock = new Mock<IPasswordHasher>();
        _loggerMock = new Mock<ILogger<RegisterHandler>>();

        _handler = new RegisterHandler(
            _repositoryMock.Object,
            _hasherMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task Handle_DeveRegistrarContaComSucesso()
    {
        // Arrange
        var command = new RegisterCommand
        {
            NumeroConta = 12345,
            Cpf = "12345678900",
            Nome = "João Silva",
            Senha = "senha123"
        };

        _repositoryMock
            .Setup(x => x.ObterPorCpfAsync(command.Cpf))
            .ReturnsAsync((ContaCorrente?)null);

        _hasherMock
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed_password");

        _repositoryMock
            .Setup(x => x.AdicionarAsync(It.IsAny<ContaCorrente>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(command.NumeroConta);

        _repositoryMock.Verify(x => x.ObterPorCpfAsync(command.Cpf), Times.Once);
        _hasherMock.Verify(x => x.Hash(It.IsAny<string>()), Times.Once);
        _repositoryMock.Verify(x => x.AdicionarAsync(It.Is<ContaCorrente>(
            c => c.Cpf == command.Cpf && 
                 c.Nome == command.Nome && 
                 c.NumeroConta == command.NumeroConta
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveLancarExcecao_QuandoCpfJaExiste()
    {
        // Arrange
        var command = new RegisterCommand
        {
            NumeroConta = 12345,
            Cpf = "12345678900",
            Nome = "João Silva",
            Senha = "senha123"
        };

        var contaExistente = new ContaCorrente(
            Guid.NewGuid(),
            54321,
            command.Cpf,
            "Outro Nome",
            "hash",
            true
        );

        _repositoryMock
            .Setup(x => x.ObterPorCpfAsync(command.Cpf))
            .ReturnsAsync(contaExistente);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("CPF já cadastrado");

        _repositoryMock.Verify(x => x.ObterPorCpfAsync(command.Cpf), Times.Once);
        _repositoryMock.Verify(x => x.AdicionarAsync(It.IsAny<ContaCorrente>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("12345678901234")]
    [InlineData("abc")]
    public async Task Handle_DeveLancarExcecao_QuandoCpfInvalido(string cpfInvalido)
    {
        // Arrange
        var command = new RegisterCommand
        {
            NumeroConta = 12345,
            Cpf = cpfInvalido,
            Nome = "João Silva",
            Senha = "senha123"
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("CPF inválido");

        _repositoryMock.Verify(x => x.ObterPorCpfAsync(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task Handle_DeveLancarExcecao_QuandoSenhaInvalida(string? senhaInvalida)
    {
        // Arrange
        var command = new RegisterCommand
        {
            NumeroConta = 12345,
            Cpf = "12345678900",
            Nome = "João Silva",
            Senha = senhaInvalida!
        };

        _repositoryMock
            .Setup(x => x.ObterPorCpfAsync(command.Cpf))
            .ReturnsAsync((ContaCorrente?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Senha inválida");
    }

    [Fact]
    public async Task Handle_DeveNormalizarSenha()
    {
        // Arrange
        var command = new RegisterCommand
        {
            NumeroConta = 12345,
            Cpf = "12345678900",
            Nome = "João Silva",
            Senha = "  senha123  " // Com espaços
        };

        _repositoryMock
            .Setup(x => x.ObterPorCpfAsync(command.Cpf))
            .ReturnsAsync((ContaCorrente?)null);

        _hasherMock
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed_password");

        _repositoryMock
            .Setup(x => x.AdicionarAsync(It.IsAny<ContaCorrente>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _hasherMock.Verify(x => x.Hash("senha123"), Times.Once); // Deve ter removido espaços
    }
}
