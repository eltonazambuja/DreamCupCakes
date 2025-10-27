using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DreamCupCakes.Data;
using DreamCupCakes.Models;
using DreamCupCakes.Models.ViewModels;
using DreamCupCakes.Services;
using System.Security.Claims;
using System.Security.Cryptography;

namespace DreamCupCakes.Controllers
{
    // Apenas usuários com a função (Role) "Administrador" podem acessar este Controller
    [Authorize(Roles = "Administrador")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IErrorLogger _logger;

        public AdminController(ApplicationDbContext context, IErrorLogger logger)
        {
            _context = context;
            _logger = logger;
        }
        private string HashSimples(string senha)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(senha));
                return BitConverter.ToString(bytes).Replace("-", "");
            }
        }

        // --------------------------------------------------------------------------------
        // 1. DASHBOARD (READ)
        // --------------------------------------------------------------------------------
        // GET: /Admin/Dashboard
        public IActionResult Dashboard()
        {
            var adminName = User.FindFirstValue(ClaimTypes.Name);
            ViewData["AdminName"] = adminName ?? "Administrador";

            return View();
        }

        // --------------------------------------------------------------------------------
        // 2. GESTÃO DE ENTREGADORES (CRUD)
        // --------------------------------------------------------------------------------

        // GET: /Admin/GerenciarEntregadores (Listar)
        public IActionResult GerenciarEntregadores()
        {
            var entregadores = _context.Usuarios
                                       .Where(u => u.Funcao == "Entregador")
                                       .OrderBy(u => u.Nome)
                                       .ToList();

            return View(entregadores);
        }

        // GET: /Admin/CadastrarEntregador
        public IActionResult CadastrarEntregador()
        {
            return View();
        }

        // POST: /Admin/CadastrarEntregador (Criar)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CadastrarEntregador(CadastroViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Verificar se o e-mail já existe
                    if (await _context.Usuarios.AnyAsync(u => u.Email == model.Email))
                    {
                        ModelState.AddModelError("Email", "Este e-mail já está cadastrado.");
                        return View(model);
                    }

                    // 2. Criar Entregador (Força a função para "Entregador")
                    var entregador = new Usuario
                    {
                        Nome = model.Nome,
                        Email = model.Email,
                        Telefone = model.Telefone,
                        Endereco = model.Endereco,
                        Funcao = "Entregador",
                        SenhaHash = HashSimples(model.Senha)
                    };

                    _context.Usuarios.Add(entregador);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Entregador {entregador.Nome} cadastrado com sucesso!";
                    return RedirectToAction("GerenciarEntregadores");
                }
                catch (Exception ex)
                {
                    await _logger.LogErrorAsync("AdminController:CadastrarEntregador", "Erro ao cadastrar novo entregador.", ex);
                    TempData["ErrorMessage"] = "Erro interno ao cadastrar. Verifique o log de erros.";
                }
            }

            // Retorno em caso de falha de validação ou erro interno
            return View(model);
        }

        // GET: /Admin/EditarEntregador/5 (Carregar formulário)
        [HttpGet]
        public async Task<IActionResult> EditarEntregador(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var entregador = await _context.Usuarios.FindAsync(id);

            if (entregador == null)
            {
                await _logger.LogErrorAsync("AdminController:EditarEntregador(GET)", $"Tentativa de acessar ID inexistente: {id}", null);
                return NotFound();
            }

            if (entregador.Funcao != "Entregador")
            {
                TempData["ErrorMessage"] = "Acesso negado: ID pertence a outra função.";
                return RedirectToAction("GerenciarEntregadores");
            }

            return View(entregador);
        }

        // POST: /Admin/EditarEntregador/5 (Atualizar)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarEntregador(int id, [Bind("Id,Nome,Email,Telefone,Endereco")] Usuario usuarioAtualizado)
        {
            if (ModelState.IsValid)
            {
                TempData["SuccessMessage"] = $"Entregador {usuarioAtualizado.Nome} atualizado com sucesso!";
                return RedirectToAction("GerenciarEntregadores");
            }

            // --- CORREÇÃO DE LOG DE VALIDAÇÃO AQUI ---  tive bastante dor de cab~ça aqui ---
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                                       .Select(x => new { Key = x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage) })
                                       .ToList();

                // Loga exatamente quais campos falharam a validação no Controller
                await _logger.LogErrorAsync("AdminController:EditarEntregador(POST)",
                                            "Falha na validação do ModelState.",
                                            new Exception($"Campos falhos: {System.Text.Json.JsonSerializer.Serialize(errors)}"));
            }

            // Retorna a View com o objeto e os erros de validação
            return View(usuarioAtualizado);
        }

        // POST: /Admin/DeletarEntregador/5 (Deletar)
        [HttpPost, ActionName("DeletarEntregador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletarEntregadorConfirmado(int id)
        {
            var entregador = await _context.Usuarios.FindAsync(id);

            if (entregador == null || entregador.Funcao != "Entregador")
            {
                TempData["ErrorMessage"] = "Entregador não encontrado.";
                return RedirectToAction("GerenciarEntregadores");
            }

            try
            {
                _context.Usuarios.Remove(entregador);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Entregador deletado com sucesso!";
            }
            catch (DbUpdateException ex) // Captura erro de chave estrangeira
            {
                await _logger.LogErrorAsync("AdminController:DeletarEntregador", $"Tentativa de deletar Entregador {id} falhou devido a FK.", ex);
                TempData["ErrorMessage"] = "Não foi possível deletar o entregador. Existem pedidos associados a ele. Primeiro, reatribua os pedidos.";
            }

            return RedirectToAction("GerenciarEntregadores");
        }
    }
}