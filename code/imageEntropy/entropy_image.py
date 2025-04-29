import os
import cv2
import numpy as np
from entropy_metrics import compute_kernel_entropy, compute_edge_entropy

# Define buildings for processing
buildings = [
    "1_detached_house_aerial"
]

# Kernel size and alpha for entropy computation
kernel_size = 100  # Example kernel size for the entropy filter
alpha = 0.05           # Example alpha value for edge entropy

for building in buildings:
    input_folder = f"../../data/processed/{building}/entropy_preparation/building_level_segmentation/"
    output_folder_kernel = f"../../data/processed/{building}/entropy_image/kernel/"
    output_folder_edge = f"../../data/processed/{building}/entropy_image/edge/"

    # Create output directories if they do not exist
    os.makedirs(output_folder_kernel, exist_ok=True)
    os.makedirs(output_folder_edge, exist_ok=True)

    # Loop through all images in the input folder
    for imageid in os.listdir(input_folder):
        if not imageid.lower().endswith((".png", ".jpg", ".jpeg", ".tif", ".bmp")):
            continue  # Skip non-image files

        image_path = os.path.join(input_folder, imageid)
        image = cv2.imread(image_path, cv2.IMREAD_UNCHANGED)

        if image is None:
            print(f"Failed to read {imageid}, skipping.")
            continue

        # Convert to grayscale for entropy computation
        gray_image = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)

        ## --- Kernel-based entropy --- ##
        kernel_entropy = compute_kernel_entropy(gray_image, kernel_size)

        # Save the kernel-based entropy image
        base_name, ext = os.path.splitext(imageid)
        output_kernel_path = os.path.join(output_folder_kernel, f"{base_name}_kernel{ext}")
        count = 1
        while os.path.exists(output_kernel_path):
            output_kernel_path = os.path.join(output_folder_kernel, f"{base_name}_kernel_{count}{ext}")
            count += 1
        cv2.imwrite(output_kernel_path, (kernel_entropy * 255).astype(np.uint8))  # Save as an image
        print(f"[{building}] Kernel entropy image saved: {output_kernel_path}")

        ## --- Edge-based entropy --- ##
        edge_entropy = compute_edge_entropy(gray_image, alpha)

        # Save the edge-based entropy image
        output_edge_path = os.path.join(output_folder_edge, f"{base_name}_edge{ext}")
        count = 1
        while os.path.exists(output_edge_path):
            output_edge_path = os.path.join(output_folder_edge, f"{base_name}_edge_{count}{ext}")
            count += 1
        cv2.imwrite(output_edge_path, (edge_entropy * 255).astype(np.uint8))  # Save as an image
        print(f"[{building}] Edge entropy image saved: {output_edge_path}")
        break

print("Entropy computation and image saving complete for all buildings.")
