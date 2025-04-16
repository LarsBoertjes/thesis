using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Globalization;
using System.Linq;
using Accord.Collections;
using Accord.MachineLearning;
using Geodelta.IO.Xml;
using MathNet.Numerics.Statistics.Mcmc;
using Accord;
using Accord.Math;
using MathNet.Numerics.Integration;
using Accord.Statistics.Kernels;


class Program
{
    static void Main()
    {
        // //string outputFilePath = "../../../../../data/experimental/bouwpub/  .obj";
        string filePath = "../../../../../data/outputs/bagids/0344100000157740_masks/dense.obj";
        List<double[]> points = LoadOBJVertices(filePath);

        // Convert to array for kd-tree
        double[][] data = points.ToArray();

        // Build kd-tree
        KDTree<float> tree = KDTree.FromData<float>(data);

        // Compute curvature for each point
        var densityValues = new List<(double density, double[] position)>();

        foreach (var node in tree)
        {
            double density = ComputeDensity(node, tree);
            densityValues.Add((density, node.Position));
        }

        // Sort by curvature and take top 10%
        int topCount = (int)(densityValues.Count * 0.1);
        var topDensityPoints = densityValues.OrderByDescending(c => c.density).Take(topCount).ToList();

        // Write to output OBJ file
        //string outputFilePath = "../../../../../data/experimental/bouwpub/high_density_points.obj";
        string outputFilePath = "../../../../../data/outputs/bagids/0344100000157740_masks/high_density_points.obj";
        WriteOBJ(outputFilePath, topDensityPoints);
    }

    static List<double[]> LoadOBJVertices(string filePath)
    {
        var points = new List<double[]>();

        foreach (var line in File.ReadLines(filePath))
        {
            if (line.StartsWith("v ")) // Vertex Line
            {
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                double x = double.Parse(parts[1], CultureInfo.InvariantCulture);
                double y = double.Parse(parts[2], CultureInfo.InvariantCulture);
                double z = double.Parse(parts[3], CultureInfo.InvariantCulture);
                points.Add(new double[] { x, y, z });
            }
        }

        // TODO: also read in vertex normals, and check which one is there without computing them afterwards.

        return points;
    }

    static void WriteOBJ(string filePath, List<(double curvature, double[] position)> points)
    {
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            foreach (var point in points)
            {
                writer.WriteLine($"v {point.position[0].ToString(CultureInfo.InvariantCulture)} {point.position[1].ToString(CultureInfo.InvariantCulture)} {point.position[2].ToString(CultureInfo.InvariantCulture)}");
            }
        }
    }

    /*static double ComputeCurvature(KDTreeNode<float> point, KDTree<float> tree)
    {
        if (point == null || tree == null)
        {
            return 0;
        }

        var neighbors = tree.Nearest(point.Position, 10); // Get 10 nearest neighbors
        if (neighbors.Count == 0)
        {
            return 0;
        }

        // Compute gravity center (Oj)
        double[] centroid = new double[point.Position.Length];
        foreach (var neighbor in neighbors)
        {
            centroid = centroid.Add(neighbor.Node.Position);
        }
        centroid = centroid.Divide(neighbors.Count);

        // Compute covariance matrix
        double[,] covarianceMatrix = new double[3, 3];
        foreach (var neighbor in neighbors)
        {
            double[] diff = neighbor.Node.Position.Subtract(centroid);
            double[,] outerProduct = diff.Outer(diff);
            covarianceMatrix = covarianceMatrix.Add(outerProduct);
        }
        covarianceMatrix = covarianceMatrix.Divide(neighbors.Count);

        // Compute eigenvalues & eigenvectors
        var evd = new Accord.Math.Decompositions.EigenvalueDecomposition(covarianceMatrix);
        double[] eigenvalues = evd.RealEigenvalues;
        double[,] eigenvectors = evd.Eigenvectors;

        // Find the smallest eigenvalue's corresponding eigenvector (normal vector)
        int minIndex = eigenvalues.IndexOf(eigenvalues.Min());
        double[] normalVector = eigenvectors.GetColumn(minIndex);

        // Compute mean included angles between normal vectors
        double curvatureIndex = 0;
        foreach (var neighbor in neighbors)
        {
            var neighborEvd = new Accord.Math.Decompositions.EigenvalueDecomposition(covarianceMatrix);
            double[] neighborEigenvalues = neighborEvd.RealEigenvalues;
            double[,] neighborEigenvectors = neighborEvd.Eigenvectors;
            int neighborMinIndex = neighborEigenvalues.IndexOf(neighborEigenvalues.Min());
            double[] neighborNormalVector = neighborEigenvectors.GetColumn(neighborMinIndex);

            double dotProduct = normalVector.Dot(neighborNormalVector);
            curvatureIndex += (dotProduct / (normalVector.Length * neighborNormalVector.Length) + 1.1);
        }

        return curvatureIndex / neighbors.Count;
    }*/

    static double ComputeDensity(KDTreeNode<float> point, KDTree<float> tree)
    {
        int numNeighbors = 10;
        var neighbors = tree.Nearest(point.Position, numNeighbors);
        if (neighbors.Count == 0)
        {
            return 0;
        }

        double totalDistance = 0;
        foreach (var neighbor in neighbors)
        {
            double distance = Math.Sqrt(
                point.Position.Zip(neighbor.Node.Position, (a, b) => (a - b) * (a - b)).Sum()
            );
            totalDistance += distance;
        }

        double density = Math.Round(1.0 / (totalDistance / numNeighbors), 6);

        return density;
    }

    /*static double ComputeEdge(double[] point, double[][] neighbors)
    {
        double edge;
        return edge;
    }*/

}