namespace CAOGAttendeeManager.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Activities",
                c => new
                    {
                        ActivityId = c.Int(nullable: false, identity: true),
                        AttendeeId = c.Int(nullable: false),
                        ActivityText = c.String(unicode: false),
                        DateString = c.String(unicode: false),
                        Date = c.DateTime(),
                    })
                .PrimaryKey(t => t.ActivityId)
                .ForeignKey("dbo.Attendees", t => t.AttendeeId, cascadeDelete: true)
                .Index(t => t.AttendeeId);
            
            CreateTable(
                "dbo.Attendees",
                c => new
                    {
                        AttendeeId = c.Int(nullable: false, identity: true),
                        Checked = c.Boolean(nullable: false),
                        LastName = c.String(unicode: false),
                        FirstName = c.String(unicode: false),
                    })
                .PrimaryKey(t => t.AttendeeId);
            
            CreateTable(
                "dbo.Attendance_Info",
                c => new
                    {
                        Attendance_InfoId = c.Int(nullable: false, identity: true),
                        AttendeeId = c.Int(nullable: false),
                        DateString = c.String(unicode: false),
                        Date = c.DateTime(),
                        Status = c.String(unicode: false),
                    })
                .PrimaryKey(t => t.Attendance_InfoId)
                .ForeignKey("dbo.Attendees", t => t.AttendeeId, cascadeDelete: true)
                .Index(t => t.AttendeeId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Attendance_Info", "AttendeeId", "dbo.Attendees");
            DropForeignKey("dbo.Activities", "AttendeeId", "dbo.Attendees");
            DropIndex("dbo.Attendance_Info", new[] { "AttendeeId" });
            DropIndex("dbo.Activities", new[] { "AttendeeId" });
            DropTable("dbo.Attendance_Info");
            DropTable("dbo.Attendees");
            DropTable("dbo.Activities");
        }
    }
}
