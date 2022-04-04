using System.IO;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Data.SqlClient;

namespace HousingRegression
{
    class Program
    {
        //Global
        private static string _sqlConnectionString;                       //holds db connection string
       //name of the model private static readonly string fileName = "housing.zip";        
            


        static void Main(string[] args)
        {

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())  
                .AddJsonFile("config.json");  //package needed

            var configuration = builder.Build();

            _sqlConnectionString = configuration["connectionString"];  //keyvalue pair 

            var items = File.ReadAllLines("./Housing.csv")           //reading the file from csv //LINQ
                .Skip(1)                                            //Skip the header row 
                .Select(line => line.Split(','))                   //for each line that I get, I'm adding a comma //each item that got split create an object for data
                .Select(i => new HouseData                        
                {                                                //mapping array of strings using index accessor
                    Price = i[0],
                    Area =  i[1],
                    Bedrooms = i[2],
                    Bathrooms = i[3],
                    Stories = i[4],
                    Mainroad = i[5],
                    Guestroom = i[6],
                    Basement = i[7],
                    Hotwaterheating = i[8],
                    Airconditioning = i[9],
                    Parking = i[10],
                    Prefarea = i[11],
                    Furnishingstatus = i[12]
                });

            //conecting to azure db
            using (var connection = new SqlConnection(_sqlConnectionString)) //passing configuration _sql
            {


                connection.Open();

                var insertCommand = @"INSERT INTO bahaydata.dbo.BahayData VALUES
                                    (@price, @area, @bedrooms, @bathrooms, @stories, @mainroad, @guestroom, @basement, @hotwaterheating,
                                     @airconditioning, @parking, @prefarea, @furnishingstatus);";

                
                var selectCommand = "SELECT COUNT (*) from dunsamalapit.dbo.BahayData";  //server and db name //number of rows that will return
                var selectSqlCommand = new SqlCommand(selectCommand, connection);
                var results = (int)selectSqlCommand.ExecuteScalar(); //Executes the query, and returns the first column of the first row in the result set returned by the query

                //deletes empty rows before inserting new rows 
                if (results > 0)
                {
                    var deleteCommand = "DELETE FROM dunsamalapit.dbo.BahayData";
                    var deleteSqlCommand = new SqlCommand(deleteCommand, connection);
                    deleteSqlCommand.ExecuteNonQuery();
                }
                //loops throught the data from csv
                foreach (var item in items)
                {
                    var command = new SqlCommand(insertCommand, connection);

                    command.Parameters.AddWithValue("@price",item.Price);
                    command.Parameters.AddWithValue("@area", item.Area);
                    command.Parameters.AddWithValue("@bedrooms",item.Bedrooms);
                    command.Parameters.AddWithValue("@bathrooms",item.Bathrooms);
                    command.Parameters.AddWithValue("@stories",item.Stories);
                    command.Parameters.AddWithValue("@mainroad",item.Mainroad);
                    command.Parameters.AddWithValue("@guestroom",item.Guestroom);
                    command.Parameters.AddWithValue("@basement",item.Basement);
                    command.Parameters.AddWithValue("@hotwaterheating",item.Hotwaterheating);
                    command.Parameters.AddWithValue("@airconditioning",item.Airconditioning);
                    command.Parameters.AddWithValue("@parking",item.Parking);
                    command.Parameters.AddWithValue("@prefarea",item.Prefarea);
                    command.Parameters.AddWithValue("@furnishingstatus",item.Furnishingstatus);
                    command.ExecuteNonQuery();

                }




            }
        }
        private static float Parse(string value)
        {
            return float.TryParse(value, out float prasedValue) ? prasedValue : default;
        }
    }
}
