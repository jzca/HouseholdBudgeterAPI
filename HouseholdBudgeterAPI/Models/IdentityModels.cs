using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using HouseholdBudgeterAPI.Models.Domain;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;

namespace HouseholdBudgeterAPI.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit https://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser()
        {
            OwnedHouseholds = new List<Household>();
            JoinedHouseholds = new List<Household>();
            InvitedHouseholds = new List<Household>();

        }
        [InverseProperty(nameof(Household.Owner))]
        public virtual List<Household> OwnedHouseholds { get; set; }
        [InverseProperty(nameof(Household.JoinedUsers))]
        public virtual List<Household> JoinedHouseholds { get; set; }
        [InverseProperty(nameof(Household.InvitedUsers))]
        public virtual List<Household> InvitedHouseholds { get; set; }




        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager, string authenticationType)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, authenticationType);
            // Add custom user claims here
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {

        public ApplicationDbContext()
                : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public DbSet<Household> Households { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<Transaction> Transactions { get; set; }


        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>()
                .HasMany(p => p.InvitedHouseholds)
                .WithMany(p => p.InvitedUsers)
                .Map(p => p.ToTable("InvitedUsersHouseholds"));

            modelBuilder.Entity<ApplicationUser>()
               .HasMany(p => p.JoinedHouseholds)
               .WithMany(p => p.JoinedUsers)
               .Map(p => p.ToTable("JoinedUsersHouseholds"));

            modelBuilder.Entity<Household>()
                .HasMany(p => p.Categories)
                .WithRequired(p => p.Household)
                .WillCascadeOnDelete(false);
        }
    }
}