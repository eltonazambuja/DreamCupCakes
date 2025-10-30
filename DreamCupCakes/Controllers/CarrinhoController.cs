using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DreamCupCakes.Data;
using DreamCupCakes.Models;
using System.Text.Json;
using System.Security.Claims;
using System.Linq;

namespace DreamCupCakes.Controllers
{
    public class CarrinhoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CarrinhoController(ApplicationDbContext context)
        {
            _context = context;
        }

        private const string CarrinhoSessionKey = "Carrinho";

        // --------------------------------------------------------------------------------
        // MÉTODOS DE GERENCIAMENTO DE SESSION
        // --------------------------------------------------------------------------------

        // Obtém o carrinho da Session (Lista de ItemPedido)
        private List<ItemPedido> GetCarrinho()
        {
            var carrinhoJson = HttpContext.Session.GetString(CarrinhoSessionKey);
            if (carrinhoJson == null)
            {
                return new List<ItemPedido>();
            }
            // Retorna a lista de ItemPedido desserializada
            return JsonSerializer.Deserialize<List<ItemPedido>>(carrinhoJson) ?? new List<ItemPedido>();
        }

        // Salva o carrinho na Session
        private void SaveCarrinho(List<ItemPedido> carrinho)
        {
            HttpContext.Session.SetString(CarrinhoSessionKey, JsonSerializer.Serialize(carrinho));
        }

        // --------------------------------------------------------------------------------
        // AÇÕES DO CARRINHO
        // --------------------------------------------------------------------------------

        // GET: /Carrinho/Index (Visualizar o Carrinho)
        public async Task<IActionResult> Index()
        {
            var carrinho = GetCarrinho();

            // Carrega detalhes do Cupcake para exibição
            foreach (var item in carrinho)
            {
                // Usamos AsNoTracking para otimizar a consulta, já que não vamos salvar alterações aqui
                item.Cupcake = await _context.Cupcakes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == item.CupcakeId);
            }

