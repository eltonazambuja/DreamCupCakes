using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DreamCupCakes.Models
{
    public class Cupcake
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório.")]
        public string Nome { get; set; }

        public string Descricao { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Required(ErrorMessage = "O valor é obrigatório.")]
        public decimal Valor { get; set; }

        public string FotoUrl { get; set; }
        public bool Ativo { get; set; } = true;
    }
}