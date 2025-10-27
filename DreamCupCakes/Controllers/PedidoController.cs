using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DreamCupCakes.Data;
using DreamCupCakes.Models;
using DreamCupCakes.Services;
using System.Security.Claims;
using System.Linq;

namespace DreamCupCakes.Controllers
{
    // A autorização é definida em nível de método para maior controle (Admin, Entregador, Cliente)
    public class PedidoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IErrorLogger _logger;

        public PedidoController(ApplicationDbContext context, IErrorLogger logger)
        {
            _context = context;
            _logger = logger;
        }

        // --------------------------------------------------------------------------------
        // 1. VISÃO ADMIN (Visão Geral/Listagem de Todos os Pedidos)
        // --------------------------------------------------------------------------------
        // GET: /Pedido/Index
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Index(DateTime? dataInicio, DateTime? dataFim, string? statusFiltro, int? clienteIdFiltro, int? entregadorIdFiltro)
        {
            // 1. Inicia a consulta base
            var pedidosQuery = _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Entregador)
                .Include(p => p.Itens)
                .AsQueryable();

            // 2. Aplica Filtros (Lógica completa de filtragem)
            if (dataInicio.HasValue)
            {
                pedidosQuery = pedidosQuery.Where(p => p.DataPedido.Date >= dataInicio.Value.Date);
            }
            if (dataFim.HasValue)
            {
                pedidosQuery = pedidosQuery.Where(p => p.DataPedido.Date <= dataFim.Value.Date);
            }
            if (!string.IsNullOrEmpty(statusFiltro) && statusFiltro != "Todos")
            {
                pedidosQuery = pedidosQuery.Where(p => p.Status == statusFiltro);
            }
            if (clienteIdFiltro.HasValue && clienteIdFiltro > 0)
            {
                pedidosQuery = pedidosQuery.Where(p => p.ClienteId == clienteIdFiltro.Value);
            }
            if (entregadorIdFiltro.HasValue)
            {
                if (entregadorIdFiltro.Value == -1)
                {
                    pedidosQuery = pedidosQuery.Where(p => p.EntregadorId == null);
                }
                else if (entregadorIdFiltro.Value > 0)
                {
                    pedidosQuery = pedidosQuery.Where(p => p.EntregadorId == entregadorIdFiltro.Value);
                }
            }


            // 3. Executa a consulta
            var pedidos = await pedidosQuery
                .OrderByDescending(p => p.DataPedido)
                .ToListAsync();

            // 4. Carrega dados adicionais para os filtros na View
            ViewBag.Clientes = await _context.Usuarios.Where(u => u.Funcao == "Cliente").OrderBy(u => u.Nome).ToListAsync();
            ViewBag.Entregadores = await _context.Usuarios.Where(u => u.Funcao == "Entregador").OrderBy(u => u.Nome).ToListAsync();

            // 5. Mantém os valores dos filtros na ViewData
            ViewData["DataInicio"] = dataInicio?.ToString("yyyy-MM-dd");
            ViewData["DataFim"] = dataFim?.ToString("yyyy-MM-dd");
            ViewData["StatusFiltro"] = statusFiltro;
            ViewData["ClienteIdFiltro"] = clienteIdFiltro;
            ViewData["EntregadorIdFiltro"] = entregadorIdFiltro;

