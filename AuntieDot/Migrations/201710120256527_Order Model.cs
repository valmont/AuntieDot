namespace AuntieDot.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class OrderModel : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Orders",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ShopifyId = c.Long(nullable: false),
                        DisplayId = c.String(),
                        LineItemSummary = c.String(),
                        CustomerName = c.String(),
                        DateCreated = c.DateTime(nullable: false),
                        IsOpen = c.Boolean(nullable: false),
                        ApplicationUser_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.ApplicationUser_Id)
                .Index(t => t.ApplicationUser_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Orders", "ApplicationUser_Id", "dbo.AspNetUsers");
            DropIndex("dbo.Orders", new[] { "ApplicationUser_Id" });
            DropTable("dbo.Orders");
        }
    }
}
