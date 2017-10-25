namespace AuntieDot.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class emailwidget : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "WidgetTitle", c => c.String());
            AddColumn("dbo.AspNetUsers", "WidgetBlurb", c => c.String());
            AddColumn("dbo.AspNetUsers", "WidgetHexColor", c => c.String());
            AddColumn("dbo.AspNetUsers", "ScriptTagId", c => c.Long());
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "ScriptTagId");
            DropColumn("dbo.AspNetUsers", "WidgetHexColor");
            DropColumn("dbo.AspNetUsers", "WidgetBlurb");
            DropColumn("dbo.AspNetUsers", "WidgetTitle");
        }
    }
}
