namespace CAOGAttendeeProject
{
    partial class AttendeeChartForm
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.DataPoint dataPoint1 = new System.Windows.Forms.DataVisualization.Charting.DataPoint(0D, 4D);
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series3 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.AttendeeChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            ((System.ComponentModel.ISupportInitialize)(this.AttendeeChart)).BeginInit();
            this.SuspendLayout();
            // 
            // AttendeeChart
            // 
            chartArea1.Name = "ChartArea1";
            this.AttendeeChart.ChartAreas.Add(chartArea1);
            legend1.Name = "Legend1";
            this.AttendeeChart.Legends.Add(legend1);
            this.AttendeeChart.Location = new System.Drawing.Point(130, 187);
            this.AttendeeChart.Name = "AttendeeChart";
            series1.ChartArea = "ChartArea1";
            series1.Legend = "Legend1";
            series1.Name = "Attended";
            series1.Points.Add(dataPoint1);
            series2.ChartArea = "ChartArea1";
            series2.Legend = "Legend1";
            series2.Name = "Follow-Up";
            series3.ChartArea = "ChartArea1";
            series3.Legend = "Legend1";
            series3.Name = "Responded";
            this.AttendeeChart.Series.Add(series1);
            this.AttendeeChart.Series.Add(series2);
            this.AttendeeChart.Series.Add(series3);
            this.AttendeeChart.Size = new System.Drawing.Size(336, 209);
            this.AttendeeChart.TabIndex = 0;
            this.AttendeeChart.Text = "ChartData";
            // 
            // AttendeeChartForm
            // 
            this.Controls.Add(this.AttendeeChart);
            this.Name = "AttendeeChartForm";
            this.Size = new System.Drawing.Size(661, 512);
            ((System.ComponentModel.ISupportInitialize)(this.AttendeeChart)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataVisualization.Charting.Chart AttendeeChart;
    }
}
