namespace BlobTutorial_V2.Models
{
    public class UserData(string excavatorName, string excavatorCompany, string excavatorLocation)
    {
        public string ExcavatorName { get; set; } = excavatorName;
        public string ExcavatorCompany { get; set; } = excavatorCompany;
        public string ExcavatorLocation { get; set; } = excavatorLocation;
    }
}