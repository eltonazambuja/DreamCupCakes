using DreamCupCakes.Data;
using DreamCupCakes.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq;

namespace DreamCupCakes.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Home/Index (Redirecionamento para a Vitrine se logado ou não)
        public IActionResult Index()
        {
            // Redireciona para a vitrine principal
            return RedirectToAction("Vitrine");
        }

        // GET: /Home/Vitrine (A tela principal da loja)
        public async Task<IActionResult> Vitrine()
        {
            // Seleciona APENAS os cupcakes que estão ATIVOS (Ativo == true)
            var cupcakes = await _context.Cupcakes
                                         .Where(c => c.Ativo)
                                         .OrderBy(c => c.Nome)
                                         .ToListAsync();

            return View(cupcakes);
        }

        // GET: /Home/Privacy (Manter a tela padrão se necessário)
        public IActionResult Privacy()
        {
            return View();
        }

        // GET: /Home/Error
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}