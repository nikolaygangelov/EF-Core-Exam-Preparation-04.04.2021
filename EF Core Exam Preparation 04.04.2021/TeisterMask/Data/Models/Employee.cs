
using System.ComponentModel.DataAnnotations;

namespace TeisterMask.Data.Models
{
    public class Employee
    {
        public Employee()
        {
                EmployeesTasks = new HashSet<EmployeeTask>();
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(40)]
        [RegularExpression(@"[A-Za-z0-9]")]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email  { get; set; }

        [Required]
        public string Phone { get; set; }
        [RegularExpression(@"\d{3}-\d{3}-\d{4}")]
        public ICollection<EmployeeTask> EmployeesTasks  { get; set;}
    }
}
