using System.IO;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;


namespace HousingRegression
{
    class Program
    {
        //Global
        private static string _sqlConnectionString;                       //holds db connection string
        private static readonly string fileName = "housing.zip";                   //model path
            


        static void Main(string[] args)
        {

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())  
                .AddJsonFile("config.json");  //package needed

            var configuration = builder.Build();

            _sqlConnectionString = configuration["connectionString"];  //keyvalue pair from azure

            var items = File.ReadAllLines("./Housing.csv")           //reading the file from csv
                .Skip(1)
                .Select(line => line.Split(','))                //for each line that I get, I'm adding a comma //each item that got split create an object for data
                .Select(i => new HouseData
                {

                    Price = i[0],
                    Area = i[1]




                });
        }
        //parse
        private static float Parse(string value)
        {







        }









    }
}
