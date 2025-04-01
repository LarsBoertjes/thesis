import pycolmap

reconstruction_path = "../../data/experimental/bouwpub/results/sparse/0"

reconstruction = pycolmap.Reconstruction(reconstruction_path)

points3D = reconstruction.points3D

for point_id, point in points3D.items():
    print(f"Point ID: {point_id}")
    print(f"Coordinates: {point.xyz}")
    print(f"Color: {point.color}")


