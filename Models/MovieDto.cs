public class MovieDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public DateTime ReleaseDate { get; set; }
    public List<ActorDto> Actors { get; set; }
    public List<RatingDto> Ratings { get; set; }
}

public class ActorDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int MovieId { get; set; }
}

public class RatingDto
{
    public int Id { get; set; }
    public int Stars { get; set; }
    public int MovieId { get; set; }
}