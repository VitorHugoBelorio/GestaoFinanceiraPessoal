using System.ComponentModel.DataAnnotations;
using GestaoFinanceiraPessoal.Api.Contracts.Onboarding;

namespace GestaoFinanceiraPessoal.Api.Contracts.Auth;

public class RegisterRequest
{
    [Required(ErrorMessage = "Informe seu nome."),
     StringLength(120, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 120 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe seu e-mail."),
     EmailAddress(ErrorMessage = "Informe um e-mail válido."),
     StringLength(256, ErrorMessage = "O e-mail deve ter até 256 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe uma senha."),
     StringLength(128, MinimumLength = 8, ErrorMessage = "A senha deve ter entre 8 e 128 caracteres.")]
    public string Password { get; set; } = string.Empty;
}

public class CompleteRegistrationRequest
{
    [Required(ErrorMessage = "Preencha os dados da conta.")]
    public RegisterRequest Account { get; set; } = null!;

    [Required(ErrorMessage = "Conclua a configuração financeira inicial.")]
    public CompleteOnboardingRequest Setup { get; set; } = null!;
}

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class UpdateAccountRequest
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [StringLength(128, MinimumLength = 8)]
    public string? NewPassword { get; set; }
}

public record AuthSessionResponse(
    int UserId,
    string Name,
    string Email,
    bool OnboardingCompleted);
