using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DreamCupCakes.Models.ViewModels
{
    public class CupcakeViewModel
    {
        public int Id { get; set; } // Necessário para a Edição

        [Required(ErrorMessage = "O nome do cupcake é obrigatório.")]
        [Display(Name = "Nome do Cupcake")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "A descrição é obrigatória.")]
        [Display(Name = "Descrição")]
        public string Descricao { get; set; }

        [Required(ErrorMessage = "O valor é obrigatório.")]
        [Range(0.01, 1000.00, ErrorMessage = "O valor deve ser positivo e válido.")]
        [Display(Name = "Valor (R$)")]
        public decimal Valor { get; set; }

        [Display(Name = "Foto do Cupcake")]
        public IFormFile? FotoArquivo { get; set; }

        public string? FotoUrlExistente { get; set; }

        [Display(Name = "Ativo (Disponível na loja)")]
        public bool Ativo { get; set; } = true;
    }
}