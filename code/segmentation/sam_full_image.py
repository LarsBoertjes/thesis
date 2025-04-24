import os
import cv2
import numpy as np
import random
from segment_anything import sam_model_registry, SamAutomaticMaskGenerator

def create_masked_image(image, masks):
    """Generate a colored RGBA image from masks."""
    mask_image_rgba = np.zeros((image.shape[0], image.shape[1], 4), dtype=np.uint8)
    for ann in masks:
        mask = ann['segmentation']
        color = [random.randint(0, 255) for _ in range(3)]
        mask_image_rgba[mask, :3] = color
        mask_image_rgba[mask, 3] = 255
    return mask_image_rgba

def process_building_images(building, mask_generator):
    input_folder = f"../../data/processed/{building}/object_isolation/building_masks/"
    output_folder = f"../../data/processed/{building}/entropy_preparation/building_level_segmentation/"
    os.makedirs(output_folder, exist_ok=True)

    for imageid in os.listdir(input_folder):
        if not imageid.lower().endswith((".png", ".jpg", ".jpeg", ".tif", ".bmp")):
            continue  # Skip non-image files

        image_path = os.path.join(input_folder, imageid)
        image = cv2.imread(image_path, cv2.IMREAD_UNCHANGED)

        if image is None:
            print(f"Failed to read {imageid}, skipping.")
            continue

        # Handle transparency and convert to RGB
        if image.shape[-1] == 4:
            image_rgb = cv2.cvtColor(image, cv2.COLOR_BGRA2RGB)
        else:
            image_rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)

        # Generate masks
        masks = mask_generator.generate(image_rgb)

        # Create RGBA mask image
        masked_rgba = create_masked_image(image, masks)

        # Write output
        base_name, ext = os.path.splitext(imageid)
        output_path = os.path.join(output_folder, imageid)
        count = 1
        while os.path.exists(output_path):
            output_path = os.path.join(output_folder, f"{base_name}_{count}{ext}")
            count += 1

        cv2.imwrite(output_path, masked_rgba)
        print(f"[{building}] Processed: {imageid} -> {output_path}")

# Load SAM model
sam_checkpoint = "../../experiments/checkpoints/sam_vit_b_01ec64.pth"
model_type = "vit_b"
device = "cuda"

sam = sam_model_registry[model_type](checkpoint=sam_checkpoint)
sam.to(device=device)

mask_generator = SamAutomaticMaskGenerator(model=sam)

# Process all buildings
buildings = [
    "1_detached_house_aerial",
    "3_tall_building_aerial",
    "4_large_building_aerial",
    "5_bouwpub_groundbased"
]

for building in buildings:
    print(f"Processing {building}")
    process_building_images(building, mask_generator)

print("Segmentation complete for all buildings.")
