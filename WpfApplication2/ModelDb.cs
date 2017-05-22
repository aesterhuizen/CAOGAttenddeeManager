namespace CAOGAttendeeProject
{
    using System;
    using System.Data.Entity;
    using System.Collections.Generic;
    using System.Linq;

    public class ModelDb : DbContext
    {
        // Your context has been configured to use a 'ModelDb' connection string from your application's 
        // configuration file (App.config or Web.config). By default, this connection string targets the 
        // 'WpfApplication2.ModelDb' database on your LocalDb instance. 
        // 
        // If you wish to target a different database and/or database provider, modify the 'ModelDb' 
        // connection string in the application configuration file.
        public ModelDb(string constr) : base(constr)
        {
        }

        // Add a DbSet for each entity type that you want to include in your model. For more information 
        // on configuring and using a Code First model, see http://go.microsoft.com/fwlink/?LinkId=390109.

        // public virtual DbSet<MyEntity> MyEntities { get; set; }
        public virtual DbSet<Attendee> Attendees { get; set; }
        public virtual DbSet<Attendance_Info> Attendance_Info { get; set; }
    }
    //    
    //}

    public class Attendee
    {

        public Attendee()
        {
            this.AttendanceList = new List<Attendance_Info> { };

        }
        public int AttendeeId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public virtual List<Attendance_Info> AttendanceList { get; set; }

        
    }

    public class Attendance_Info
    {

        public int Attendance_InfoId { get; set; }
        public int AttendeeId { get; set; }
        //public virtual Attendee Attendee { get; set; }
        private bool HasThreeConsequitiveFollowUps { get; set; }

        public DateTime Last_Attended { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }

        
    }
}