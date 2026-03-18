namespace SportHub.Models.Entities
{
    public class Sport
    {
        public int SportID { get; set; }
        public string SportName { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
        public string? Description { get; set; }

        public ICollection<Court> Courts { get; set; } = new List<Court>();
    }

    public class CourtOwner
    {
        public int OwnerID { get; set; }
        public int UserID { get; set; }
        public User User { get; set; } = null!;

        public string BusinessName { get; set; } = string.Empty;
        public string? TaxCode { get; set; }
        public string? BankAccount { get; set; }
        public string? BankName { get; set; }
        public bool IsApproved { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<CourtVenue> Venues { get; set; } = new List<CourtVenue>();
    }

    public class CourtVenue
    {
        public int VenueID { get; set; }
        public int OwnerID { get; set; }
        public CourtOwner Owner { get; set; } = null!;

        public string VenueName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? Ward { get; set; }
        public string District { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? PhoneContact { get; set; }
        public TimeSpan OpenTime { get; set; } = new TimeSpan(6, 0, 0);
        public TimeSpan CloseTime { get; set; } = new TimeSpan(22, 0, 0);

        public bool AmenityParking { get; set; } = false;
        public bool AmenityShower { get; set; } = false;
        public bool AmenityLocker { get; set; } = false;
        public bool AmenityFood { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Court> Courts { get; set; } = new List<Court>();
    }

    public class Court
    {
        public int CourtID { get; set; }
        public int VenueID { get; set; }
        public CourtVenue Venue { get; set; } = null!;

        public int SportID { get; set; }
        public Sport Sport { get; set; } = null!;

        public string CourtName { get; set; } = string.Empty;
        public string? CourtType { get; set; } // Indoor, Outdoor
        public string? SurfaceType { get; set; }
        public byte MaxPlayers { get; set; } = 4;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<CourtImage> Images { get; set; } = new List<CourtImage>();
        public ICollection<PricingRule> PricingRules { get; set; } = new List<PricingRule>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }

    public class CourtImage
    {
        public int ImageID { get; set; }
        public int CourtID { get; set; }
        public Court Court { get; set; } = null!;

        public string ImageUrl { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public byte SortOrder { get; set; } = 0;
        public bool IsMain { get; set; } = false;
    }
}
