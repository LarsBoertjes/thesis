// See https://aka.ms/new-console-template for more information
using System;
using System.Collections.Generic;
using Geodelta.LinearAlgebra.Rotation;
using Geodelta.Photogrammetry;
using Geodelta.Photogrammetry.IO.Exif;
using Geodelta.Units;
using Geodelta.Units.Angle;
using Geodelta.Units.Length;
using Microsoft.Data.SqlClient;
using Npgsql;
using Helpers;

namespace TerrainPointExample
{
    class Program
    {
        static async Task Main()
        {
            // Connecting to aerial_bag_db database (PostgreSQL)
            var connectionString = $"Host=localhost;Username=postgres;Password={Environment.GetEnvironmentVariable("DB_PASSWORD")};Database=aerial_bag_db";
            await using var dataSource = NpgsqlDataSource.Create(connectionString);

            // Defining list for reading camera_specs data into memory
            List<CameraSpecs> cameraSpecsList = new();

            // Initializing length units
            Micrometer micrometer = new Micrometer();
            Millimeter millimeter = new Millimeter();
            Meter meter = new Meter();
            Degree degree = new Degree();

            // Retrieve camera_specs data
            await using (var cmd = dataSource.CreateCommand("SELECT * FROM camera_specs"))
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    // Read in all the columns
                    var cameraId = reader.GetString(0);
                    var pixelSize = new Quantity<Micrometer>(reader.GetDouble(1) * 1000, micrometer);
                    var widthPx = reader.GetInt32(2);
                    var heightPx = reader.GetInt32(3);
                    var focalLength = new Quantity<Millimeter>(reader.GetDouble(4), millimeter);
                    var ppx = new Quantity<Millimeter>(reader.GetDouble(5), millimeter);
                    var ppy = new Quantity<Millimeter>(reader.GetDouble(6), millimeter);

                    // Create CameraSpecs object
                    cameraSpecsList.Add(new CameraSpecs(cameraId, pixelSize, widthPx, heightPx, focalLength, ppx, ppy));

                    // Create PrincipalPoint object
                    // PrincipalPoint principalPoint = new PrincipalPoint(ppx, ppy)
                }
            }


            Console.WriteLine(cameraSpecsList.Count);

            // Defining list for ImagePlanes
            List<ImagePlane> imagePlanes = new List<ImagePlane>();

            await using (var cmd = dataSource.CreateCommand("SELECT * FROM image_specs"))
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    // Read in columns
                    var image_id = reader.GetString(0);
                    var X = new Quantity<Meter>(reader.GetDouble(1), meter);
                    var Y = new Quantity<Meter>(reader.GetDouble(2), meter);
                    var Z = new Quantity<Meter>(reader.GetDouble(3), meter);
                    var Omega = new Quantity<Degree>(reader.GetDouble(4), degree);
                    var Phi = new Quantity<Degree>(reader.GetDouble(5), degree);
                    var Kappa = new Quantity<Degree>(reader.GetDouble(6), degree);
                    var camera_id = reader.GetString(7);

                    // Create Exterior Orientation
                    // Construct Optical Center
                    var opticalCenter = new TerrainPoint<Meter>(X, Y, Z);
                    // Convert OPK angles to a rotation matrix
                    IRotation3D attitude = new RotationOPK(Omega, Phi, Kappa);
                    // Create ExteriorOrientation
                    var exteriorOrientation = new ExteriorOrientation<Meter>
                    {
                        OpticalCenter = opticalCenter,
                        Attitude = attitude
                    };

                    // Create Interior Orientation


                }
            }



        }
    }
}

