using GestaoFinanceiraPessoal.Models;
using Microsoft.EntityFrameworkCore;

namespace GestaoFinanceiraPessoal.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Receita> Receitas { get; set; }
        public DbSet<Despesa> Despesas { get; set; }
        public DbSet<Categoria> Categorias { get; set; }

        public DbSet<Recorrencia> Recorrencias { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Categoria 1:N Despesas
            modelBuilder.Entity<Categoria>()
                .HasMany(c => c.Despesas)
                .WithOne(d => d.Categoria)
                .HasForeignKey(d => d.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
