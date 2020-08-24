// <copyright file="Coordinate.cs" company="Spatial Focus GmbH">
// Copyright (c) Spatial Focus GmbH. All rights reserved.
// </copyright>

namespace FileGeodatabaseSample.Models
{
	public class Coordinate
	{
		public Coordinate(double x, double y)
		{
			X = x;
			Y = y;
		}

		public double X { get; set; }

		public double Y { get; set; }
	}
}