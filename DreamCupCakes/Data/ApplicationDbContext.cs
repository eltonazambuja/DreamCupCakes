using Microsoft.EntityFrameworkCore;
using DreamCupCakes.Models;

namespace DreamCupCakes.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Cupcake> Cupcakes { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<ItemPedido> ItensPedido { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Cliente)
                .WithMany(u => u.PedidosFeitos) 
                .HasForeignKey(p => p.ClienteId)
                .OnDelete(DeleteBehavior.Restrict); 

            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Entregador)
                .WithMany(u => u.PedidosParaEntregar) 
                .HasForeignKey(p => p.EntregadorId)
                .IsRequired(false) 
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Email)
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }
    }
}