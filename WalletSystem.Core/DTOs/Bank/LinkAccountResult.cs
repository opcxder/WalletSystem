

namespace WalletSystem.Core.DTOs.Bank
{
    public class LinkAccountResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }

        public Guid? ExternalReferenceId { get; set; }
    }
}
