
using System;
using System.Data.Entity;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows.Controls;
using System.IO;

namespace CAOGAttendeeManager
{

    public class AttendeeManagerDBModel : DbContext
    {
        // Your context has been configured to use a 'AttendeeManagerDBModel' connection string from your application's 
        // configuration file (App.config or Web.config). By default, this connection string targets the 
        // 'CAOGAttendeeManager.AttendeeManagerDBModel' database on your LocalDb instance. 
        // 
        // If you wish to target a different database and/or database provider, modify the 'AttendeeManagerDBModel' 
        // connection string in the application configuration file.

        public AttendeeManagerDBModel() : base("name=AccessConnection")
        {

        }
        public AttendeeManagerDBModel(string ConStr) : base(ConStr)
        {
           
        }

        // Add a DbSet for each entity type that you want to include in your model. For more information 
        // on configuring and using a Code First model, see http://go.microsoft.com/fwlink/?LinkId=390109.


        public virtual DbSet<Attendee> Attendees { get; set; }
        public virtual DbSet<Attendance_Info> Attendance_Info { get; set; }
        public virtual DbSet<Activity> Activities { get; set; }
    }



    public class Attendee : INotifyPropertyChanged
    {

        public Attendee()
        {
            AttendanceList = new ObservableCollection<Attendance_Info>() { };
            ActivityList = new ObservableCollection<Activity>() { };

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


        public virtual ObservableCollection<Attendance_Info> AttendanceList { get; set; }
        public virtual ObservableCollection<Activity> ActivityList { get; set; }

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


    public class Activity
    {

        public Activity()
        {
            ActivityText = "";

            ActivityId = 0;
            AttendeeId = 0;
            Date = null;
        }

        public int ActivityId { get; set; }
        public int AttendeeId { get; set; }

        public virtual Attendee Attendee { get; set; }

        private string _activityText = "";
        public string ActivityText 
        {
            get
            {
                return _activityText;
            }

            set
            {
                if (_activityText != value)
                {
                    _activityText = value;
                    

                    NotifyPropertyChanged("ActivityText");
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


        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }

    public enum ArrayFormat
    {
        STX = 0x02,
        ETX = 0x03
    }


    public class TreeNode : TreeViewItem
    {
        public MemoryStream rtbDescriptionMStream { get; set; }

        public int Level { get; set; }

        public TreeNode()
        {
            rtbDescriptionMStream = new MemoryStream();
            Level = 0;
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
            ActivityText = "";
            Date = null;
            Church_Last_Attended = "";
            Activity_Last_Attended = "";
            ChurchStatus = "";
            // DynamicProperties = new Dictionary<string, object>() { };

            AttendanceList = new ObservableCollection<Attendance_Info>() { };
            ActivityList = new ObservableCollection<Activity>() { };


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

        private string _activityText = "";
        public string ActivityText
        {
            get
            {
                return _activityText;
            }
            set
            {
                if (_activityText != value)
                    _activityText = value;

                NotifyPropertyChanged("ActivityText");
            }

        }
       

        public virtual ObservableCollection<Activity> ActivityList { get; set; }

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

        //string[] m_aryfunction_steps = { };

        //string m_functionSteps = "";
        //public string FunctionSteps
        //{
        //    get
        //    {
        //        return m_functionSteps;
        //    }
        //    set
        //    {
        //        m_functionSteps = value;
        //    }
        //}









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

}
