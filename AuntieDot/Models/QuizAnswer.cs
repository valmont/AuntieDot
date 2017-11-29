using System.ComponentModel.DataAnnotations;

namespace AuntieDot.Models {
    public class QuizAnswer {
        [Key]
        public int Id { get; set; }

        public string Answer { get; set; }
        public int ChosenTally { get; set; }
    }
}