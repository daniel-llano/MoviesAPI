using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class MoviesController : ControllerBase
{
    private readonly MovieDbContext _context;

    public MoviesController(MovieDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetMovies(
        string? title = null, 
        string? actorName = null, 
        int? releaseYear = null, 
        int? minRating = null,
        int page = 1, 
        int pageSize = 10)
    {
        var query = _context.Movies
                            .Include(m => m.Actors)
                            .Include(m => m.Ratings)
                            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(title))
        {
            query = query.Where(m => m.Title.ToLower().Contains(title.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(actorName))
        {
            query = query.Where(m => m.Actors.Any(a => a.Name.ToLower().Contains(actorName.ToLower())));
        }

        if (releaseYear.HasValue)
        {
            query = query.Where(m => m.ReleaseDate.Year == releaseYear);
        }

        if (minRating.HasValue)
        {
            query = query.Where(m => m.Ratings.Any(r => r.Stars >= minRating));
        }

        // Count total records
        var totalCount = await query.CountAsync();

        // Calculate total pages
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        // Paginate the results
        var movies = await query
                            .Skip((page - 1) * pageSize)
                            .Take(pageSize)
                            .ToListAsync();

        var result = movies.Select(m => new
        {
            m.Id,
            m.Title,
            m.ReleaseDate,
            Actors = string.Join(", ", m.Actors.Select(a => a.Name)),
            AverageRating = m.Ratings.Any() ? m.Ratings.Average(r => r.Stars) : 0
        });

        return Ok(new { TotalPages = totalPages, Data = result });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Movie>> GetMovie(int id)
    {
        var movie = _context.Movies
        .Include(m => m.Actors)
        .Include(m => m.Ratings)
        .FirstOrDefault(m => m.Id == id);

        if (movie == null)
        {
            return NotFound();
        }

        var movieDto = new MovieDto
        {
            Id = movie.Id,
            Title = movie.Title,
            ReleaseDate = movie.ReleaseDate,
            Actors = movie.Actors.Select(a => new ActorDto { Id = a.Id, Name = a.Name, MovieId = a.MovieId }).ToList(),
            Ratings = movie.Ratings.Select(r => new RatingDto { Id = r.Id, Stars = r.Stars, MovieId = r.MovieId }).ToList()
        };

        return Ok(movieDto);
    }

    // POST: api/Movies
    [HttpPost]
    public async Task<ActionResult<Movie>> PostMovie(MovieDto movie)
    {
        var newMovie = new Movie { Title = movie.Title, ReleaseDate = movie.ReleaseDate, 
            Actors = new List<Actor>(), Ratings = new List<Rating>() };
        // Add movie to context
        _context.Movies.Add(newMovie);
        
        // If there are actors, add them
        if (movie.Actors != null && movie.Actors.Any())
        {
            foreach (var actor in movie.Actors)
            {
                var newActor = new Actor { Name = actor.Name, Movie = newMovie };
                _context.Actors.Add(newActor);
                newMovie.Actors.Add(newActor); 
            }
        }

        // If there are ratings, add them
        if (movie.Ratings != null && movie.Ratings.Any())
        {
            foreach (var rating in movie.Ratings)
            {
                var newRating = new Rating { Stars = rating.Stars, Movie = newMovie };
                _context.Ratings.Add(newRating);
                newMovie.Ratings.Add(newRating);
            }
        }

        // Save changes
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetMovie", new { id = movie.Id }, movie);
    }

    // PUT: api/Movies/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutMovie(int id, MovieDto updatedMovie)
    {
        if (id != updatedMovie.Id)
        {
            return BadRequest();
        }

        var existingMovie = await _context.Movies
                                            .Include(m => m.Actors)
                                            .Include(m => m.Ratings)
                                            .FirstOrDefaultAsync(m => m.Id == id);

        if (existingMovie == null)
        {
            return NotFound();
        }

        // Clear current actors and ratings
        existingMovie.Actors.Clear();
        existingMovie.Ratings.Clear();

        // Update movie properties
        existingMovie.Title = updatedMovie.Title;
        existingMovie.ReleaseDate = updatedMovie.ReleaseDate;

        // If there are actors, add them
        if (updatedMovie.Actors != null && updatedMovie.Actors.Any())
        {
            foreach (var actor in updatedMovie.Actors)
            {
                var newActor = new Actor { Name = actor.Name, Movie = existingMovie, MovieId = existingMovie.Id };
                existingMovie.Actors.Add(newActor); 
            }
        }

        // If there are ratings, add them
        if (updatedMovie.Ratings != null && updatedMovie.Ratings.Any())
        {
            foreach (var rating in updatedMovie.Ratings)
            {
                var newRating = new Rating { Stars = rating.Stars, Movie = existingMovie, MovieId = existingMovie.Id };
                existingMovie.Ratings.Add(newRating);
            }
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!MovieExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMovie(int id)
    {
        var movie = await _context.Movies
                                    .Include(m => m.Actors)
                                    .Include(m => m.Ratings)
                                    .FirstOrDefaultAsync(m => m.Id == id);

        if (movie == null)
        {
            return NotFound();
        }

        // Remove related actors
        _context.Actors.RemoveRange(movie.Actors);

        // Remove related ratings
        _context.Ratings.RemoveRange(movie.Ratings);

        // Remove the movie
        _context.Movies.Remove(movie);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("CleanAndResedData")]
    public IActionResult ImportData() {
        // Read movies from CSV and save to database
        using (var reader = new StreamReader("movies.csv"))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {   // Read records
            var records = csv.GetRecords<MovieRecord>().ToList();
            var movies = new List<Movie>();
            // Clean Actors table
            _context.Database.ExecuteSqlRaw("DELETE FROM Actors");
            // Clean Movies table
            _context.Database.ExecuteSqlRaw("DELETE FROM Movies");
            // Begin a transaction
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    foreach (var record in records)
                    {
                        // Create Movie entity
                        var movie = new Movie
                        {
                            Title = record.Title,
                            ReleaseDate = DateTime.Parse(record.ReleaseDate),
                            Actors = new List<Actor>()
                        };

                        // Split actors string and create Actor entities
                        var actorNames = record.Actors.Split(',');
                        foreach (var actorName in actorNames)
                        {
                            var actor = new Actor
                            {
                                Name = actorName.Trim(),
                                Movie = movie
                            };
                            movie.Actors.Add(actor);
                        }
                        movies.Add(movie);
                    }

                    // AddRange will queue up the entities to be added to the context
                    _context.Movies.AddRange(movies);

                    // SaveChanges will execute the SQL commands to insert the entities
                    _context.SaveChanges();

                    // Commit the transaction if all operations succeed
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    // Rollback the transaction if an exception occurs
                    Console.WriteLine($"Error occurred: {ex.Message}");
                    transaction.Rollback();
                }
            }
        }

        return NoContent();
    }

    private bool MovieExists(int id)
    {
        return _context.Movies.Any(e => e.Id == id);
    }
}
