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
            AttendanceList = new List<Attendance_Info> { };

        }
        public int AttendeeId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
       
        public virtual List<Attendance_Info> AttendanceList { get; set; }
       

    }

    public class Attendance_Info
    {
        //public Attendance_Info()
        //{
        //    lstActivities = new List<Activity> { };
        //}
        public int Attendance_InfoId { get; set; }
        public int AttendeeId { get; set; }
        public virtual Attendee Attendee { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
      //  public virtual List<Activity> lstActivities { get; set; }
 
        //public string Phone { get; set; }
        //public string Email { get; set; }
    }
    public class AttRecord
    {
        public int id;
        public string fname;
        public string lname;
        public DateTime date;
        public string status;



    }
    public class Activity
    {
        public Activity()
        {
            lstActivityName = new List<string> { };
        }
        public string Header { get; set; }
        public List<string> lstActivityName { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }


    }

    public class BinaryTreeNode
    {
        public BinaryTreeNode(string header, string data=null)
        {
            this.Name = header;
            this.Metadata = data;
        }

        public string Name { get; set; }
        public string Metadata { get; set; }
        public BinaryTreeNode Left { get; set; }
        public BinaryTreeNode Right { get; set; }

        public void set_left(BinaryTreeNode new_left)
        {
            Left = new_left;

        }

        public void set_right(BinaryTreeNode new_right)
        {
            Right = new_right;
        }


    }
    public enum TreeTraversal
    {
        BinTreeTraversal,
        
    }

   



}
