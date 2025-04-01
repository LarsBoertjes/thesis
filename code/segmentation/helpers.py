import numpy as np
import torch
import matplotlib.pyplot as plt
import cv2
import psycopg2
import os

def show_mask(mask, ax, random_color=False):
    '''
    Show the segmented mask on top of the image
    '''
    if random_color:
        color = np.concatenate([np.random.random(3), np.array([0.6])], axis=0)
    else:
        color = np.array([30/255, 144/255, 255/255, 0.6])
    h, w = mask.shape[-2:]
    mask_image = mask.reshape(h, w, 1) * color.reshape(1, 1, -1)
    ax.imshow(mask_image)

def show_box(box, ax):
    x0, y0, x1, y1 = box
    w, h = x1 - x0, y1 - y0
    ax.add_patch(plt.Rectangle((x0, y0), w, h, edgecolor='green', facecolor=(0,0,0,0), lw=2))

def wkt_to_bbox(wkt):
    '''
    :param wkt: WKT of the bagid polygon
    :return: bounding box of the bagid polygon
    '''
    coords = wkt.replace("POLYGON((", "").replace("))", "").split(", ")
    x_vals = []
    y_vals = []
    for coord in coords:
        x, y = map(float, coord.split())
        x_vals.append(x)
        y_vals.append(y)
    return [min(x_vals), min(y_vals), max(x_vals), max(y_vals)]

def transform_bounding_boxes(boxes, img_width, img_height, direction):
    transformed_boxes = []

    for box in boxes:
        x_min, y_min, x_max, y_max = box

        # Step 1: Rotate 90 degrees clockwise
        new_x_min = y_min
        new_y_min = img_width - x_max
        new_x_max = y_max
        new_y_max = img_width - x_min

        # Step 2: Flip vertically
        final_x_min = new_x_min
        final_y_min = img_height - new_y_max
        final_x_max = new_x_max
        final_y_max = img_height - new_y_min

        # Step 3: Shift downward (if boxes are too high)
        shift_down = img_height / 3.1  # Shift down by half the image height
        final_y_min += shift_down
        final_y_max += shift_down

        if direction == "Fwd":
            transformed_boxes.append(
                [final_x_min - 20, final_y_min - 400, final_x_max + 20, final_y_max + 20])  # Forward
        elif direction == "Left":
            transformed_boxes.append(
                [final_x_min - 200, final_y_min + 150, final_x_max + 20, final_y_max + 300])  # Left
        elif direction == "Right":
            transformed_boxes.append(
                [final_x_min + 100, final_y_min - 20, final_x_max + 150, final_y_max + 100])  # Right
        elif direction == "Back":
            transformed_boxes.append(
                [final_x_min + 50, final_y_min + 200, final_x_max + 200, final_y_max + 200])  # Back

    return transformed_boxes

def get_unique_filename(output_dir, filename):
    '''
    Create a unique filename by appending a counter or timestamp if the file already exists
    '''
    base_name, ext = os.path.splitext(filename)
    counter = 1
    while os.path.exists(os.path.join(output_dir, filename)):
        filename = f"{base_name}_{counter}{ext}"
        counter += 1
    return filename

def connect_to_database(imageid):
    # Database configuration
    DB_CONFIG = {
        "dbname" : "BagMapDB",
        "user" : "postgres",
        "password" : os.getenv("DB_PASSWORD"),
        "host" : "localhost",
        "port" : "5432"
    }

    # Connect to the database
    conn = psycopg2.connect(**DB_CONFIG)
    cursor = conn.cursor()

    cursor.execute("SELECT bag_ids, bboxes FROM bag_in_image_utrecht WHERE image_name = %s;", (imageid,))

    result = cursor.fetchone()

    cursor.close()
    conn.close()

    if result:
        bag_ids = result[0]  # List of bag IDs
        bboxes_wkt = result[1]  # List of WKT polygons
        return bag_ids, bboxes_wkt

    return [], []


