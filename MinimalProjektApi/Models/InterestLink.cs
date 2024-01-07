namespace MinimalProjektApi.Models
{
    public class InterestLink
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
        public int InterestId { get; set; }
        public string Url { get; set; }

        public virtual Person Persons { get; set; }
        public virtual Interest Interests { get; set; }
    }
}
