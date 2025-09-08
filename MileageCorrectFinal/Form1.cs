using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MileageCorrectFinal
{
    public partial class Form1 : Form
    {
        string conStr = ConfigurationManager.ConnectionStrings["GPSDBConnectionString"].ConnectionString;
        SqlConnection con;
        SqlCommand cmd;
        SqlDataReader rd;

        SqlConnection con1;
        SqlCommand cmd1;
        SqlDataReader rd1;

        SqlConnection con2;
        SqlCommand cmd2;
        SqlDataReader rd2;

        SqlConnection con3;
        SqlCommand cmd3;
        SqlDataReader rd3;

        public Form1()
        {
            InitializeComponent();
        }

        private void btnLastNid_Click(object sender, EventArgs e)
        {
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void FillCombo()
        {
        }

        private void cmbTblName_SelectedIndexChanged(object sender, EventArgs e)
        {
         
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        private void btnRepair_Click(object sender, EventArgs e)
        {
            con = new SqlConnection(conStr);
            cmd = new SqlCommand();
            cmd.Connection = con;
            con.Open();
            cmd.CommandText = "select TableName, [nTimeFrom] from [All_Track] where [Status] = 0 and nTimeFrom > 0";
            rd = cmd.ExecuteReader();
            int nTimeFrom = 0;
            string tableName;
            while (rd.Read())
            {
                tableName = rd.GetString(0);
                nTimeFrom = rd.GetInt32(1);
                DateTime dtFrom = UnixTimeStampToDateTime(Convert.ToDouble(nTimeFrom));

                //zero lat lon
                con1 = new SqlConnection(conStr);
                cmd1 = new SqlCommand();
                cmd1.Connection = con1;
                con1.Open();
                cmd1.CommandText = "delete from " + tableName.Trim() + " where dbLat <1 or dbLon <1";
                cmd1.ExecuteNonQuery();
                con1.Close();

                con2 = new SqlConnection(conStr);
                cmd2 = new SqlCommand();
                cmd2.Connection = con2;
                con2.Open();
                cmd2.CommandText = "select nTime, dbLon, dbLat, nMileage from " + tableName + " WHERE nTime BETWEEN DATEDIFF (SECOND, '1970-01-01 06:00:00', '" + dtFrom + "') AND DATEDIFF(SECOND, '1970-01-01 06:00:00', GETDATE()) ORDER BY nTime";
                rd2 = cmd2.ExecuteReader();

                int i = 0;
                decimal lon = 0;
                decimal lat = 0;
                int m = 0;
                int calMile;
                int nTime = 0;

                while (rd2.Read())
                {
                    i++;
                    if (i == 1)
                    {
                        lon = rd2.GetDecimal(1);
                        lat = rd2.GetDecimal(2);
                        m = rd2.GetInt32(3);
                    }
                    else if (i > 1)
                    {
                        calMile = Convert.ToInt32(DirectDistance(Convert.ToDouble(lat), Convert.ToDouble(lon), Convert.ToDouble(rd2.GetDecimal(2)), Convert.ToDouble(rd2.GetDecimal(1))));
                        if (calMile > 80000)
                        {
                            calMile = 100;
                        }
                        calMile = calMile + m;
                        nTime = rd2.GetInt32(0);

                        con3 = new SqlConnection(conStr);
                        cmd3 = new SqlCommand();
                        cmd3.Connection = con3;
                        con3.Open();
                        cmd3.CommandText = "update " + tableName + " set nMileage = " + calMile + " where nTime = " + nTime + "";
                        cmd3.ExecuteNonQuery();
                        con3.Close();

                        lon = rd2.GetDecimal(1);
                        lat = rd2.GetDecimal(2);
                        m = calMile;
                    }
                }

                rd2.Close();
                con2.Close();

                //update track_last table
                string strTeid = tableName;
                if (strTeid.Contains("Track__"))
                {
                    strTeid = strTeid.Replace("Track__", "-");
                }
                else
                {
                    strTeid = strTeid.Replace("Track_", "");
                }

                con2 = new SqlConnection(conStr);
                cmd2 = new SqlCommand();
                cmd2.Connection = con2;
                con2.Open();
                cmd2.CommandText = "update Table_TrackLast set nMileage = " + m + " where strTEID = '" + strTeid + "'";
                cmd2.ExecuteNonQuery();
                con2.Close();

                con2 = new SqlConnection(conStr);
                cmd2 = new SqlCommand();
                cmd2.Connection = con2;
                con2.Open();
                cmd2.CommandText = "update [All_Track] set [Status] = 1, [nTimeTo] = " + nTime + " where TableName = '" + tableName + "'";
                cmd2.ExecuteNonQuery();
                con2.Close();

                //lblStatus.Text = "SUCCESS! action completed.";
                
                //FillCombo();
            }
            rd.Close();
            con.Close();
            MessageBox.Show("SUCCESS! action completed.");
        }

        double DirectDistance(double lat1, double lng1, double lat2, double lng2)
        {
            double earthRadius = 3959.00;
            double dLat = ToRadians(lat2 - lat1);
            double dLng = ToRadians(lng2 - lng1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double dist = earthRadius * c;
            double meterConversion = 1609.334;
            return Math.Ceiling(dist * meterConversion);
        }

        double ToRadians(double degrees)
        {
            double radians = degrees * 3.14159265 / 180;
            return radians;
        }
    }
}
