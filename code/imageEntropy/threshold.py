import os
import cv2 as cv
import numpy as np

buildings = [
    #"1_detached_house_aerial",
    #"2_block_terraced_aerial",
    #"3_tall_building_aerial",
    #"4_large_building_aerial",
    #"5_bouwpub_groundbased",
    "6_geodelta_drone"
]

for building in buildings:
    input_folder_entropy = f"../../data/processed/{building}/entropy_image/edge_detection/"
    input_folder_mask = f"../../data/processed/{building}/object_isolation/building_masks/"
    output_folder = f"../../data/processed/{building}/threshold_edge/90/images/"
    os.makedirs(output_folder, exist_ok=True)

    # Loop through all images in the input folder
    for imageid in os.listdir(input_folder_mask):
        if not imageid.lower().endswith((".png", ".jpg", ".jpeg", ".tif", ".bmp")):
            continue

        image_path_entropy = os.path.join(input_folder_entropy, imageid)
        image_path_mask = os.path.join(input_folder_mask, imageid)

        # Load entropy image as float [0, 1]
        entropy_img = cv.imread(image_path_entropy, cv.IMREAD_GRAYSCALE)
        if entropy_img is None:
            print(f"Missing entropy image for {imageid}, skipping.")
            continue
        entropy_norm = entropy_img.astype(np.float32) / 255.0

        # Load mask image (uint8 or other)
        mask_img = cv.imread(image_path_mask, cv.IMREAD_UNCHANGED)
        if mask_img is None:
            print(f"Missing mask image for {imageid}, skipping.")
            continue

        # Create binary mask: 1 where entropy >= 0.75
        entropy_thresh = (entropy_norm >= 0.9).astype(np.uint8)

        # Apply thresholded entropy mask to building mask
        masked_output = cv.bitwise_and(mask_img, mask_img, mask=entropy_thresh)

        # Save the result
        output_path = os.path.join(output_folder, imageid)
        cv.imwrite(output_path, masked_output)
        print(f"[{building}] Thresholded mask saved: {output_path}")