            return View(pedidos);
        }

        // --------------------------------------------------------------------------------
        // 2. VISÃO ENTREGADOR (Listagem de Pedidos Atribuídos)
        // --------------------------------------------------------------------------------
        // GET: /Pedido/MeusPedidos
        [Authorize(Roles = "Entregador")]
        public async Task<IActionResult> MeusPedidos()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var pedidos = await _context.Pedidos
                .Include(p => p.Cliente)
                // Filtra pedidos atribuídos a este entregador
                .Where(p => p.EntregadorId == userId)
                .OrderByDescending(p => p.DataPedido)
                .ToListAsync();

            return View(pedidos); // MeusPedidos.cshtml
        }

        // --------------------------------------------------------------------------------
        // 3. VISÃO CLIENTE (Acompanhamento de Pedidos)
        // --------------------------------------------------------------------------------
        // GET: /Pedido/MeusPedidosCliente
        // Acesso restrito APENAS a quem tem a role Cliente, resolvendo o erro 500/Acesso Negado
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> MeusPedidosCliente(DateTime? dataInicio, DateTime? dataFim)
        {
            var clienteIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            int clienteId = int.Parse(clienteIdClaim);

            // Filtra SOMENTE pelos pedidos deste cliente
            var pedidosQuery = _context.Pedidos
                .Include(p => p.Entregador)
                .Where(p => p.ClienteId == clienteId)
                .AsQueryable();

            // Aplica Filtros de Data
            if (dataInicio.HasValue)
            {
                pedidosQuery = pedidosQuery.Where(p => p.DataPedido.Date >= dataInicio.Value.Date);
            }
            if (dataFim.HasValue)
            {
                pedidosQuery = pedidosQuery.Where(p => p.DataPedido.Date <= dataFim.Value.Date);
            }

            var pedidos = await pedidosQuery
                .OrderByDescending(p => p.DataPedido)
                .ToListAsync();

            ViewData["DataInicio"] = dataInicio?.ToString("yyyy-MM-dd");
            ViewData["DataFim"] = dataFim?.ToString("yyyy-MM-dd");

            return View(pedidos);
        }

        // --------------------------------------------------------------------------------
        // 4. DETALHES DO PEDIDO (Para Admin e Entregador)
        // --------------------------------------------------------------------------------
        // GET: /Pedido/Detalhes/5
        [Authorize(Roles = "Administrador,Entregador,Cliente")] // Permite a todos verem os detalhes
        public async Task<IActionResult> Detalhes(int? id)
        {
            if (id == null) return NotFound();

            var pedido = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Entregador)
                .Include(p => p.Itens)
                    .ThenInclude(i => i.Cupcake)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (pedido == null) return NotFound();

            // Segurança: Garante que Clientes só vejam seus próprios pedidos
            if (User.IsInRole("Cliente") && pedido.ClienteId != int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!))
            {
                return Forbid();
            }
            // Segurança: Garante que Entregadores só vejam pedidos atribuídos a eles
            if (User.IsInRole("Entregador") && pedido.EntregadorId != int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!))
            {
                return Forbid();
            }


            // Carrega entregadores para o dropdown de atribuição (apenas para Admin)
            if (User.IsInRole("Administrador"))
            {
                ViewBag.EntregadoresDisponiveis = await _context.Usuarios
                    .Where(u => u.Funcao == "Entregador")
                    .ToListAsync();
            }

            return View(pedido);
        }

        // --------------------------------------------------------------------------------
        // 5. ATRIBUIÇÃO E ATUALIZAÇÃO DE STATUS
        // --------------------------------------------------------------------------------

        // POST: /Pedido/AtribuirEntregador/5 (Apenas Admin)
        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AtribuirEntregador(int id, int entregadorId)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null) return NotFound();

            pedido.EntregadorId = entregadorId > 0 ? entregadorId : null;

            try
            {
                _context.Update(pedido);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Entregador atribuído com sucesso.";
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("PedidoController:AtribuirEntregador", $"Erro ao atribuir entregador {entregadorId} ao pedido {id}.", ex);
                TempData["ErrorMessage"] = "Erro ao atribuir entregador. Verifique o log.";
            }

            return RedirectToAction(nameof(Detalhes), new { id });
        }

        // POST: /Pedido/AtualizarStatus/5 (Admin e Entregador)
        [Authorize(Roles = "Administrador,Entregador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AtualizarStatus(int id, string novoStatus)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null) return NotFound();

            // Validação de Entregador: Se for Entregador, só pode atualizar status do próprio pedido.
            if (User.IsInRole("Entregador") && pedido.EntregadorId != int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!))
            {
                return Forbid();
            }

            // Status Permitidos para a atualização
            var statusPermitidos = new[] { "Pago", "Em Preparação", "A caminho", "Entregue" };
            if (!statusPermitidos.Contains(novoStatus))
            {
                TempData["ErrorMessage"] = "Status inválido.";
                return RedirectToAction(nameof(Detalhes), new { id });
            }

            pedido.Status = novoStatus;

            try
            {
                _context.Update(pedido);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Status do Pedido {id} atualizado para '{novoStatus}'.";
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("PedidoController:AtualizarStatus", $"Erro ao atualizar status do pedido {id} para {novoStatus}.", ex);
                TempData["ErrorMessage"] = "Erro ao atualizar status. Verifique o log.";
            }

            return RedirectToAction(nameof(Detalhes), new { id });
        }

        // 6. RELATÓRIOS (Visão por Período - Para Admin)
        // GET: /Pedido/RelatorioVendas
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> RelatorioVendas(DateTime? dataInicio, DateTime? dataFim)
        {
            dataInicio ??= DateTime.Now.Date.AddDays(-7);
            dataFim ??= DateTime.Now.Date;

            DateTime inicioFiltro = dataInicio.Value.Date;

            DateTime fimFiltro = dataFim.Value.Date.AddDays(1);


            var pedidos = await _context.Pedidos
                .Include(p => p.Cliente)
                .Where(p => p.DataPedido >= inicioFiltro && p.DataPedido < fimFiltro)
                .OrderByDescending(p => p.DataPedido)
                .ToListAsync();

            ViewData["DataInicio"] = inicioFiltro.ToString("yyyy-MM-dd");
            ViewData["DataFim"] = dataFim.Value.ToString("yyyy-MM-dd");

            return View(pedidos);
        }
        [Authorize(Roles = "Administrador")]
        [HttpPost]
        public async Task<IActionResult> ExportarRelatorio(DateTime? dataInicio, DateTime? dataFim)
        {
            dataInicio ??= DateTime.Now.Date.AddDays(-7);
            dataFim ??= DateTime.Now.Date;


            DateTime inicioFiltro = dataInicio.Value.Date;
            DateTime fimFiltro = dataFim.Value.Date.AddDays(1); // Início do dia seguinte

            var pedidos = await _context.Pedidos
                .Include(p => p.Cliente)
                .Where(p => p.DataPedido >= inicioFiltro && p.DataPedido < fimFiltro)
                .OrderByDescending(p => p.DataPedido)
                .ToListAsync();

            // 1. Cria o conteúdo do CSV
            var builder = new System.Text.StringBuilder();

            // Cabeçalho do CSV
            builder.AppendLine("ID;Data do Pedido;Cliente;Email do Cliente;Status;Valor Total;Forma de Pagamento");

            // Corpo do CSV
            foreach (var pedido in pedidos)
            {
                // Usa .Replace(";", ",") para evitar que nomes com ponto-e-vírgula quebrem a formatação CSV
                builder.AppendLine($"{pedido.Id};" +
                                   $"{pedido.DataPedido:yyyy-MM-dd HH:mm};" +
                                   $"{pedido.Cliente?.Nome.Replace(";", ",")};" +
                                   $"{pedido.Cliente?.Email};" +
                                   $"{pedido.Status};" +
                                   $"{pedido.ValorTotal:N2};" +
                                   $"{pedido.FormaPagamento}");
            }
            string fileName = $"Relatorio_Vendas_{dataInicio.Value:yyyyMMdd}_a_{dataFim.Value:yyyyMMdd}.csv";

            return File(
                System.Text.Encoding.UTF8.GetBytes(builder.ToString()),
                "text/csv",
                fileName
            );
        }

    }
    

    
}