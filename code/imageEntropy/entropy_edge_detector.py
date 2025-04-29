import os
import cv2 as cv
import numpy as np
import matplotlib.pyplot as plt

def compute_edge_entropy(binary_mask, alpha):
    """Computes edge-based entropy, considering proximity to edges or corners"""
    distances = cv.distanceTransform(255 - binary_mask, cv.DIST_L2, 5)
    edge_entropy = np.exp(-alpha * distances).astype(float)
    edge_entropy = (edge_entropy - np.min(edge_entropy)) / (np.max(edge_entropy) - np.min(edge_entropy) + 1e-8)
    return edge_entropy

# Define buildings for processing
buildings = [
    #"1_detached_house_aerial",
    #"3_tall_building_aerial",
    #"4_large_building_aerial",
    #"5_bouwpub_groundbased",
    "6_geodelta_drone"
]

alpha = 0.01

for building in buildings:
    input_folder = f"../../data/processed/{building}/object_isolation/building_masks/"
    output_folder = f"../../data/processed/{building}/entropy_image/edge_detection/"
    os.makedirs(output_folder, exist_ok=True)

    # Loop through all images in the input folder
    for imageid in os.listdir(input_folder):
        if not imageid.lower().endswith((".png", ".jpg", ".jpeg", ".tif", ".bmp")):
            continue # Skip non-image files in input folder

        image_path = os.path.join(input_folder, imageid)
        image = cv.imread(image_path, cv.IMREAD_GRAYSCALE)

        if image is None:
            print(f"Failed to read {imageid}, skipping.")
            continue

        # === Edge Masks ===
        canny_edges = cv.Canny(image, 260, 300)

        # === Entropy Computation ===
        entropy_canny = compute_edge_entropy(canny_edges, alpha)

        # Save the edge-detection entropy image
        base_name, ext = os.path.splitext(imageid)
        output_path = os.path.join(output_folder, f"{base_name}{ext}")

        cv.imwrite(output_path, (entropy_canny * 255).astype(np.uint8))
        print(f"[{building}] canny edge detection entropy saved : {output_path}")




