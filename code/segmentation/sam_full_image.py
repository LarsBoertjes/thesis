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


def apply_canny_edge_detection(image, low_threshold=100, high_threshold=200):
    """Apply Canny edge detection to an RGB image and return a binary edge image."""
    gray = cv2.cvtColor(image, cv2.COLOR_RGB2GRAY)
    edges = cv2.Canny(gray, low_threshold, high_threshold)
    return edges


def process_building_images(building, mask_generator):
    input_folder = f"../../data/processed/{building}/object_isolation/building_masks/"
    output_folder_masks = f"../../data/processed/{building}/entropy_preparation/building_level_segmentation/"
    output_folder_edges = f"../../data/processed/{building}/entropy_preparation/edge_detection/"

    os.makedirs(output_folder_masks, exist_ok=True)
    os.makedirs(output_folder_edges, exist_ok=True)

    # Initialize lists to store metrics for the building
    num_masks_list = []
    predicted_iou_list = []
    stability_score_list = []

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

        ## --- MASK GENERATION --- ##
        masks = mask_generator.generate(image_rgb)
        masked_rgba = create_masked_image(image_rgb, masks)

        # Save mask result
        base_name, ext = os.path.splitext(imageid)
        output_mask_path = os.path.join(output_folder_masks, imageid)
        count = 1
        while os.path.exists(output_mask_path):
            output_mask_path = os.path.join(output_folder_masks, f"{base_name}_{count}{ext}")
            count += 1
        cv2.imwrite(output_mask_path, masked_rgba)
        print(f"[{building}] Masked image saved: {output_mask_path}")

        ## --- EDGE DETECTION --- ##
        edges = apply_canny_edge_detection(image_rgb)

        # Save edge detection result
        output_edge_path = os.path.join(output_folder_edges, imageid)
        count = 1
        while os.path.exists(output_edge_path):
            output_edge_path = os.path.join(output_folder_edges, f"{base_name}_{count}{ext}")
            count += 1
        cv2.imwrite(output_edge_path, edges)
        print(f"[{building}] Edge image saved: {output_edge_path}")

        ## --- METRICS COLLECTION --- ##
        num_masks = len(masks)
        num_masks_list.append(num_masks)

        predicted_iou = np.mean([mask['predicted_iou'] for mask in masks])
        predicted_iou_list.append(predicted_iou)

        stability_score = np.mean([mask['stability_score'] for mask in masks])
        stability_score_list.append(stability_score)

    ## --- FINAL METRICS REPORT --- ##
    mean_num_masks = np.mean(num_masks_list)
    median_num_masks = np.median(num_masks_list)
    mean_predicted_iou = np.mean(predicted_iou_list)
    mean_stability_score = np.mean(stability_score_list)

    print(f"[{building}] mean number of masks: {mean_num_masks:.2f}")
    print(f"[{building}] median number of masks: {median_num_masks:.2f}")
    print(f"[{building}] mean predicted IoU: {mean_predicted_iou:.4f}")
    print(f"[{building}] mean stability score: {mean_stability_score:.4f}")


# --- Load SAM model ---
sam_checkpoint = "../../experiments/checkpoints/sam_vit_b_01ec64.pth"
model_type = "vit_b"
device = "cuda"

sam = sam_model_registry[model_type](checkpoint=sam_checkpoint)
sam.to(device=device)
mask_generator = SamAutomaticMaskGenerator(model=sam)

# --- Process all buildings ---
buildings = [
    "1_detached_house_aerial"
]

for building in buildings:
    print(f"Processing {building}")
    process_building_images(building, mask_generator)

print("Segmentation and edge detection complete for all buildings.")
