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
    // Acesso restrito a usuários logados (pode ser qualquer um, mas o uso é focado no Cliente)
    [Authorize]
    public class ClienteController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IErrorLogger _logger;

        public ClienteController(ApplicationDbContext context, IErrorLogger logger)
        {
            _context = context;
            _logger = logger;
        }

        // Função utilitária para Hash Simples (Replicada aqui)
        private string HashSimples(string senha)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(senha));
                return BitConverter.ToString(bytes).Replace("-", "");
            }
        }

        // --------------------------------------------------------------------------------
        // GET: /Cliente/Perfil (Carregar Perfil)
        // --------------------------------------------------------------------------------
        public async Task<IActionResult> Perfil()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int id)) return Forbid();

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            // Mapeia o Model do DB para o ViewModel
            var model = new PerfilViewModel
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                Telefone = usuario.Telefone,
                Endereco = usuario.Endereco
            };

            return View(model);
        }

        // --------------------------------------------------------------------------------
        // POST: /Cliente/Perfil (Atualizar Perfil)
        // --------------------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Perfil(PerfilViewModel model)
        {
            // Remove a validação do Email para evitar conflito de unicidade do próprio usuário
            ModelState.Remove(nameof(model.Email));

            // Verifica se as senhas estão sendo alteradas, e as valida
            if (!string.IsNullOrEmpty(model.NovaSenha) && model.NovaSenha != model.ConfirmaNovaSenha)
            {
                ModelState.AddModelError(nameof(model.ConfirmaNovaSenha), "As senhas não coincidem.");
            }

            if (ModelState.IsValid)
            {
                var usuarioExistente = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == model.Id);
                if (usuarioExistente == null) return NotFound();

                // Garante que o Email não está sendo usado por outro usuário
                var emailCheck = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Email == model.Email && u.Id != model.Id);
                if (emailCheck != null)
                {
                    ModelState.AddModelError(nameof(model.Email), "Este e-mail já está em uso por outro usuário.");
                    return View(model);
                }

                try
                {
                    // Monta o objeto Usuario com os dados atualizados
                    var usuarioAtualizado = new Usuario
                    {
                        Id = model.Id,
                        Nome = model.Nome,
                        Email = model.Email,
                        Telefone = model.Telefone,
                        Endereco = model.Endereco,
                        Funcao = usuarioExistente.Funcao,

                        // Atualiza Senha ou mantém a existente
                        SenhaHash = !string.IsNullOrEmpty(model.NovaSenha)
                                    ? HashSimples(model.NovaSenha)
                                    : usuarioExistente.SenhaHash
                    };

                    _context.Usuarios.Attach(usuarioAtualizado);
                    _context.Entry(usuarioAtualizado).State = EntityState.Modified;

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Perfil atualizado com sucesso!";
                    return RedirectToAction(nameof(Perfil));
                }
                catch (Exception ex)
                {
                    await _logger.LogErrorAsync("ClienteController:Perfil(POST)", $"Erro ao atualizar perfil do usuário ID: {model.Id}", ex);
                    ModelState.AddModelError(string.Empty, "Erro ao salvar alterações. Consulte o log.");
                }
            }

            return View(model);
        }
    }
}