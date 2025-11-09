using System.ComponentModel.DataAnnotations;

namespace ReviewsParser.Api.Data
{
    public class ParsedReview
    {
        [Key]
        public int Id { get; set; }
        public int ParsingTaskId { get; set; }
        public string Car { get; set; }
        public string Author { get; set; }
        public string Rating { get; set; }
        public string Url { get; set; }
    }
}