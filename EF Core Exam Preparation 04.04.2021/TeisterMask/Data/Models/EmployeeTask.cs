using System.ComponentModel.DataAnnotations.Schema;

namespace TeisterMask.Data.Models
{
    public class EmployeeTask
    {
        public int EmployeeId { get; set; }
        [ForeignKey(nameof(EmployeeId))]
        public Employee Employee { get; set; }


        public int TaskId { get; set; }
        [ForeignKey(nameof(TaskId))]
        public Task Task { get; set; }
    }
}