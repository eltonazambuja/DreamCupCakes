using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using DreamCupCakes.Data;
using DreamCupCakes.Models;
using DreamCupCakes.Models.ViewModels;
using DreamCupCakes.Services;
using System.Security.Claims;
using System.Security.Cryptography;

namespace DreamCupCakes.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IErrorLogger _logger;

        public AccountController(ApplicationDbContext context, IErrorLogger logger)
        {
            _context = context;
            _logger = logger;
        }

        // Função utilitária para Hash Simples
        private string HashSimples(string senha)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(senha));
                return BitConverter.ToString(bytes).Replace("-", "");
            }
        }

        // --------------------------------------------------------------------------------
        // CADASTRO (GET)
        // --------------------------------------------------------------------------------
        [HttpGet]
        public IActionResult Cadastro()
        {
            // Checa se já existe um Admin para desabilitar a opção no formulário
            ViewBag.ExisteAdmin = _context.Usuarios.Any(u => u.Funcao == "Administrador");
            return View();
        }

        // --------------------------------------------------------------------------------
        // CADASTRO (POST)
        // --------------------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cadastro(CadastroViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Validação de Email
                if (await _context.Usuarios.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Este e-mail já está cadastrado.");
                    ViewBag.ExisteAdmin = await _context.Usuarios.AnyAsync(u => u.Funcao == "Administrador");
                    return View(model);
                }

                // 2. Validação de Admin (Garante que só há um Administrador inicial)
                if (model.Funcao == "Administrador" && await _context.Usuarios.AnyAsync(u => u.Funcao == "Administrador"))
                {
                    ModelState.AddModelError("Funcao", "Já existe um administrador. Você não pode se cadastrar como outro.");
                    ViewBag.ExisteAdmin = true;
                    return View(model);
                }

                // 3. Criação e Salva no DB
                var usuario = new Usuario
                {
                    Nome = model.Nome,
                    Email = model.Email,
                    Telefone = model.Telefone,
                    Endereco = model.Endereco,
                    Funcao = model.Funcao,
                    SenhaHash = HashSimples(model.Senha)
                };

                try
                {
                    _context.Usuarios.Add(usuario);
                    await _context.SaveChangesAsync();

                    // REDIRECIONAMENTO PÓS-CADASTRO: Volta para o Login
                    TempData["SuccessMessage"] = "Cadastro realizado com sucesso! Faça o login.";
                    return RedirectToAction("Login");
                }
                catch (Exception ex)
                {
                    await _logger.LogErrorAsync("AccountController:Cadastro", $"Erro ao cadastrar novo usuário: {model.Email}", ex);
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro inesperado. Tente novamente.");
                }
            }

            ViewBag.ExisteAdmin = await _context.Usuarios.AnyAsync(u => u.Funcao == "Administrador");
            return View(model);
        }

        // --------------------------------------------------------------------------------
        // LOGIN (GET)
        // --------------------------------------------------------------------------------
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Se o usuário já estiver logado, redireciona para o Dashboard correto
            if (User.Identity!.IsAuthenticated)
            {
                return RedirectToUserDashboard(User.FindFirstValue(ClaimTypes.Role)!);
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // --------------------------------------------------------------------------------
        // LOGIN (POST)
        // --------------------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl)
        {
            if (ModelState.IsValid)
            {
                // 1. Encontrar o usuário e verificar a senha
                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == model.Email);
                string senhaHash = HashSimples(model.Senha);

                if (usuario != null && usuario.SenhaHash == senhaHash)
                {
                    // cuidado com ERRO CS1061

                    // 2. Criar os Claims (Identidade)
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()), // ID do Usuário
                        new Claim(ClaimTypes.Name, usuario.Nome),
                        new Claim(ClaimTypes.Role, usuario.Funcao)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    // 3. Logar o usuário (Cria o Cookie)
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity));

                    // 4. Redirecionar
                    if (Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    // Redirecionamento por Funcao
                    return RedirectToUserDashboard(usuario.Funcao);
                }

                ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos. Tente novamente.");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        // --------------------------------------------------------------------------------
        // LOGOUT
        // --------------------------------------------------------------------------------
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account"); // Volta para a tela de Login
        }

        // --------------------------------------------------------------------------------
        // ACESSO NEGADO
        // --------------------------------------------------------------------------------
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // --------------------------------------------------------------------------------
        // ROTA PRIVADA: Redireciona para o Dashboard correto
        // --------------------------------------------------------------------------------
        private IActionResult RedirectToUserDashboard(string funcao)
        {
            return funcao switch
            {
                "Administrador" => RedirectToAction("Dashboard", "Admin"),
                // O Entregador é redirecionado para o seu Dashboard de Pedidos
                "Entregador" => RedirectToAction("MeusPedidos", "Pedido"),
                _ => RedirectToAction("Vitrine", "Home"), // Cliente vai para a loja/vitrine
            };
        }
    }
}