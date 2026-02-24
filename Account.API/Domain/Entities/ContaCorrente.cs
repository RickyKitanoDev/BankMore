using Account.API.Domain.Exceptions;

namespace Account.API.Domain.Entities;

public class ContaCorrente
{
    public Guid Id { get; private set; }
    public int NumeroConta { get; private set; }
    public string Cpf { get; private set; }
    public string Nome { get; private set; }
    public string SenhaHash { get; private set; }
    public bool Ativo { get; private set; }

    private ContaCorrente() { }

    public ContaCorrente(Guid id, int numero, string cpf, string nome, string senhaHash, bool ativo)
    {
        Id = id;
        NumeroConta = numero;
        Cpf = cpf;
        Nome = nome;
        SenhaHash = senhaHash;
        Ativo = ativo;
    }

    public void ValidarAtiva()
    {
        if (!Ativo)
            throw new BusinessException("Conta inativa", "INACTIVE_ACCOUNT");
    }
}

