import os
import cv2
import numpy as np
from segment_anything import SamAutomaticMaskGenerator, sam_model_registry

# Initialize the SAM model
checkpoint_path = "checkpoints/sam_vit_h_4b8939.pth"
model_type = "vit_h"
sam = sam_model_registry[model_type](checkpoint=checkpoint_path)
mask_generator = SamAutomaticMaskGenerator(sam)

# Folder paths
image_folder = "../data/experimental"
output_folder = "../data/outputs"

# Process each image in the folder
for filename in os.listdir(image_folder):
    if filename.endswith(".png"):
        image_path = os.path.join(image_folder, filename)
        print(f"Processing: {filename}")

        # Load the image
        image = cv2.imread(image_path)
        if image is None:
            print(f"Failed to load image: {image_path}")
            continue

        image_rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)

        # Generate masks
        masks = mask_generator.generate(image_rgb)

        # Save or visualize the masks (example: save as a NumPy file)
        output_path = os.path.join(output_folder, f"{os.path.splitext(filename)[0]}_masks.npy")
        np.save(output_path, masks)
        print(f"Masks saved to: {output_path}")

        # Visualize the masks
        for i, mask in enumerate(masks):
            mask_overlay = mask['segmentation']  # mask segmentation (binary mask)
            color_mask = np.zeros_like(image)  # create an empty image for the color mask

            # Convert the binary mask to 3 channels (for overlaying)
            color_mask[mask_overlay == 1] = [0, 255, 0]  # color mask (green for example)

            # Overlay the mask onto the original image
            overlayed_image = cv2.addWeighted(image, 0.7, color_mask, 0.3, 0)

            # Show the image with the overlayed mask
            cv2.imshow(f"Mask {i+1} - {filename}", overlayed_image)

            # Optionally save the overlay image (e.g., with the same name as the original image)
            overlay_output_path = os.path.join(output_folder, f"{os.path.splitext(filename)[0]}_overlay_{i+1}.png")
            cv2.imwrite(overlay_output_path, overlayed_image)

        cv2.waitKey(0)  # Wait until a key is pressed to close the image window
        cv2.destroyAllWindows()  # Close all OpenCV windows

    print("Processing complete!")

