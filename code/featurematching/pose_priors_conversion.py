import numpy as np
from scipy.spatial.transform import Rotation as R


def euler_to_quaternion(omega, phi, kappa):
    """Convert Euler angles (omega, phi, kappa) to quaternions (QW, QX, QY, QZ)."""
    omega, phi, kappa = np.radians([omega, phi, kappa])  # Convert to radians
    rotation = R.from_euler('ZYX', [kappa, phi, omega], degrees=False)
    qw, qx, qy, qz = rotation.as_quat()  # Returns (qx, qy, qz, qw)
    return qw, qx, qy, qz, rotation


def compute_translation(x, y, z, rotation):
    """Compute the translation vector TX, TY, TZ using -R^T * P."""
    P = np.array([x, y, z])
    R_inv = rotation.as_matrix().T  # Transpose of the rotation matrix
    T = -R_inv @ P
    return T[0], T[1], T[2]  # TX, TY, TZ


def process_optdata(input_file, output_file):
    """Read optdata.txt, convert values, and write images.txt in the correct format."""
    with open(input_file, 'r') as infile, open(output_file, 'w') as outfile:
        lines = infile.readlines()
        image_id = 1  # Start numbering from 1

        for line in lines:
            parts = line.strip().split()
            if len(parts) < 7:
                continue  # Skip malformed lines

            image_name, x, y, z, omega, phi, kappa = parts[:7]
            x, y, z = map(float, [x, y, z])
            omega, phi, kappa = map(float, [omega, phi, kappa])

            # Convert to quaternions
            qw, qx, qy, qz, rotation = euler_to_quaternion(omega, phi, kappa)

            # Compute translation vector
            tx, ty, tz = compute_translation(x, y, z, rotation)

            # Write to images.txt with blank line after each entry
            outfile.write(
                f"{image_id} {qw:.6f} {qx:.6f} {qy:.6f} {qz:.6f} {tx:.6f} {ty:.6f} {tz:.6f} 1 {image_name}.jpg\n\n")

            image_id += 1  # Increment image ID


# Run the function
process_optdata("optdata.txt", "images.txt")
print("images.txt file created successfully!")
