namespace CAOGAttendeeProject
{
    using System;
    using System.ComponentModel;
    using System.Data.Entity;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
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
            this.AttendanceList = new ObservableCollection<Attendance_Info>();

        }
        public int AttendeeId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        // keep Prospect field for legacy purposes
        public int Prospect { get; set; }

        public virtual ObservableCollection<Attendance_Info> AttendanceList { get; private set; }
       

    }

    public class Attendance_Info
    {
        
        public int Attendance_InfoId { get; set; }
        public int AttendeeId { get; set; }

        public DateTime Date { get; set; }
        public string Status { get; set; }
        public string Activity { get; set; }
        public DateTime? ActivityDate { get; set; }

        public string Phone { get; set; }
        public string Email { get; set; }

        public virtual Attendee Attendee { get; set; }

        
    }
    public class AttRecord
    {
        public int id;
        public string fname;
        public string lname;
        public DateTime? date;
        public string status;
        public DateTime? activity_date;
        public string activity;
        public string phone;
        public string email;






    }

  

    public class TabState
    {
        public TabState()
        {
            txtSearchActiveState = "";
            txtSearchActivityState = "";
            txtSearchProspectState = "";

            

            ProspectTab_isFilterbyDateChecked = false;

            ActiveTab_isFilterbyActivityDateChecked = false;
            ActiveTab_isActivityChecked = false;

            ActiveTab_isFilterbyDateChecked = false;
            ActivityTab_isActivityDateChecked = false;
            ActivityTab_isActivityChecked = false;

            ActiveTab_isAttendedChecked = false;
            ActiveTab_isRespondedChecked = false;
            ActiveTab_isFollowUpChecked = false;
            ActiveTab_isChurchStatusChecked = false;
        }
        public string txtSearchActiveState { get; set; }
        public string txtSearchProspectState { get; set; }
        public string txtSearchActivityState { get; set; }

        public bool? ActiveTab_isAttendedChecked { get; set;  }
        public bool? ActiveTab_isRespondedChecked { get; set; }
        public bool? ActiveTab_isFollowUpChecked { get; set; }
        public bool? ActiveTab_isActivityChecked { get; set; }
        public bool? ActiveTab_isChurchStatusChecked { get; set; }

        public bool? ActiveTab_isFilterbyDateChecked { get; set; }
        public bool? ProspectTab_isFilterbyDateChecked { get; set; }

        public bool? ActiveTab_isFilterbyActivityDateChecked { get; set; }
        public bool? ActivityTab_isActivityChecked { get; set; }

        public bool? ActivityTab_isActivityDateChecked { get; set; }

    }
   
    public class ActivityGroup : INotifyPropertyChanged
    {
        public ActivityGroup()
        {
            this.lstActivityTasks = new ObservableCollection<ActivityTask>();
        }
        
        public string ActivityName { get; set; }
        private bool _IsSelected = false;

        public event PropertyChangedEventHandler PropertyChanged;

        #region INotifyPropertyChanged Members


        private void OnChanged(string prop)
        {
            this.PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        public bool IsSelected
        {
            get { return _IsSelected; }
            set { _IsSelected = value; OnChanged("IsChecked"); }
        }

        public ObservableCollection<ActivityTask> lstActivityTasks { get; set; }
    }
    public class ActivityTask : INotifyPropertyChanged
    {

        public ActivityTask()
        {
            this.lstsubTasks = new ObservableCollection<ActivityTask>();
        }
        public string TaskName { get; set; }
        public string Description { get; set; }
        private bool _IsSelected = false;

        public event PropertyChangedEventHandler PropertyChanged;

        #region INotifyPropertyChanged Members


        private void OnChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        public bool IsSelected
        {
            get { return _IsSelected; }

            set
            {
                _IsSelected = value;
                OnChanged("IsChecked");
            }
        }

      

        public ObservableCollection<ActivityTask> lstsubTasks { get; set; }
    }





}
