import numpy as np
import cv2
from scipy.ndimage import generic_filter

def entropy_filter(window):
    """Computes entropy for a given kernel window."""
    unique, counts = np.unique(window, return_counts=True)
    probabilities = counts / counts.sum()
    entropy = -np.sum(probabilities * np.log2(probabilities + 1e-10))
    return entropy

def compute_kernel_entropy(image, kernel_size):
    """Computes entropy on a certain image"""
    # Compute entropy values using kernel
    entropy_map = generic_filter(image.astype(float), entropy_filter, size=kernel_size)

    # Normalize entropy for visualization
    entropy_map = (entropy_map - np.min(entropy_map)) / (np.max(entropy_map) - np.min(entropy_map))
    return entropy_map

def compute_edge_entropy(image, alpha):
    """Computes edge-based entropy, considering proximity to edges."""
    edge_mask = cv2.Canny(image, threshold1=100, threshold2=200)

    distances = cv2.distanceTransform(255 - edge_mask, cv2.DIST_L2, 5)

    edge_entropy = np.exp(-alpha * distances).astype(float)

    edge_entropy = (edge_entropy - np.min(edge_entropy)) / (np.max(edge_entropy) - np.min(edge_entropy))
    return edge_entropy