            return View(carrinho);
        }

        // POST: /Carrinho/Adicionar
        [HttpPost]
        [ValidateAntiForgeryToken] // Boa prática de segurança
        public async Task<IActionResult> Adicionar(int cupcakeId)
        {
            var cupcake = await _context.Cupcakes.FindAsync(cupcakeId);

            if (cupcake == null || !cupcake.Ativo)
            {
                TempData["ErrorMessage"] = "O produto não está disponível ou não existe.";
                return RedirectToAction("Vitrine", "Home");
            }

            var carrinho = GetCarrinho();
            var itemExistente = carrinho.FirstOrDefault(i => i.CupcakeId == cupcakeId);

            if (itemExistente != null)
            {
                itemExistente.Quantidade++;
            }
            else
            {
                // Adiciona novo item com preço unitário do momento
                carrinho.Add(new ItemPedido
                {
                    CupcakeId = cupcakeId,
                    Quantidade = 1,
                    PrecoUnitario = cupcake.Valor,
                    // Não incluí o objeto Cupcake completo no carrinho da Session para manter leve.
                });
            }

            SaveCarrinho(carrinho);
            TempData["SuccessMessage"] = $"'{cupcake.Nome}' adicionado ao carrinho!";
            return RedirectToAction("Vitrine", "Home");
        }

        // --------------------------------------------------------------------------------
        // FINALIZAÇÃO DE PEDIDO (CHECKOUT)
        // --------------------------------------------------------------------------------

        // GET: /Carrinho/Finalizar (Tela de Confirmação/Checkout)
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> Finalizar()
        {
            var carrinho = GetCarrinho();

            if (!carrinho.Any())
            {
                TempData["ErrorMessage"] = "Seu carrinho está vazio!";
                return RedirectToAction("Vitrine", "Home");
            }

            // Garante que os dados do Cliente (Endereço, Telefone) estejam na ViewBag
            var clienteId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var cliente = await _context.Usuarios.FindAsync(int.Parse(clienteId));

            // Carrega detalhes dos Cupcakes para a tela de resumo
            foreach (var item in carrinho)
            {
                item.Cupcake = await _context.Cupcakes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == item.CupcakeId);
            }

            ViewBag.Cliente = cliente;
            return View(carrinho); // Retorna a View Finalizar.cshtml
        }

        // POST: /Carrinho/Confirmar
        [Authorize(Roles = "Cliente")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Recebe todos os campos do formulário: forma de pagamento, opção escolhida e novo endereço
        public async Task<IActionResult> Confirmar(string formaPagamento, string opcaoEntrega, string? novoEndereco)
        {
            var carrinho = GetCarrinho();
            var cliente = await _context.Usuarios.FindAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));

            // Determina o endereço padrão do cliente (para usar se ele escolher a opção "Cadastrado")
            string enderecoFinal = cliente?.Endereco ?? "Endereço Cadastrado Não Encontrado";

            // 1. Validação do Formulário (Lado do Servidor)
            if (string.IsNullOrEmpty(formaPagamento))
            {
                ModelState.AddModelError("formaPagamento", "A forma de pagamento é obrigatória.");
            }

            if (opcaoEntrega == "Alternativo")
            {
                if (string.IsNullOrWhiteSpace(novoEndereco))
                {
                    ModelState.AddModelError("NovoEndereco", "O endereço alternativo é obrigatório.");
                }
                else
                {
                    // Se Alternativo foi escolhido e está preenchido, define o endereço final.
                    enderecoFinal = novoEndereco;
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var clienteId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                    decimal valorTotal = carrinho.Sum(i => i.Quantidade * i.PrecoUnitario);

                    // CRIAÇÃO DO PEDIDO: ATRIBUIÇÃO DO ENDEREÇO CORRETO
                    var pedido = new Pedido
                    {
                        ClienteId = clienteId,
                        DataPedido = DateTime.Now,
                        Status = "Pago",
                        ValorTotal = valorTotal,
                        FormaPagamento = formaPagamento,
                        // LINHA CRÍTICA: Salva o endereço final escolhido no novo campo do Pedido
                        EnderecoEntrega = enderecoFinal,
                        Itens = new List<ItemPedido>()
                    };

                    // 2. Adiciona os Itens, Salva e Limpa a Sessão
                    foreach (var itemCarrinho in carrinho)
                    {
                        pedido.Itens.Add(new ItemPedido
                        {
                            CupcakeId = itemCarrinho.CupcakeId,
                            Quantidade = itemCarrinho.Quantidade,
                            PrecoUnitario = itemCarrinho.PrecoUnitario,
                        });
                    }

                    _context.Pedidos.Add(pedido);
                    await _context.SaveChangesAsync();
                    HttpContext.Session.Remove(CarrinhoSessionKey);

                    TempData["SuccessMessage"] = $"Pedido registrado com sucesso! Entrega em: {enderecoFinal}";
                    return RedirectToAction("Vitrine", "Home");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Ocorreu um erro ao finalizar o pedido. Tente novamente.";
                    return RedirectToAction(nameof(Index));
                }
            }

            // Se a validação falhar, repopula TempData e retorna a View com os erros.
            TempData["NovoEndereco"] = novoEndereco;
            TempData["FormaPagamento"] = formaPagamento;
            TempData["OpcaoEntrega"] = opcaoEntrega;

            return RedirectToAction(nameof(Finalizar));
        }
        // POST: /Carrinho/Remover
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remover(int cupcakeId)
        {
            var carrinho = GetCarrinho();

            // Encontra o item a ser removido pelo ID
            var itemToRemove = carrinho.FirstOrDefault(i => i.CupcakeId == cupcakeId);

            if (itemToRemove != null)
            {
                var cupcakeName = itemToRemove.Cupcake?.Nome ?? "Item";

                // Remove o item da lista
                carrinho.Remove(itemToRemove);

                // Salva a lista atualizada de volta na Session
                SaveCarrinho(carrinho);

                TempData["SuccessMessage"] = $"'{cupcakeName}' foi removido do carrinho.";
            }
            else
            {
                TempData["ErrorMessage"] = "Erro: Item não encontrado no carrinho.";
            }

            return RedirectToAction(nameof(Index)); // Volta para a visualização do carrinho
        }

    }
}