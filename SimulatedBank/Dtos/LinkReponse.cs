namespace SimulatedBank.Dtos
{
    public class LinkReponse
    {
        public bool Success { get; set; } 
        public Guid? ExternalReferenceId { get; set; }
        public string? Message { get; set; }
    }
}
