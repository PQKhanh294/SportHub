namespace SportHub.Models.ViewModels
{
    public class SportViewModel
    {
        public int SportID { get; set; }
        public string SportName { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
        public string? Description { get; set; }
    }
}
