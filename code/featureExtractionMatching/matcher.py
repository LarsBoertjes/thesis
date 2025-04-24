import os
import subprocess
from pathlib import Path
from lightglue import LightGlue, SuperPoint, DISK
from lightglue.utils import load_image, rbd
from lightglue import viz2d
import torch
import matplotlib.pyplot as plt

def setup_lightglue():
    """Checks if LightGlue is in the right directory and installs it if not."""
    if Path.cwd().name != "LightGlue":
        if not Path("LightGlue").exists():
            subprocess.run(["git", "clone", "--quiet", "https://github.com/cvg/LightGlue/"], check=True)
        os.chdir("LightGlue")
        subprocess.run(["pip", "install", "--progress-bar", "off", "--quiet", "-e", "."], check=True)

def display_images(image0, image1, kpts0, kpts1, m_kpts0, m_kpts1, matches01):
    """Display images and matches using viz2d's internal plotting."""
    # Plot matches (this creates its own figure)
    axes = viz2d.plot_images([image0, image1])
    viz2d.plot_matches(m_kpts0, m_kpts1, color="lime", lw=0.2)
    viz2d.add_text(0, f'Stop after {matches01['stop']} layers", fs=20)

    # Show first plot
    import matplotlib.pyplot as plt
    plt.show()

    # Plot keypoints
    kpc0 = viz2d.cm_prune(matches01["prune0"])
    kpc1 = viz2d.cm_prune(matches01["prune1"])
    viz2d.plot_images([image0, image1])
    viz2d.plot_keypoints([kpts0, kpts1], colors=[kpc0, kpc1], ps=10)

    # Show second plot
    plt.show()


def process_images():
    """The main image matching logic using LightGlue."""
    images = Path("../../data/processed/4_large_building_aerial/object_isolation/building_masks")

    # Determine device (GPU or CPU)
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

    # Initialize the extractor and matcher
    extractor = SuperPoint(max_num_keypoints=2048).eval().to(device)
    matcher = LightGlue(features="superpoint").eval().to(device)

    # Load the images
    image0 = load_image(images / "183006898_0061_01_0137_P00_01.png")
    image1 = load_image(images / "183006898_0061_01_0137_P00_01.png")

    # Extract features
    feats0 = extractor.extract(image0.to(device))
    feats1 = extractor.extract(image1.to(device))
    matches01 = matcher({"image0": feats0, "image1": feats1})

    # Remove batch dimension
    feats0, feats1, matches01 = [rbd(x) for x in [feats0, feats1, matches01]]

    # Get keypoints and matches
    kpts0, kpts1, matches = feats0["keypoints"], feats1["keypoints"], matches01["matches"]
    m_kpts0, m_kpts1 = kpts0[matches[..., 0]], kpts1[matches[..., 1]]

    # Display the visualizations
    display_images(image0, image1, kpts0, kpts1, m_kpts0, m_kpts1, matches01)

def main():
    try:
        setup_lightglue()
        process_images()
    except Exception as e:
        print(f"An error occurred: {e}")

    print(os.getcwd())

if __name__ == "__main__":
    main()