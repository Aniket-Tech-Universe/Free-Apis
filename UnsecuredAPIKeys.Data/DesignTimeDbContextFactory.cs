using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UnsecuredAPIKeys.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DBContext>
    {
        public DBContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DBContext>();
            
            // Use the same connection string as your main application
            optionsBuilder.UseNpgsql("Host=localhost;Database=UnsecuredAPIKeys;Username=postgres;Password=devpassword;Port=5432", npgsqlOptions =>
            {
                npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                npgsqlOptions.EnableRetryOnFailure(3);
            });

            return new DBContext(optionsBuilder.Options);
        }
    }
}
