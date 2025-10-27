using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DreamCupCakes.Models
{
    public class Pedido
    {
        [Key]
        public int Id { get; set; }

        // Chave estrangeira para o Cliente (ClienteId)
        [Required]
        public int ClienteId { get; set; }
        public virtual Usuario Cliente { get; set; }

        public int? EntregadorId { get; set; }
        public virtual Usuario Entregador { get; set; }

        public DateTime DataPedido { get; set; } = DateTime.Now;

        public string Status { get; set; } = "Aguardando Pagamento";

        [Column(TypeName = "decimal(18,2)")]
        public decimal ValorTotal { get; set; }

        public string FormaPagamento { get; set; }

        public virtual ICollection<ItemPedido> Itens { get; set; } = new List<ItemPedido>();
    }
}