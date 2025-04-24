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
using System.Reflection.Metadata.Ecma335;
using Accord.Math.Geometry;


class Program
{
    static void Main()
    {
        // string filePath = "../../../../../data/experimental/bouwpub/results/k150_e150/dense/0/fused.obj"; // bouwpub
        string filePath = "../../../../../data/outputs/bagids/0344100000157740_masks/dense.obj"; // bagid 0344100000157740
        List<double[]> points = LoadOBJVertices(filePath);

        // Convert to array for kd-tree
        double[][] data = points.ToArray();

        // Build kd-tree
        KDTree<float> tree = KDTree.FromData<float>(data);

        // Compute curvature, density and edge for each point
        var curvatureValues = new List<(double curvature, double[] position)>();
        var densityValues = new List<(double density, double[] position)>();
        var edgeValues = new List<(double edge, double[] position)>();

        int numNeighbors = 25;

        var curvatureMap = new Dictionary<KDTreeNode<float>, double>();
        foreach (var node in tree)
        {
            double curvature = ComputePointCurvature(node, tree, numNeighbors);
            curvatureMap[node] = curvature;
        }

        foreach (var node in tree)
        {
            double curvature = ComputeCurvature(node, tree, numNeighbors, curvatureMap);
            double density = ComputeDensity(node, tree);
            double edge = ComputeEdge(node, tree);

            curvatureValues.Add((curvature, node.Position));
            densityValues.Add((density, node.Position));
            edgeValues.Add((edge, node.Position));
        }

        // Sort by curvature and take top 10%
        int topCountCurvature = (int)(curvatureValues.Count * 0.1);
        var topCurvaturePoints = curvatureValues.OrderByDescending(c => c.curvature).Take(topCountCurvature).ToList();

        int topCountDensity = (int)(densityValues.Count * 0.1);
        var topDensityPoints = densityValues.OrderByDescending(c => c.density).Take(topCountDensity).ToList();

        int topCountEdge = (int)(edgeValues.Count * 0.1);
        var topEdgePoints = edgeValues.OrderByDescending(c => c.edge).Take(topCountEdge).ToList();

        // Write to output OBJ file
        // bouwpub
        /*string outputFilePathCurvature =  "../../../../../data/experimental/bouwpub/results/k150_e150/dense/0/high_curvature_entropy_points.obj";
        string outputFilePathEdge = "../../../../../data/experimental/bouwpub/results/k150_e150/dense/0/high_edge_points.obj";
        string outputFilePathDensity = "../../../../../data/experimental/bouwpub/results/k150_e150/dense/0/high_density_points.obj";*/

        // bagid 0344100000157740
        string outputFilePathCurvature = "../../../../../data/outputs/bagids/0344100000157740_masks/high_curvature_points.obj";
        string outputFilePathEdge = "../../../../../data/outputs/bagids/0344100000157740_masks/high_edge_points.obj";
        string outputFilePathDensity = "../../../../../data/outputs/bagids/0344100000157740_masks/high_density_points.obj";

        WriteOBJ(outputFilePathCurvature, topCurvaturePoints);
        WriteOBJ(outputFilePathEdge, topEdgePoints);
        WriteOBJ(outputFilePathDensity, topDensityPoints);

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

    static double ComputeCurvature(KDTreeNode<float> point, KDTree<float> tree, int numNeighbors, Dictionary<KDTreeNode<float>, double> curvatureMap)
    {
        if (point == null || tree == null || !curvatureMap.ContainsKey(point)) return 0;

        var neighbors = tree.Nearest(point.Position, numNeighbors);
        if (neighbors.Count == 0) return 0;

        // Collect curvatures for point + neighbors
        List<double> curvatures = new List<double> { curvatureMap[point] };

        foreach (var neighbor in neighbors)
        {
            if (curvatureMap.TryGetValue(neighbor.Node, out var c))
                curvatures.Add(c);
        }

        double sum = curvatures.Sum();
        if (sum == 0) return 0;

        List<double> probabilities = curvatures.Select(k => k / sum).ToList();

        double entropy = 0;
        foreach (var p in probabilities)
        {
            if (p > 0)
                entropy -= p * Math.Log(p);
        }

        double maxEntropy = Math.Log(probabilities.Count);
        if (maxEntropy == 0) return 0;

        return entropy / maxEntropy;
    }


    static double ComputePointCurvature(KDTreeNode<float> point, KDTree<float> tree, int numNeighbors)
    {
        var neighbors = tree.Nearest(point.Position, numNeighbors);
        if (neighbors.Count == 0) return 0;

        double[] centroid = new double[3];
        foreach (var neighbor in neighbors)
            centroid = centroid.Add(neighbor.Node.Position);
        centroid = centroid.Divide(neighbors.Count);

        double[,] covarianceMatrix = new double[3, 3];
        foreach (var neighbor in neighbors)
        {
            double[] diff = neighbor.Node.Position.Subtract(centroid);
            covarianceMatrix = covarianceMatrix.Add(diff.Outer(diff));
        }
        covarianceMatrix = covarianceMatrix.Divide(neighbors.Count);

        var evd = new Accord.Math.Decompositions.EigenvalueDecomposition(covarianceMatrix);
        double[] eigenvalues = evd.RealEigenvalues;
        double sum = eigenvalues.Sum();
        if (sum == 0) return 0;

        return eigenvalues.Min() / sum;
    }



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

    static double ComputeEdge(KDTreeNode<float> point, KDTree<float> tree)
    {
        int numNeighbors = 10;
        var neighbors = tree.Nearest(point.Position, numNeighbors);

        if (neighbors.Count == 0)
        {
            return 0;
        }

        int dimensions = point.Position.Length;
        float[] gravityCenter = new float[dimensions];

        // Sum all neighbor positions
        foreach (var neighbor in neighbors)
        {
            for (int i = 0; i < dimensions; i++)
            {
                gravityCenter[i] += (float)neighbor.Node.Position[i];

            }
        }

        // Average to get gravity center
        for (int i = 0; i < dimensions; i++)
        {
            gravityCenter[i] /= numNeighbors;
        }

        // Compute Euclidean distance from the point to the gravity center
        double edgeIndex = Math.Sqrt(
                point.Position.Zip(gravityCenter, (a, b) => (a - b) * (a - b)).Sum());

        return Math.Round(edgeIndex, 6);
    }
}