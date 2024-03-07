public class Movie
{
    public int Id { get; set; }
    public string Title { get; set; }
    public DateTime ReleaseDate { get; set; }
    public List<Actor> Actors { get; set; }
    public List<Rating> Ratings { get; set; }
}