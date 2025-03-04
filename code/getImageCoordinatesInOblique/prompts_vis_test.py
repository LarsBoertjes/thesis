import psycopg2
import numpy as np
import cv2
import matplotlib.pyplot as plt
import os

# Database configuration
DB_CONFIG = {
    "dbname": "aerial_bag_db",
    "user": "postgres",
    "password": os.getenv("DB_PASSWORD"),
    "host": "localhost",
    "port": "5432"
}

# Connect to the database
conn = psycopg2.connect(**DB_CONFIG)
cursor = conn.cursor()

# SQL query to fetch data for the specific imageid
image_id = "231005232_0056_01_0099_P00_01.jpg"
cursor.execute("SELECT array_agg(x_prompt) AS x_prompts, array_agg(y_prompt) AS y_prompts FROM image_prompts_2d_ta WHERE imageid = %s GROUP BY imageid;", (image_id,))

# Fetch and process results
result = cursor.fetchone()
if result is None:
    print(f"No data found for imageid: {image_id}")
    cursor.close()
    conn.close()
    exit()

# PostgreSQL already returns INTEGER[] as Python lists, no need for manual parsing
x_prompts = result[0]  # This is already a list of integers
y_prompts = result[1]  # This is already a list of integers

# Close the connection
cursor.close()
conn.close()

# Function to plot points on the image
def plot_points(image_path, x_coords, y_coords):
    # Load the image
    image = cv2.imread(image_path)
    if image is None:
        print(f"Error: Could not load image {image_path}")
        return

    image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)  # Convert BGR to RGB for Matplotlib

    # Plot the image
    plt.figure(figsize=(10, 6))
    plt.imshow(image)

    # Plot the points
    plt.scatter(x_coords, y_coords, c='red', marker='x', label='Prompts')

    plt.title(f"Image: {image_path}")
    plt.legend()
    plt.show()


# Define the correct image path
image_path = f"{image_id}"  # Adjust path based on your folder structure

# Convert lists to NumPy arrays
x_coords = np.array(x_prompts, dtype=np.int32)
y_coords = np.array(y_prompts, dtype=np.int32)

plot_points(image_path, x_coords, y_coords)
