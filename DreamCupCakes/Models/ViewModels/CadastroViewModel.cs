using System.ComponentModel.DataAnnotations;

namespace DreamCupCakes.Models.ViewModels
{
    public class CadastroViewModel
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "O telefone é obrigatório.")]
        public string Telefone { get; set; }

        [Required(ErrorMessage = "O endereço é obrigatório.")]
        public string Endereco { get; set; }

        [Required(ErrorMessage = "A função é obrigatória.")]
        [Display(Name = "Tipo de Conta")]
        public string Funcao { get; set; } // Cliente, Administrador, Entregador

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [DataType(DataType.Password)]
        public string Senha { get; set; }

        [DataType(DataType.Password)]
        [Compare("Senha", ErrorMessage = "A senha e a confirmação de senha não coincidem.")]
        [Display(Name = "Confirmar Senha")]
        public string ConfirmaSenha { get; set; }
    }
}