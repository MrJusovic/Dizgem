using Dizgem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Dizgem.Data
{
    // IdentityDbContext sınıfı, kullanıcılar ve roller için gerekli tabloları otomatik oluşturur.
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Modellerimizi veritabanı tablolarına dönüştürmek için DbSet'ler ekliyoruz.
        public DbSet<Post> Posts { get; set; }
        public DbSet<Page> Pages { get; set; }
        public DbSet<Theme> Themes { get; set; }
        public DbSet<Settings> Settings { get; set; }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<PostCategory> PostCategories { get; set; }
        public DbSet<PageCategory> PageCategories { get; set; }
        public DbSet<PostTag> PostTags { get; set; }

        public DbSet<Menu> Menus { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Burada tablolar arasındaki ilişkileri tanımlayabiliriz.
            // Örneğin: Bir yazarın birden fazla yazısı olabilir.
            builder.Entity<Post>()
                .HasOne(p => p.Author)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.AuthorId);

            builder.Entity<Page>()
                .HasOne(p => p.Author)
                .WithMany(u => u.Pages)
                .HasForeignKey(p => p.AuthorId);

            // PostCategory için bileşik anahtar anahtar (composite primary key) tanımlaması
            builder.Entity<PostCategory>()
                .HasKey(pc => new { pc.PostId, pc.CategoryId });

            // PageCategory için bileşik anahtar anahtar (composite primary key) tanımlaması
            builder.Entity<PageCategory>()
                .HasKey(pc => new { pc.PageId, pc.CategoryId });

            // Post -> PostCategory ilişkisi (bir yazının birden çok kategorisi olabilir)
            builder.Entity<PostCategory>()
                .HasOne(pc => pc.Post)
                .WithMany(p => p.PostCategories)
                .HasForeignKey(pc => pc.PostId);

            // Page -> PageCategory ilişkisi (bir yazının birden çok kategorisi olabilir)
            builder.Entity<PageCategory>()
                .HasOne(pc => pc.Page)
                .WithMany(p => p.PageCategories)
                .HasForeignKey(pc => pc.PageId);

            // Category -> PostCategory ilişkisi (bir kategorinin birden çok yazısı olabilir)
            builder.Entity<PostCategory>()
                .HasOne(pc => pc.Category)
                .WithMany(c => c.PostCategories)
                .HasForeignKey(pc => pc.CategoryId);

            // Category -> PageCategory ilişkisi (bir kategorinin birden çok yazısı olabilir)
            builder.Entity<PageCategory>()
                .HasOne(pc => pc.Category)
                .WithMany(c => c.PageCategories)
                .HasForeignKey(pc => pc.CategoryId);

            // PostTag için bileşik anahtar anahtar (composite primary key) tanımlaması
            builder.Entity<PostTag>()
                .HasKey(pt => new { pt.PostId, pt.TagId });

            // PageTag için bileşik anahtar anahtar (composite primary key) tanımlaması
            builder.Entity<PageTag>()
                .HasKey(pt => new { pt.PageId, pt.TagId });

            // Post -> PostTag ilişkisi
            builder.Entity<PostTag>()
                .HasOne(pt => pt.Post)
                .WithMany(p => p.PostTags)
                .HasForeignKey(pt => pt.PostId);

            // Page -> PostTag ilişkisi
            builder.Entity<PageTag>()
                .HasOne(pt => pt.Page)
                .WithMany(p => p.PageTags)
                .HasForeignKey(pt => pt.PageId);

            // Tag -> PostTag ilişkisi
            builder.Entity<PostTag>()
                .HasOne(pt => pt.Tag)
                .WithMany(t => t.PostTags)
                .HasForeignKey(pt => pt.TagId);

            builder.Entity<PageTag>()
                .HasOne(pt => pt.Tag)
                .WithMany(t => t.PageTags)
                .HasForeignKey(pt => pt.TagId);

            // MenuItem'ın kendi kendine olan ilişkisini (parent-child) tanımlar.
            // Bu, bir menü öğesinin birden çok alt öğesi olabileceğini,
            // ve bir alt öğenin sadece bir üst öğesi olabileceğini belirtir.
            builder.Entity<MenuItem>()
                .HasOne(m => m.Parent)
                .WithMany(m => m.Children)
                .HasForeignKey(m => m.ParentId)
                .OnDelete(DeleteBehavior.Restrict); // Bir üst öğe silinirse, veritabanı bütünlüğünü korumak
                                                    // için alt öğelerin silinmesini engeller.
                                                    // Önce alt öğeleri silmeniz veya başka bir yere taşımanız gerekir.
        }
    }
}
