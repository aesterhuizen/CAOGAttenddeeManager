namespace CAOGAttendeeProject
{
    using System;
    using System.Windows;
    using System.Data;
    using System.ComponentModel;
    using System.Data.Entity;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Collections;
    using System.Collections.Specialized;

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
        public virtual DbSet<ActivityPair> Activities { get; set; }
    }
    //    
    //}

    public class Attendee : INotifyPropertyChanged
    {

        public Attendee()
        {
            AttendanceList = new ObservableCollection<Attendance_Info>() { };
            ActivityList = new ObservableCollection<ActivityPair>() { };

        }
        public int AttendeeId { get; set; }
        private string _lastname = "";
        public string LastName
        {
            get
            {
                return _lastname;
            }
            set
            {
                if (_lastname != value)
                {
                    _lastname = value;
                }
                NotifyPropertyChanged("LastName");
            }
        }
        private string _firstname = "";
        public string FirstName
        {
            get
            {
                return _firstname;
            }
            set
            {

                if (_firstname != value)
                {
                    _firstname = value;
                }
                NotifyPropertyChanged("FirstName");

            }

        }

        private string _phone = "";
        public string Phone
        {
            get
            {
                return _phone;
            }
            set
            {

                if (_phone != value)
                {
                    _phone = value;
                }
                NotifyPropertyChanged("Phone");

            }
        }

        private string _email = "";
        public string Email
        {
            get
            {
                return _email;
            }
            set
            {

                if (_email != value)
                {
                    _email = value;
                }
              //  NotifyPropertyChanged("Email");

            }
        }

        public virtual ObservableCollection<Attendance_Info> AttendanceList { get; private set; } 
        public virtual ObservableCollection<ActivityPair> ActivityList { get; private set; }

    public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Attendance_Info 
    {


        public int Attendance_InfoId { get; set; }
        public int AttendeeId { get; set; }




     
        public string DateString { get; private set; }
        private DateTime _date;
        public DateTime Date
        {
            get
            {
                return _date;
            }

            set
            {
                if (_date != value)
                {
                    _date = value;
                    DateString = _date.ToString("MM-dd-yyyy");

                   
                }
            }
        }



        public string Status { get; set; }

        public virtual Attendee Attendee { get; set; }

        
    }


    public class ActivityPair :INotifyPropertyChanged
    {
       
        public ActivityPair()
        {

        }
        public int ActivityPairId {get; set; }
        public int AttendeeId { get; set; }

        public virtual Attendee Attendee { get; set;}
        public string ActivityGroup { get; set; }

        public static bool operator!= (ActivityPair A, ActivityPair B)
        {

            if (!(A is null) && !(B is null))
            {
                if (!(A.ToString().Equals(B.ToString())))
                {
                    return true;
                }
            }
            else if ( !(A is null) && (B is null) )
            {
                return true;
            }
         
                return false;

        }

        public static bool operator ==(ActivityPair A, ActivityPair B)
        {

           
            if (!(A is null) && !(B is null) )
            {
                if (A.ToString().Equals(B.ToString()))
                {
                    return true;

                }
            }
            else if ((A is null) && (B is null))
            {
                return true;
            }

            return false;
            
          
                
          

        }

        public override string ToString()
        {
            if (ParentTaskName == "" && ChildTaskName == "")
            {
                return "n/a";
            }
            else if (ChildTaskName == "")
            {

                return ActivityGroup + "->" + ParentTaskName;
            }
            else if (ParentTaskName != "" && ChildTaskName != "")
            {
                return ActivityGroup + "->" + ParentTaskName + "->" + ChildTaskName;
            }
            else
            {
                return "n/a";
            }
            

        }
        public string ParentTaskName
        {
            get
            {
                return _parenttaskname;
            }

            set
            {
                if (_parenttaskname != value)
                {
                    _parenttaskname = value;
                    NotifyPropertyChanged("ParentTaskName");
                }
            }
        }
        public string ChildTaskName
        {
            get
            {
                return _childtaskname;
            }

            set
            {
                if (_childtaskname != value)
                {
                    _childtaskname = value;
                    NotifyPropertyChanged("ChildTaskName");
                }
            }
        }

    
        public string DateString { get; private set; }
        private DateTime? _date = null;
        public DateTime? Date
        {
            get
            {
                return _date;
            }

            set
            {
                if (_date != value)
                {
                    _date = value;
                    DateString = _date?.ToString("MM-dd-yyyy");

                    NotifyPropertyChanged("DateString");
                }
            }
        }
        private string _parenttaskname = "";
        private string _childtaskname = "";
       

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }

    public class DefaultTableRow : INotifyPropertyChanged
    {


      

        public DefaultTableRow()
        {
            AttendanceList = new ObservableCollection<Attendance_Info>() { };
            ActivityList = new ObservableCollection<ActivityPair>() { };

        }
        public int AttendeeId { get; set; }

        public string FirstLastName { get; set; }

        private string _lastname = "";
        public string LastName
        {
            get
            {
                return _lastname;
            }
            set
            {
                if (_lastname != value)
                {
                    _lastname = value;
                }
                NotifyPropertyChanged("LastName");
            }
        }
        private string _firstname = "";
        public string FirstName
        {
            get
            {
                return _firstname;
            }
            set
            {
                
                    if (_firstname != value)
                    {
                        _firstname = value;
                    }
                    NotifyPropertyChanged("FirstName");
                
            }

        }
        private string _activity = "";

        public string Activity
        {
            get
            {
                return _activity;
            }

            set
            {
                _activity = value;
                NotifyPropertyChanged("Activity");
            }

        }

        public virtual ObservableCollection<ActivityPair> ActivityList { get; set; }

        public virtual ObservableCollection<Attendance_Info> AttendanceList { get; set; }

     


        private string _church_last_attended = "";
        public string Church_Last_Attended
        {
            get
            {
                return _church_last_attended;
            }
            set
            {
                _church_last_attended = value;
                NotifyPropertyChanged("Church_Last_Attended");
            }
        }
        private string _activity_last_attended = "";
        public string Activity_Last_Attended
        {
            get
            {
                return _activity_last_attended;
            }

            set
            {


                _activity_last_attended = value;
                NotifyPropertyChanged("Activity_Last_Attended");

            }
        }
        private string _church_status = "";
        public string ChurchStatus
        {
            get
            {
                return _church_status;
            }
            set
            {
                _church_status = value;
                NotifyPropertyChanged("ChurchStatus");
            }
        }
        private string _phone = "";
        public string Phone
        {
            get
            {
                return _phone;
            }
            set
            {

                if (_phone != value)
                {
                    _phone = value;
                }
                NotifyPropertyChanged("Phone");

            }
        }

        private string _email = "";
        public string Email
        {
            get
            {
                return _email;
            }
            set
            {

                if (_email != value)
                {
                    _email = value;
                }
                NotifyPropertyChanged("Email");

            }
        }

       

       

        public event PropertyChangedEventHandler PropertyChanged;
      
     

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        

       
    }
    public class AttendanceTableRow : INotifyPropertyChanged
    {
        public AttendanceTableRow()
        {
            IsNewrow = false;
            IsModifiedrow = false;
            
        }
        private int _attendeeId = 0;
        private bool _isModified = false;

        public bool IsNewrow { get; set; }
        public bool IsModifiedrow
        {
            get
            {
                return _isModified;
            }
            set
            {
                if (_isModified != value)
                {
                    _isModified = value;
                  // NotifyPropertyChanged("IsModifiedrow");
                }
                
            }

        }

        public int AttendeeId
        {
            get
            {
                return _attendeeId;
            }

            set
            {
                if (_attendeeId != value)
                {
                    _attendeeId = value;
                    NotifyPropertyChanged("AttendeeId");
                }
            }

        }
        public string FirstLastName { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string DateString { get; set; }

    
        public bool Attended { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

       
    }
  

    public class ActivityRecord
    {
        public int id;
        public string fname;
        public string lname;
        public string activity_date;
        public string activity;
        
    }
    public class AttRecord
    {
        public int id;
        public string fname;
        public string lname;
        public DateTime? date;

        public string status;
        public string activity_date;
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

            

            ProspectPanel_isFilterbyDateChecked = false;

            ActivePanel_isFilterbyActivityDateChecked = false;
            ActivePanel_isActivityChecked = false;

            ActivePanel_isFilterbyDateChecked = false;

            ActivityPanel_isActivityDateChecked = false;
            ActivityPanel_isActivityFilterChecked = false;

            ActivePanel_isAttendedChecked = false;
            ActivePanel_isRespondedChecked = false;
            ActivePanel_isFollowUpChecked = false;
            ActivePanel_isChurchStatusChecked = false;
        }
        public string txtSearchActiveState { get; set; }
        public string txtSearchProspectState { get; set; }
        public string txtSearchActivityState { get; set; }

        public bool? ActivePanel_isAttendedChecked { get; set;  }
        public bool? ActivePanel_isRespondedChecked { get; set; }
        public bool? ActivePanel_isFollowUpChecked { get; set; }
        public bool? ActivePanel_isActivityChecked { get; set; }
        public bool? ActivePanel_isChurchStatusChecked { get; set; }

        public bool? ActivePanel_isFilterbyDateChecked { get; set; }
        public bool? ProspectPanel_isFilterbyDateChecked { get; set; }

        public bool? ActivePanel_isFilterbyActivityDateChecked { get; set; }
        public bool? ActivityPanel_isActivityFilterChecked { get; set; }


        public bool? ActivityPanel_isActivityDateChecked { get; set; }

    }

    public class ActivityGroup : INotifyPropertyChanged
    {
        public ActivityGroup()
        {
            lstActivityTasks = new ObservableCollection<ActivityTask>();
            Parent = "";
            ActivityName = "";
        }

        public string Parent {get; set;}
        public string ActivityName { get; set; }
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
            set { _IsSelected = value; OnChanged("IsSelected"); }
        }

        public ObservableCollection<ActivityTask> lstActivityTasks { get; set; }
    }


    public class ActivityTask : INotifyPropertyChanged
    {

        public ActivityTask()
        {
            this.lstsubTasks = new ObservableCollection<ActivityTask>() { };
            ActivityId = 0;
            Parent = "";
            TaskName = "";
            Description = "";

        }

       
        public int ActivityId {get;set; }
        public string Parent { get; set; }
      
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
                OnChanged("IsSelected");
            }
        }

      

        public ObservableCollection<ActivityTask> lstsubTasks { get; set; }
    }

  


}
