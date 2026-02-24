using System.Text.RegularExpressions;

namespace Account.API.Domain.ValueObjects;

public static class CpfValidator
{
    // Very simple CPF format check (11 digits) and basic invalid sequences
    public static bool IsValid(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf)) return false;

        var digits = Regex.Replace(cpf, "[^0-9]", string.Empty);
        if (digits.Length != 11) return false;

        // Reject known invalid repeated sequences
        var invalids = new[]
        {
            "00000000000","11111111111","22222222222","33333333333",
            "44444444444","55555555555","66666666666","77777777777",
            "88888888888","99999999999"
        };

        if (invalids.Contains(digits)) return false;

        // For this exercise we just validate length and not the full CPF algorithm
        return true;
    }
}
