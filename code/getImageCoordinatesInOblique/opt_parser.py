# Script to parse opt file for exteriors-camera-all.
import csv

input_file = "../../data/BAGpositiondata/image_parameters/exteriors-camera-all.opt"
camera_output_file = "../../data/BAGpositiondata/image_parameters/cameras.csv"
images_output_file = "../../data/BAGpositiondata/image_parameters/images.csv"

with open(input_file, 'r', encoding="utf-8") as file:
    lines = file.readlines()

camera_data = []
images_data = []

camera_section = True

for line in lines:
    line = line.strip()

    if not line:
        continue

    if "ImageId" in line:
        camera_section = False
        continue

    # Processing camera section
    if camera_section:
        parts = line.split()
        if len(parts) == 7:
            camera_data.append(parts)

    # Processing image section
    else:
        parts = line.split()
        if len(parts) == 8:
            images_data.append(parts)

# Write camera data to CSV
with open(camera_output_file, 'w') as csvfile:
    writer = csv.writer(csvfile, delimiter=',')
    #writer.writerows(["CameraId", "PixelSize_um", "Height_px", "Focal_mm", "PPX_mm", "PPY_mm"])
    writer.writerows(camera_data)

# Write images data to CSV
with open(images_output_file, 'w') as csvfile:
    writer = csv.writer(csvfile, delimiter=',')
    #writer.writerows(["ImageId", "X_m", "Y_m", "Z_m", "Omega_deg", "Phi_deg", "Kappa_deg", "CameraId"])
    writer.writerows(images_data)


