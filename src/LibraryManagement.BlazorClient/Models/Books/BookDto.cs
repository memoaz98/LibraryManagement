namespace LibraryManagement.BlazorClient.Models.Books;

public class BookDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Isbn { get; set; } = string.Empty;
    public int PublicationYear { get; set; }
    public int CategoryId { get; set; }
    public string? Synopsis { get; set; }
}