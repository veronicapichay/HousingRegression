using Microsoft.Extensions.Configuration;
using Microsoft.ML;
using Microsoft.WindowsAzure.Storage;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace HousingRegression
{
    class Program
    {
        //Global
        private static string _sqlConnectionString;                       //holds db connection string
        private static readonly string fileName = "kubo.zip";                                                             //name of the model private static readonly string fileName = "housing.zip";        

        static async Task Main(string[] args)
        {

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json");  //package needed

            var configuration = builder.Build();

            _sqlConnectionString = configuration["connectionString"];  //keyvalue pair 

            var items = File.ReadAllLines("./HouseInfo.csv")           //reading the file from csv //LINQ
                .Skip(1)                                            //Skip the header row 
                .Select(line => line.Split(','))                   //for each line that I get, I'm adding a comma //each item that got split create an object for data
                .Select(i => new HouseData
                {                                                //mapping array of strings using index accessor
                    Price = Parse(i[0]),
                    Area = Parse(i[1]),
                    Bedrooms = Parse(i[2]),
                    Bathrooms = Parse(i[3]),
                    Stories = Parse(i[4]),
                    Mainroad = i[5],
                    Guestroom = i[6],
                    Basement = i[7],
                    Hotwaterheating = i[8],
                    Airconditioning = i[9],
                    Parking = Parse(i[10]),
                    Prefarea = i[11],
                    Furnishingstatus = i[12]
                });

            //conecting to azure db
            using (var connection = new SqlConnection(_sqlConnectionString)) //passing configuration _sql
            {

                connection.Open();
                //query to insert to the db, create a table in azure db and add a primary key
                var insertCommand = @"INSERT INTO dbo.HomeData VALUES           
                                    (@price, @area, @bedrooms, @bathrooms, @stories, @mainroad, @guestroom, @basement, @hotwaterheating,
                                     @airconditioning, @parking, @prefarea, @furnishingstatus);";


                var selectCommand = "SELECT COUNT (*) from dbo.HomeData";   //number of rows that will return
                var selectSqlCommand = new SqlCommand(selectCommand, connection);
                var results = (int)selectSqlCommand.ExecuteScalar(); //fetching data from the first column of the first row


                //deletes empty rows before inserting new rows 
                if (results > 0)
                {
                    var deleteCommand = "DELETE FROM dbo.HomeData";
                    var deleteSqlCommand = new SqlCommand(deleteCommand, connection);
                    deleteSqlCommand.ExecuteNonQuery();    //deletes the row based on the number of rows affected 
                }
                //loops through the data from csv and writes data to db
                foreach (var item in items)
                {
                    var command = new SqlCommand(insertCommand, connection);

                    command.Parameters.AddWithValue("@price", item.Price);
                    command.Parameters.AddWithValue("@area", item.Area);
                    command.Parameters.AddWithValue("@bedrooms", item.Bedrooms);
                    command.Parameters.AddWithValue("@bathrooms", item.Bathrooms);
                    command.Parameters.AddWithValue("@stories", item.Stories);
                    command.Parameters.AddWithValue("@mainroad", item.Mainroad);
                    command.Parameters.AddWithValue("@guestroom", item.Guestroom);
                    command.Parameters.AddWithValue("@basement", item.Basement);
                    command.Parameters.AddWithValue("@hotwaterheating", item.Hotwaterheating);
                    command.Parameters.AddWithValue("@airconditioning", item.Airconditioning);
                    command.Parameters.AddWithValue("@parking", item.Parking);
                    command.Parameters.AddWithValue("@prefarea", item.Prefarea);
                    command.Parameters.AddWithValue("@furnishingstatus", item.Furnishingstatus);
                    command.ExecuteNonQuery();

                }

                //reading from queried the data that will be used for the model 

                var data = new List<HouseData>(); //all the data that we pulled from the db

                using (var conn = new SqlConnection(_sqlConnectionString)) //new connection
                {
                    conn.Open();

                    var selectCmd = "SELECT * FROM dbo.KuboData";
                    var sqlCommand = new SqlCommand(selectCmd, conn); //created a new command to grab data from db 
                    var reader = sqlCommand.ExecuteReader(); //reads all the rows from the command

                    while (reader.Read())
                    {
                        data.Add(new HouseData                                           //adds the row to the newly created list 
                        {
                            Price = Parse(reader.GetValue(0).ToString()),               //another mapping from what we get from the db to the properties on the object 
                            Area = Parse(reader.GetValue(1).ToString()),
                            Bedrooms = Parse(reader.GetValue(2).ToString()),
                            Bathrooms = Parse(reader.GetValue(3).ToString()),
                            Stories = Parse(reader.GetValue(4).ToString()),
                            Mainroad = reader.GetValue(5).ToString(),
                            Guestroom = reader.GetValue(6).ToString(),
                            Basement = reader.GetValue(7).ToString(),
                            Hotwaterheating = reader.GetValue(8).ToString(),
                            Airconditioning = reader.GetValue(9).ToString(),
                            Parking = Parse(reader.GetValue(10).ToString()),
                            Prefarea = reader.GetValue(11).ToString(),
                            Furnishingstatus = reader.GetValue(12).ToString(),
                        });

                    }
                }
                var context = new MLContext();
                var mlData = context.Data.LoadFromEnumerable(data);                          //passing the list of data 
                var testTrainSplit = context.Regression.TrainTestSplit(mlData);             //2 object that has train set and test set. This will be different in higher versions of ml.net

                //create pipeline
                var pipeline = context.Transforms.Categorical.OneHotEncoding("TypeOneHot", "Price")            //onehotencoding is converting each categorical value into a new categorical column and assign a binary value of 1 or 0
                             .Append(context.Transforms.Concatenate("Features", "Area", "Bedrooms", "Bathrooms", "Stories", "Mainroad", "Guestroom", "Basement", "Hotwaterheating", "Airconditioning", "Parking", "Furnishingstatus"))
                             .Append(context.Regression.Trainers.FastTree(labelColumnName: "Prefarea")); //Fastree is the trainer that being used. Training Prefarea column

                //setting up pipeline
                var model = pipeline.Fit(testTrainSplit.TrainSet);                                                           //using fit method passing dataset

                //saving to blob storage //create a container inside your blob storage
                var storageAccount = CloudStorageAccount.Parse(configuration["blobConnectionString"]);                    //connect to blob storage account using Microsoft.WindowsAzure.Storage;
                var client = storageAccount.CreateCloudBlobClient();
                var container = client.GetContainerReference("models");                                                   //referencing to the new container

                var blob = container.GetBlockBlobReference(fileName);                                                   //model file

                using (var stream = File.Create(fileName))
                {
                    context.Model.Save(model, stream);  //saves model on local disk
                }

                await blob.UploadFromFileAsync(fileName);           
            }
        }
        private static int Parse(string value)
        {
            return int.TryParse(value, out int prasedValue) ? prasedValue : default;
        }
    }
}





















