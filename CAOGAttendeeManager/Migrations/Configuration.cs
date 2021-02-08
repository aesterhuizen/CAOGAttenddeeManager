namespace CAOGAttendeeManager.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<CAOGAttendeeManager.AttendeeManagerDBModel>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            ContextKey = "CAOGAttendeeManager.AttendeeManagerDBModel";
        }

        protected override void Seed(CAOGAttendeeManager.AttendeeManagerDBModel context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method
            //  to avoid creating duplicate seed data.
        }
    }
}
