using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace AuntieDot.Models {
    public class QuizQuestion {
        [Key]
        public int Id { get; set; }
        public string Question { get; set; }
        public virtual Collection<QuizAnswer> Answers { get; set; } = new Collection<QuizAnswer>();
    }
}