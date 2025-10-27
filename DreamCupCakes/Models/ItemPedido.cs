using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DreamCupCakes.Models
{
    public class ItemPedido
    {
        [Key]
        public int Id { get; set; }

        public int PedidoId { get; set; }
        public virtual Pedido Pedido { get; set; }

        [Required]
        public int CupcakeId { get; set; }
        public virtual Cupcake Cupcake { get; set; }

        [Required]
        public int Quantidade { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecoUnitario { get; set; }
    }
}