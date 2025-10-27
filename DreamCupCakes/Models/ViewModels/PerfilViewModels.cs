using System.ComponentModel.DataAnnotations;

namespace DreamCupCakes.Models.ViewModels
{
    public class PerfilViewModel
    {
        public int Id { get; set; } // Necessário para a identificação do registro a ser atualizado

        [Required(ErrorMessage = "O nome é obrigatório.")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "O telefone é obrigatório.")]
        public string Telefone { get; set; }

        [Required(ErrorMessage = "O endereço é obrigatório.")]
        public string Endereco { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Nova Senha (deixe em branco para manter a atual)")]
        public string? NovaSenha { get; set; }

        [DataType(DataType.Password)]
        [Compare("NovaSenha", ErrorMessage = "As senhas não coincidem.")]
        [Display(Name = "Confirmar Nova Senha")]
        public string? ConfirmaNovaSenha { get; set; }
    }
}