using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Geodelta.Photogrammetry;
using Geodelta.Photogrammetry.IO.Exif;
using Geodelta.LinearAlgebra.Rotation;
using Geodelta.Units;
using Geodelta.Units.Length;
using Geodelta.Units.Angle;
using Npgsql;

namespace Helpers
{
    public static class DatabaseHelper
    {
        // Connecting to PostgreSQL database
        private static string ConnectionString => $"Host=localhost;Username=postgres;Password={Environment.GetEnvironmentVariable("DB_PASSWORD")};Database=aerial_bag_db";

        // Get camera specifications for Back, Fwd, Left and Right
        public static async Task<List<CameraSpecs>> GetCameraSpecsAsync()
        {
            // Initialize custom cameraSpecList that reads in all columns from camera_specs table
            List<CameraSpecs> cameraSpecsList = new();
            Micrometer micrometer = new();
            Millimeter millimeter = new();

            // Connecting to database and querying all rows from camera_specs
            await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
            await using var cmd = dataSource.CreateCommand("SELECT * FROM camera_specs");
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var cameraId = reader.GetString(0);
                var pixelSize = new Quantity<Micrometer>(reader.GetDouble(1), micrometer);
                var widthPx = reader.GetInt32(2);
                var heightPx = reader.GetInt32(3);
                var focalLength = new Quantity<Millimeter>(reader.GetDouble(4), millimeter);
                var ppx = new Quantity<Millimeter>(reader.GetDouble(5), millimeter);
                var ppy = new Quantity<Millimeter>(reader.GetDouble(6), millimeter);

                cameraSpecsList.Add(new CameraSpecs(cameraId, pixelSize, widthPx, heightPx, focalLength, ppx, ppy));
            }

            return cameraSpecsList;
        }

        // Constructing ImagePlane objects
        public static async Task<List<ImagePlane>> GetImagePlanesAsync(List<CameraSpecs> cameraSpecsList)
        {
            List<ImagePlane> imagePlanes = new();
            Meter meter = new();
            Degree degree = new();
            Millimeter millimeter = new();

            // Connecting to database and querying all rows from image_specs
            await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
            await using var cmd = dataSource.CreateCommand("SELECT * FROM image_specs");
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var imageId = reader.GetString(0);
                var X = new Quantity<Meter>(reader.GetDouble(1), meter);
                var Y = new Quantity<Meter>(reader.GetDouble(2), meter);
                var Z = new Quantity<Meter>(reader.GetDouble(3), meter);
                var Omega = new Quantity<Degree>(reader.GetDouble(4), degree);
                var Phi = new Quantity<Degree>(reader.GetDouble(5), degree);
                var Kappa = new Quantity<Degree>(reader.GetDouble(6), degree);
                var cameraId = reader.GetString(7);

                // Creating ExteriorOrientation
                var opticalCenter = new TerrainPoint<Meter>(X, Y, Z);
                IRotation3D attitude = new RotationOPK(Omega, Phi, Kappa);
                var exteriorOrientation = new ExteriorOrientation { OpticalCenter = opticalCenter, Attitude = attitude };

                // Retrieving necessary information from camera_specs list
                var matchingCameraSpecs = cameraSpecsList.Find(c => c.CameraId == cameraId);

                // Creating InteriorOrientation
                var interiorOrientation = new InteriorOrientation();

                if (matchingCameraSpecs != null)
                {
                    var principalPoint = new PrincipalPoint(matchingCameraSpecs.PrincipalPointX, matchingCameraSpecs.PrincipalPointY);
                    var sensorWidth = new Quantity<Millimeter>(matchingCameraSpecs.PixelSize.Value * matchingCameraSpecs.WidthPx / 1000, millimeter);
                    var sensorHeight = new Quantity<Millimeter>(matchingCameraSpecs.PixelSize.Value * matchingCameraSpecs.HeightPx / 1000, millimeter);

                    interiorOrientation.FocalLength = matchingCameraSpecs.FocalLength;
                    interiorOrientation.PrincipalPoint = principalPoint;
                    interiorOrientation.Sensor = new ImageSensor(sensorWidth, sensorHeight, matchingCameraSpecs.PixelSize);
                }

                // Adding imagePlane to list
                imagePlanes.Add(new ImagePlane(exteriorOrientation, interiorOrientation, imageId));
            }

            return imagePlanes;
        }

        // Getting BAGids in footprint of image of smalltestarea
        public static async Task<Dictionary<string, List<string>>> GetBAGinImage()
        {
            // Initialize custom cameraSpecList that reads in all columns from camera_specs table
            Dictionary<string, List<string>> imageWithBagIDs = new();

            // Getting Image ID and BagIDs present in image from flightplans table
            await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
            await using var cmd = dataSource.CreateCommand("SELECT name, bag_ids_smalltestarea FROM flightplans WHERE bag_ids_smalltestarea IS NOT NULL;");
            // change to command in next line if you want to query the bag_ids_testarea
            //await using var cmd = dataSource.CreateCommand("SELECT name, bag_ids_testarea FROM flightplans WHERE bag_ids_smalltestarea IS NOT NULL;");

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                string imageId = reader.GetString(0);
                string[] bagIdsArray = reader.GetFieldValue<string[]>(1);

                // Convert array to a list and store in dictionary
                imageWithBagIDs[imageId] = bagIdsArray.ToList();
            }

            return imageWthBagIDs
        }
    }
}
