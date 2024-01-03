

using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using TeisterMask.Data.Models;

namespace TeisterMask.DataProcessor.ImportDto
{
    public class ImportEmployeesDTO
    {
        [Required]
        [MaxLength(40)]
        [MinLength(3)]
        [RegularExpression(@"[A-Za-z0-9]+")]
        [JsonProperty("Username")]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        [JsonProperty("Email")]
        public string Email { get; set; }

        [Required]
        [JsonProperty("Phone")]
        [RegularExpression(@"^\d{3}-\d{3}-\d{4}\b")]
        public string Phone { get; set; }

        [JsonProperty("Tasks")]
        public int[] Tasks { get; set; }
    }
}
