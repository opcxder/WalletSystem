

using System.ComponentModel.DataAnnotations;

namespace WalletSystem.Infrastructure.Config
{
    public class SmtpSettings
    {
        [Required]
        public string Server { get; set; }

        [Range(1,65535)]
        public int Port { get; set; }

        [Required]
        public string SenderName { get; set; }

        [Required]
        public string SenderEmail { get; set; }

        [Required]
        public string SenderUsername { get; set; }

        [Required]
        public string AppPassword {get;set;}
    }
}
