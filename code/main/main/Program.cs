using System;
using System.Diagnostics;
using System.IO;

namespace MeshProcessingBackend
{
    class Program
    {
        static void Main(string[] args)
        {
            // Print a simple greeting message to confirm the program is running
            Console.WriteLine("Mesh Processing Started");

            // Start Python script for MVS processing (Multi-View Stereo)
            string pythonScriptPath = @"C:\Users\LarsBroertjes\Documents\GitHub\thesis\code\main\pipeline_test.py";
            string pythonArguments = ""; 
            RunPythonScript(pythonScriptPath, pythonArguments);

            // After Python processing, you can load and manipulate meshes
            //string meshFilePath = @"path_to_generated_mesh.obj";
            //LoadAndProcessMesh(meshFilePath);

            Console.WriteLine("Mesh processing completed!");
        }

        // Method to run a Python script from C#
        public static void RunPythonScript(string scriptPath, string arguments)
        {
            try
            {
                // Create a new process to execute the Python script
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = @"C:\Users\LarsBroertjes\Documents\GitHub\thesis\.venv\Scripts\python.exe"; // Adjust this path
                start.Arguments = $"{scriptPath} {arguments}";
                start.UseShellExecute = false;
                start.RedirectStandardOutput = true;
                start.RedirectStandardError = true;

                // Start the process
                using (Process process = Process.Start(start))
                {
                    // Capture and print the output of the Python script
                    using (StreamReader reader = process.StandardOutput)
                    {
                        string result = reader.ReadToEnd();
                        Console.WriteLine($"Python script output: {result}");
                    }

                    // Capture and print any errors from the Python script
                    using (StreamReader errorReader = process.StandardError)
                    {
                        string errors = errorReader.ReadToEnd();
                        if (!string.IsNullOrEmpty(errors))
                        {
                            Console.WriteLine($"Python script errors: {errors}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running Python script: {ex.Message}");
            }
        }

        // Example method for loading and processing a 3D mesh
        public static void LoadAndProcessMesh(string meshFilePath)
        {
            // In this example, we just print the file path, but you can use libraries like AssimpNet to load and manipulate the mesh
            Console.WriteLine($"Loading mesh from: {meshFilePath}");

            // Example: Use AssimpNet or other libraries to process the mesh
            // You could load the mesh, manipulate vertices, etc. depending on your needs
        }
    }
}