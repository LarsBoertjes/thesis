using System;

public class CameraSpecs
{
	public string CameraId { get; set; }
	public Quantity<Micrometer> PixelSize { get; set; }
	public int WidthPx { get; set; }
	public int HeightPx { get; set; }
	public Quantity<Millimeter> FocalLength { get; set; }
	public Quantity<Millimeter> PrincipalPointX { get; set; }
	public Quantity<Millimeter> PrincipalPointY { get; set; }

	public CameraSpecs(string cameraId, Quantity<Millimeter> pixelSize, int widthPx, int heightPx, Quantity<Millimeter> focalLength,
		Quantity<Millimeter> ppx, Quantity<Millimeter> ppy)
	{
		CameraId = cameraId;
		PixelSize = pixelSize;
		WidthPx = widthPx;
		HeightPx = heightPx;
		FocalLength = focalLength;
		PrincipalPointX = ppx;
		PrincipalPointY = ppy;
	}
}
