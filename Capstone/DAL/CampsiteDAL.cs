﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Capstone.Models;

namespace Capstone.DAL
{
    public class CampsiteDAL
    {
        public List<Campsite> GetCampsites(Campground cg, string connectionString)
        {
            List<Campsite> campsites = new List<Campsite>();
            string campName = cg.Name;
            int campNameId = 0;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlCommand cg_id = new SqlCommand("SELECT campground_id FROM campground WHERE campground.name = @name", conn);
                    cg_id.Parameters.AddWithValue("@name", campName);

                    SqlDataReader reader = cg_id.ExecuteReader();
                    while (reader.Read())
                    {
                        campNameId = Convert.ToInt32(reader["campground_id"]);
                    }
                    reader.Close();
                
                    SqlCommand cmd = new SqlCommand("SELECT * FROM site JOIN campground ON site.campground_id = campground.campground_id WHERE site.campground_id = @cg_campground_id", conn);
                    cmd.Parameters.AddWithValue("@cg_campground_id", campNameId);

                    SqlDataReader reader2 = cmd.ExecuteReader();
                    while(reader2.Read())
                    {
                        Campsite cs = new Campsite();

                        cs.SiteNumber = Convert.ToInt32(reader2["site_number"]);
                        cs.MaxOccupancy = Convert.ToInt32(reader2["max_occupancy"]);
                        cs.Accessible = Convert.ToBoolean(reader2["accessible"]);
                        cs.Utilities = Convert.ToBoolean(reader2["utilities"]);
                        cs.MaxRvLength = Convert.ToInt32(reader2["max_rv_length"]);

                        campsites.Add(cs);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return campsites;
        }

        public List<Campsite> GetCampsitesByAvailability(string connectionString, Campground cg,
                                                         DateTime desiredArrival, DateTime desiredDeparture)
        {

            List<Campsite> campsites = new List<Campsite>();
            string campName = cg.Name;
            int campNameId = 0;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlCommand cg_id = new SqlCommand("SELECT campground_id FROM campground WHERE campground.name = @name", conn);
                    cg_id.Parameters.AddWithValue("@name", campName);

                    SqlDataReader reader = cg_id.ExecuteReader();
                    while (reader.Read())
                    {
                        campNameId = Convert.ToInt32(reader["campground_id"]);
                    }
                    //reader.Close();                    
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine(ex.Message);
            }
            try
            {
                using (SqlConnection conn2 = new SqlConnection(connectionString))
                {
                    conn2.Open();
                    SqlCommand cmd = new SqlCommand(@"SELECT site.* FROM site
                                                    LEFT JOIN reservation ON site.site_id = reservation.site_id 
                                                    JOIN campground ON campground.campground_id = site.campground_id 
                                                    WHERE ((@todate <= reservation.from_date) OR (@fromdate >= reservation.to_date))  
                                                    AND campground.name = @campName", conn2);
                    cmd.Parameters.AddWithValue("@campName", campName);
                    cmd.Parameters.AddWithValue("@fromdate", desiredArrival);
                    cmd.Parameters.AddWithValue("@todate", desiredDeparture);

                    SqlDataReader reader2 = cmd.ExecuteReader();
                    while (reader2.Read())
                    {
                        Campsite cs = new Campsite();

                        cs.SiteID = Convert.ToInt32(reader2["site_id"]);
                        cs.MaxOccupancy = Convert.ToInt32(reader2["max_occupancy"]);
                        cs.Accessible = Convert.ToBoolean(reader2["accessible"]);
                        cs.Utilities = Convert.ToBoolean(reader2["utilities"]);
                        cs.MaxRvLength = Convert.ToInt32(reader2["max_rv_length"]);

                        campsites.Add(cs);
                    }
                }
            }
            catch(SqlException ex)
            {
                Console.WriteLine(ex.Message);
            }
            return campsites;
        }

        public decimal CalculateCostOfReservation(Campsite site, DateTime arrival, DateTime departure, string connectionString)
        {

            //return the rate based on the site chosen (the math from the dates will be done back in the cli? <--it would be better to do it here
            decimal dailyFee = 0.0M;
            decimal finalFee = 0.0M;            

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(@"SELECT campground.daily_fee  
                                                      FROM campground
                                                      JOIN site ON campground.campground_id = site.campground_id
                                                      JOIN reservation ON site.site_id = reservation.site_id
                                                      WHERE site.site_id = @siteid", conn);
                    cmd.Parameters.AddWithValue("@siteid", site.SiteID);

                    SqlDataReader reader = cmd.ExecuteReader();

                    TimeSpan difference = departure - arrival;

                    while(reader.Read())
                    {
                        dailyFee = Convert.ToDecimal(reader["daily_fee"]);
                        finalFee = (decimal)difference.TotalDays * dailyFee;
                    }
                }
            }
            catch(SqlException ex)
            {
                Console.WriteLine(ex.Message);
            }

            return finalFee;
        }

        public void CreateReservation(int siteID, DateTime arrival, DateTime departure, string name, string connectionString)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(@"INSERT INTO reservation(site_id, name, from_date, to_date)
                                                      VALUES(@siteid, @name, @arrival, @departure)", conn);
                    cmd.Parameters.AddWithValue("@siteid", siteID);
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@arrival", arrival);
                    cmd.Parameters.AddWithValue("@departure", departure);

                    cmd.ExecuteNonQuery();
                }
            }
            catch(SqlException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public int GetReservationID(int siteID, string connectionString)
        {
            int reservationID = 0;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(@"SELECT reservation_id 
                                                      FROM reservation
                                                      JOIN site ON site.site_id = reservation.site_id
                                                      WHERE site.site_id = @siteid", conn);
                    cmd.Parameters.AddWithValue("@siteid", siteID);

                    SqlDataReader reader = cmd.ExecuteReader();
                    
                    while (reader.Read())
                    {
                        reservationID = Convert.ToInt32(reader["reservation_id"]);
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine(ex.Message);
            }

            return reservationID;
        }

        public Reservation FindReservationByID(int reservationID, string connectionString)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(@"SELECT *
                                                      FROM reservation
                                                      WHERE reservation.reservation_id = @id"
                                                      , conn);
                    cmd.Parameters.AddWithValue("@id", reservationID);

                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        Reservation r = new Reservation();

                        r.Name = Convert.ToString(reader["name"]);
                        r.SiteID = Convert.ToInt32(reader["site_id"]);
                        r.ReservationID = Convert.ToInt32(reader["reservation_id"]);
                        r.FromDate = Convert.ToDateTime(reader["from_date"]);
                        r.ToDate = Convert.ToDateTime(reader["to_date"]);
                        r.FoundedDate = Convert.ToDateTime(reader["create_date"]);

                        return r;
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;
        }
    }
}
