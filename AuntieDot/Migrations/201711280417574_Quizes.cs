namespace AuntieDot.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Quizes : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Quizs",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        DiscountCode = c.String(),
                        CompletionTally = c.Int(nullable: false),
                        ConversionTally = c.Int(nullable: false),
                        ApplicationUser_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.ApplicationUser_Id)
                .Index(t => t.ApplicationUser_Id);
            
            CreateTable(
                "dbo.QuizQuestions",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Question = c.String(),
                        Quiz_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Quizs", t => t.Quiz_Id)
                .Index(t => t.Quiz_Id);
            
            CreateTable(
                "dbo.QuizAnswers",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Answer = c.String(),
                        ChosenTally = c.Int(nullable: false),
                        QuizQuestion_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.QuizQuestions", t => t.QuizQuestion_Id)
                .Index(t => t.QuizQuestion_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Quizs", "ApplicationUser_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.QuizQuestions", "Quiz_Id", "dbo.Quizs");
            DropForeignKey("dbo.QuizAnswers", "QuizQuestion_Id", "dbo.QuizQuestions");
            DropIndex("dbo.QuizAnswers", new[] { "QuizQuestion_Id" });
            DropIndex("dbo.QuizQuestions", new[] { "Quiz_Id" });
            DropIndex("dbo.Quizs", new[] { "ApplicationUser_Id" });
            DropTable("dbo.QuizAnswers");
            DropTable("dbo.QuizQuestions");
            DropTable("dbo.Quizs");
        }
    }
}
