namespace CAOGAttendeeManager
{
    using System;
    using System.ComponentModel;
    using System.Collections.ObjectModel;
    using System.Data.Entity;

    // [DbConfigurationType(typeof(MyDbConfiguration))]

    
    public class ModelDb : DbContext
    {
        // Your context has been configured to use a 'ModelDb' connection string from your application's 
        // configuration file (App.config or Web.config). By default, this connection string targets the 
        // 'WpfApplication2.ModelDb' database on your LocalDb instance. 
        // 
        // If you wish to target a different database and/or database provider, modify the 'ModelDb' 
        // connection string in the application configuration file.

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    optionsBuilder.UseSqlServer(
        //        @"Server=(Server=tcp:caogserver.database.windows.net,1433;Initial Catalog=CAOGdb_2018_09_14_Prod;Persist Security Info=False;User ID=sqladmin;Password=RFtgYH56&*;MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30");
        //}
        public ModelDb() : base("AccessConnection")
        {
             
        }
        public ModelDb(string constr) : base(constr)
        {

        }
        // Add a DbSet for each entity type that you want to include in your model. For more information 
        // on configuring and using a Code First model, see http://go.microsoft.com/fwlink/?LinkId=390109.

        
        public virtual DbSet<Attendee> Attendees { get; set; }
        public virtual DbSet<Attendance_Info> Attendance_Info { get; set; }
        public virtual DbSet<ActivityPair> Activities { get; set; }
    }
   
    public class Attendee : INotifyPropertyChanged
    {

        public Attendee()
        {
            AttendanceList = new ObservableCollection<Attendance_Info>() { };
            ActivityList = new ObservableCollection<ActivityPair>() { };

        }

        public bool Checked { get; set; }

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

        private string m_strFunctionSteps = "";
        //public string FunctionSteps
        //{
        //    get
        //    {
        //        return m_strFunctionSteps;
        //    }
        //    set
        //    {
        //        m_strFunctionSteps = value;
        //    }
        //}


        public virtual ObservableCollection<Attendance_Info> AttendanceList { get; private set; }
        public virtual ObservableCollection<ActivityPair> ActivityList { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

  
    public class Attendance_Info : IComparable, INotifyPropertyChanged
    {


        public int Attendance_InfoId { get; set; }
        public int AttendeeId { get; set; }





        public string DateString { get; private set; }
        private DateTime? _date;
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
                    NotifyPropertyChanged();

                }
            }
        }



        private string _status = "";
        public string Status
        {
            get
            {
                return _status;
            }
            set
            {
                _status = value;
                NotifyPropertyChanged();
            }
        }
        public virtual Attendee Attendee { get; set; }

