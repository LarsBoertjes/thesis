// See https://aka.ms/new-console-template for more information
using BAG;
using NetTopologySuite.Geometries; // For handling geometric data
using System;
using System.Collections.Generic;
using Geodelta.Photogrammetry;
using Geodelta.Units;
using Geodelta.Units.Length;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using NetTopologySuite.Operation.Valid;
using System.Reflection;
using Npgsql;

namespace TerrainPointExample
{
    class Program
    {
        static async Task Main()
        {
            var connectionString = $"Host=localhost;Username=postgres;Password={Environment.GetEnvironmentVariable("DB_PASSWORD")};Database=aerial_bag_db";
            await using var dataSource = NpgsqlDataSource.Create(connectionString);

            // Retrieve all rows from camera_specs test
            await using (var cmd = dataSource.CreateCommand("SELECT * FROM camera_specs"))
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var dataTypeName = reader.GetFieldType(i).Name; 

                        if (dataTypeName == "Double" || dataTypeName == "DoublePrecision")
                        {
                            Console.WriteLine(reader.GetDouble(i));
                        }
                        else 
                        {
                            Console.WriteLine(reader.GetValue(i));
                        }
                    }
                }
            }



        }
    }
}

