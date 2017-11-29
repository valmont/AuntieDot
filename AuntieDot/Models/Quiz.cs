using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace AuntieDot.Models {
    public class Quiz {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string DiscountCode { get; set; }
        public int CompletionTally { get; set; }
        public int ConversionTally { get; set; }
        public virtual Collection<QuizQuestion> Questions { get; set; } = new Collection<QuizQuestion>();
    }
}