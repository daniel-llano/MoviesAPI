using CsvHelper.Configuration.Attributes;
public class MovieRecord
{
    [Name("Title")]
    public string Title { get; set; }

    [Name("Release Date")]
    public string ReleaseDate { get; set; }

    [Name("Actors")]
    public string Actors { get; set; }
}