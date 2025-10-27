using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DreamCupCakes.Models
{
    public class Usuario
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório.")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "O telefone é obrigatório.")]
        public string Telefone { get; set; }

        [Required(ErrorMessage = "O endereço é obrigatório.")]
        public string Endereco { get; set; }

        [Required]
        public string Funcao { get; set; }

        [Required(ErrorMessage = "A senha é obrigatória.")]
        public string SenhaHash { get; set; }

        public virtual ICollection<Pedido> PedidosFeitos { get; set; } = new List<Pedido>();

        public virtual ICollection<Pedido> PedidosParaEntregar { get; set; } = new List<Pedido>();
    }
}