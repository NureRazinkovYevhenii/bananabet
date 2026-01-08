using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BananaBet_API.Models
{
    public class EloSnapshot
    {
        // Composite key (Date, Club)
        public DateOnly Date { get; set; }
        public string Club { get; set; } = null!;
        public double Elo { get; set; }
    }
}

