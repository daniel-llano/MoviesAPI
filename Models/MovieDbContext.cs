using Microsoft.EntityFrameworkCore;

public class MovieDbContext : DbContext
{
    public MovieDbContext(DbContextOptions<MovieDbContext> options) : base(options)
    {
    }

    public DbSet<Movie> Movies { get; set; }
    public DbSet<Actor> Actors { get; set; }
    public DbSet<Rating> Ratings { get; set; }
    public DbSet<Person> Persons { get; set; }
}
