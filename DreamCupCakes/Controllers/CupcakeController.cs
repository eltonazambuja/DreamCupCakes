using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DreamCupCakes.Data;
using DreamCupCakes.Models;
using DreamCupCakes.Models.ViewModels;
using DreamCupCakes.Services;

namespace DreamCupCakes.Controllers
{
    // Apenas Administradores podem gerenciar Cupcakes
    [Authorize(Roles = "Administrador")]
    public class CupcakeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IErrorLogger _logger;

        public CupcakeController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment, IErrorLogger logger)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _logger = logger;
        }

        // Caminho relativo para a pasta de imagens de Cupcakes
        private const string CupcakeImagesFolder = "images/cupcakes";


        // --------------------------------------------------------------------------------
        // 1. LISTAGEM (READ)
        // --------------------------------------------------------------------------------
        // GET: /Cupcake/Index
        public async Task<IActionResult> Index()
        {
            var cupcakes = await _context.Cupcakes.OrderBy(c => c.Nome).ToListAsync();
            return View(cupcakes);
        }

        // --------------------------------------------------------------------------------
        // 2. CADASTRO (CREATE) - GET
        // --------------------------------------------------------------------------------
        // GET: /Cupcake/Cadastrar
        public IActionResult Cadastrar()
        {
            return View(new CupcakeViewModel());
        }

        // --------------------------------------------------------------------------------
        // 2. CADASTRO (CREATE) - POST
        // --------------------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cadastrar(CupcakeViewModel model)
        {
            // Validação customizada para o arquivo: é obrigatório no cadastro
            if (model.FotoArquivo == null)
            {
                ModelState.AddModelError("FotoArquivo", "A foto do cupcake é obrigatória.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Processar Upload da Imagem
                    string wwwRootPath = _hostEnvironment.WebRootPath;
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.FotoArquivo!.FileName);
                    string targetPath = Path.Combine(wwwRootPath, CupcakeImagesFolder, fileName);

                    // Garante que o diretório exista
                    Directory.CreateDirectory(Path.Combine(wwwRootPath, CupcakeImagesFolder));

                    using (var fileStream = new FileStream(targetPath, FileMode.Create))
                    {
                        await model.FotoArquivo.CopyToAsync(fileStream);
                    }

                    // 2. Mapear e Salvar no DB
                    var cupcake = new Cupcake
                    {
                        Nome = model.Nome,
                        Descricao = model.Descricao,
                        Valor = model.Valor,
                        Ativo = model.Ativo,
                        FotoUrl = $"/{CupcakeImagesFolder}/{fileName}"
                    };

                    _context.Add(cupcake);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Cupcake '{cupcake.Nome}' cadastrado com sucesso!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await _logger.LogErrorAsync("CupcakeController:Cadastrar(POST)", "Erro ao salvar novo Cupcake ou upload de arquivo.", ex);
                    TempData["ErrorMessage"] = "Erro interno ao cadastrar. Verifique o log de erros.";
                }
            }

            // Retorna a View com o modelo e os erros
            return View(model);
        }

        // --------------------------------------------------------------------------------
        // 3. EDIÇÃO (UPDATE) - GET
        // --------------------------------------------------------------------------------
        // GET: /Cupcake/Editar/5
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null) return NotFound();

            var cupcake = await _context.Cupcakes.FindAsync(id);
            if (cupcake == null) return NotFound();

            // Mapear Model de DB para ViewModel para a edição
            var model = new CupcakeViewModel
            {
                Id = cupcake.Id,
                Nome = cupcake.Nome,
                Descricao = cupcake.Descricao,
                Valor = cupcake.Valor,
                Ativo = cupcake.Ativo,
                FotoUrlExistente = cupcake.FotoUrl 
            };
            return View(model);
        }

        // --------------------------------------------------------------------------------
        // 3. EDIÇÃO (UPDATE) - POST
        // --------------------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, CupcakeViewModel model)
        {
            if (id != model.Id) return NotFound();

            // 1. Busca o objeto existente para referência
            var cupcakeFromDb = await _context.Cupcakes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (cupcakeFromDb == null) return NotFound();

            // Remove a necessidade do arquivo ser obrigatório, pois já existe um.
            ModelState.Remove("FotoArquivo");

            if (ModelState.IsValid)
            {
                string newFotoUrl = cupcakeFromDb.FotoUrl;

                try
                {
                    // 2. Processar novo Upload, se houver
                    if (model.FotoArquivo != null)
                    {
                        // Deletar foto antiga (boa prática)
                        if (!string.IsNullOrEmpty(cupcakeFromDb.FotoUrl))
                        {
                            var oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, cupcakeFromDb.FotoUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        // Salvar nova foto
                        string wwwRootPath = _hostEnvironment.WebRootPath;
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.FotoArquivo.FileName);
                        string targetPath = Path.Combine(wwwRootPath, CupcakeImagesFolder, fileName);

                        using (var fileStream = new FileStream(targetPath, FileMode.Create))
                        {
                            await model.FotoArquivo.CopyToAsync(fileStream);
                        }
                        newFotoUrl = $"/{CupcakeImagesFolder}/{fileName}";
                    }

                    // 3. Mapear para o Model de DB e salvar
                    var cupcake = new Cupcake
                    {
                        Id = model.Id,
                        Nome = model.Nome,
                        Descricao = model.Descricao,
                        Valor = model.Valor,
                        Ativo = model.Ativo,
                        FotoUrl = newFotoUrl // Salva o novo ou o antigo URL
                    };

                    _context.Update(cupcake);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Cupcake '{cupcake.Nome}' atualizado com sucesso!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await _logger.LogErrorAsync("CupcakeController:Editar(POST)", "Erro ao atualizar Cupcake ou upload de arquivo.", ex);
                    TempData["ErrorMessage"] = "Erro interno ao atualizar. Verifique o log de erros.";
                }
            }

            // Garante que o URL existente seja passado de volta em caso de erro
            model.FotoUrlExistente = cupcakeFromDb.FotoUrl;
            return View(model);
        }

        // --------------------------------------------------------------------------------
        // 4. DELEÇÃO (DELETE)
        // --------------------------------------------------------------------------------
        // POST: /Cupcake/Deletar/5
        [HttpPost, ActionName("Deletar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletarConfirmado(int id)
        {
            var cupcake = await _context.Cupcakes.FindAsync(id);

            if (cupcake == null)
            {
                TempData["ErrorMessage"] = "Cupcake não encontrado.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // 1. Deletar Arquivo de Imagem no disco
                if (!string.IsNullOrEmpty(cupcake.FotoUrl))
                {
                    var imagePath = Path.Combine(_hostEnvironment.WebRootPath, cupcake.FotoUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                // 2. Deletar Registro no DB
                _context.Cupcakes.Remove(cupcake);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Cupcake '{cupcake.Nome}' deletado com sucesso!";
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("CupcakeController:Deletar", $"Erro ao deletar Cupcake {id} e seu arquivo.", ex);
                TempData["ErrorMessage"] = "Erro ao deletar Cupcake. Verifique o log de erros.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}