        public int CompareTo(object obj)
        {
            Attendance_Info d = (Attendance_Info)obj;


            return (int)_date?.CompareTo(d?._date);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    public class ActivityPair : INotifyPropertyChanged
    {
                    
        public ActivityPair()
        {
            ActivityGroup = "";
            ParentTaskName = "";
            ChildTaskName = "";
            ActivityPairId = 0;
            AttendeeId = 0;
            Date = null;
        }

        public void Clear()
        {
            ActivityGroup = "";
            ParentTaskName = "";
            ChildTaskName = "";
            Date = null;
            AttendeeId = 0;
            ActivityPairId = 0;
        }
        public int ActivityPairId { get; set; }
        public int AttendeeId { get; set; }

        public virtual Attendee Attendee { get; set; }
        public string ActivityGroup { get; set; }

        public static bool operator !=(ActivityPair A, ActivityPair B)
        {

            if (!(A is null) && !(B is null))
            {
                if (!(A.ToString().Equals(B.ToString())))
                {
                    return true;
                }
            }
            else if (!(A is null) && (B is null))
            {
                return true;
            }

            return false;

        }

        public static bool operator ==(ActivityPair A, ActivityPair B)
        {


            if (!(A is null) && !(B is null))
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

        //public int CompareTo(object obj)
        //{
        //    ActivityPair at = (ActivityPair)obj;

        //    DateTime dt = at._date.GetValueOrDefault();

        //    DateTime tdt = _date.GetValueOrDefault();

        //    return tdt.CompareTo(dt);
        //}


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


    //public class DefaultTableRowWrapper : DefaultTableRow
    //{
        
    //    public Dictionary<string, object> DynamicProperties { get; set; }

    //    public DefaultTableRowWrapper() : base ()
    //    {
    //        DynamicProperties = new Dictionary<string, object>();
    //    }
    //    public IEnumerable<string> GetDynamicMemberNames()
    //    {
    //        return DynamicProperties.Keys;

    //    }


    //    public int Count
    //    {
    //        get { return DynamicProperties.Count; }
    //    }

    //    public IDictionary<string, object> MakeDynamicProperties(string function_steps)
    //    {
    //        string concat_string = "";
    //        if (function_steps != "")
    //        {
    //            string[] arycolumn_value = base.FunctionSteps.Split(',');
    //            foreach (string column_value in arycolumn_value)
    //            {
    //                string[] strcolumn_value = column_value.Split('|');
    //                DynamicProperties.Add(strcolumn_value[0].ToLower(), strcolumn_value[1].ToLower());




    //            }
    //        }
    //        else
    //        {
    //            for (int i = 0; i <= base.Columns.Length - 1; i++)
    //            {
    //                if (base.Columns[i] != "0")
    //                    DynamicProperties.Add(base.Columns[i].ToLower(), "");
    //                else
    //                    break;
    //            }
    //        }

    //        return DynamicProperties;
    //    }
    //}



    public class DefaultTableRow : INotifyPropertyChanged
    {


        //public Dictionary<string, object> DynamicProperties { get; set; }

      
        //public void MakeDynamicProperties()
        //{

        //    //Add User added columns as dictionary keys 
        //    for (int i = 0; i <= Columns.Length - 1; i++)
        //    {

        //        if (Columns[i] == "0") { break; }

        //        string key = Columns[i].ToLower();
                
        //        DynamicProperties.Add(key, "");

        //    }

        //    //DynamicProperties["AttendeeId".ToLower()] = AttendeeId;
        //    //DynamicProperties["FirstName".ToLower()] = FirstName;
        //    //DynamicProperties["LastName".ToLower()] = LastName;
        //    //DynamicProperties["Activity".ToLower()] = Activity;
        //    //DynamicProperties["ChurchStatus".ToLower()] = ChurchStatus;


        //    //Add values to keys according to the class fields

        //    if (m_functionSteps != "")
        //    {

                

        //        //find key in dictionary and add the corresponding value from the function steps string to it
        //        string[] arycolumn_value = m_functionSteps.Split(',');
        //        foreach (string column_value in arycolumn_value)
        //        {
        //            string[] strcolumn_value = column_value.Split('|');

        //            string key = strcolumn_value[0].ToLower();
        //            string value = strcolumn_value[1].ToLower();

                   
        //            DynamicProperties[key] = value;
        //        }


        //    }
        //    else
        //    {
        //        //Add User added columns as dictionary keys 
        //        for (int i = 0; i <= m_aryfunction_steps.Length - 1; i++)
        //        {

        //            if (m_AllTableColumns_Array[i] == "0") { break; }

        //            string key = m_AllTableColumns_Array[i].ToLower();

        //            DynamicProperties.Add(key, "");

        //        }

              
        //    }

            
        //}


        ////Array holding the column header of the datagrid
        //string[] m_AllTableColumns_Array = new string[50];

        //public string[] Columns
        //{
        //    get
        //    {
        //        return m_AllTableColumns_Array;
        //    }
        //    set
        //    {
        //        if (m_AllTableColumns_Array != value)
        //        {
        //            for (int i =0; i<= m_AllTableColumns_Array.Length-1;i++)
        //            {
        //                m_AllTableColumns_Array[i] = value[i];
        //            }
                    
        //        }
                    
        //    }
        //}


        public DefaultTableRow()
        {
            AttendeeId = 0;
            FirstName = "";
            LastName = "";
            Activity = "";
            Date = null;
            Church_Last_Attended = "";
            Activity_Last_Attended = "";
            ChurchStatus = "";
           // DynamicProperties = new Dictionary<string, object>() { };

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
                    Church_Last_Attended = _date?.ToString("MM-dd-yyyy");

                    NotifyPropertyChanged("DateString");
                }
            }
        }


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

        string[] m_aryfunction_steps = { };

        string m_functionSteps = "";
        public string FunctionSteps
        {
            get
            {
                return m_functionSteps;
            }
            set
            {
                m_functionSteps = value;
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
            Attended = "";
            IsNewrow = false;
            IsModifiedrow = false;
            
        }
      
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
                 
                }
                
            }

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
        public string DateString { get; set; }

        private string _attended = "";
        public string Attended
        {
            get
            {
                return _attended;
            }
            set
            {
                if (_attended != value)
                {
                    _attended = value;
                    NotifyPropertyChanged("Attended");
                }
            }
        }

        //private string _activity = "";

        
    
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

       
    }
  

    public class ActivityHeader
    {
        public ActivityHeader()
        {
            Name = "";
            Groups = new ObservableCollection<ActivityGroup>();
        }
        public string Name { get; set; }
        public ObservableCollection<ActivityGroup> Groups { get; set; } 
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
