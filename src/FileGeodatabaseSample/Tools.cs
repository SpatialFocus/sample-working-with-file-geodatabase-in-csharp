// <copyright file="Tools.cs" company="Spatial Focus GmbH">
// Copyright (c) Spatial Focus GmbH. All rights reserved.
// </copyright>

namespace FileGeodatabaseSample
{
	using System.Collections.Generic;
	using FileGeodatabaseSample.Models;
	using OSGeo.OGR;
	using AxisMappingStrategy = OSGeo.OSR.AxisMappingStrategy;
	using CoordinateTransformation = OSGeo.OSR.CoordinateTransformation;
	using SpatialReference = OSGeo.OSR.SpatialReference;

	public static class Tools
	{
		public static Geometry GeometryToWGS84(Geometry geom, SpatialReference spatialReference)
		{
			if (geom is null)
			{
				return null;
			}

			geom.ExportToWkt(out string wkt);
			Geometry result = Geometry.CreateFromWkt(wkt);

			using SpatialReference dest = new SpatialReference(string.Empty);
			dest.ImportFromEPSG(4326);

			// GDAL 3 swapped coordinates. See https://github.com/OSGeo/gdal/issues/1546
			dest.SetAxisMappingStrategy(AxisMappingStrategy.OAMS_TRADITIONAL_GIS_ORDER);

			using CoordinateTransformation transformer = new CoordinateTransformation(spatialReference, dest);
			result.Transform(transformer);

			return result;
		}

		public static List<SampleItemGeometry> GetSampleItemGeometriesPolygon(Feature feature, SpatialReference spatialReference)
		{
			if (feature is null)
			{
				return null;
			}

			List<SampleItemGeometry> sampleItemGeometries = new List<SampleItemGeometry>();

			// TODO: If the spatialReference is WGS84, only one geometry should be stored
			Geometry geometry = feature.GetGeometryRef();
			using Geometry geometryWGS84 = Tools.GeometryToWGS84(geometry, spatialReference);

			sampleItemGeometries.Add(new SampleItemGeometry
			{
				GeometryWKT = geometry.ToText(), GeometryWGS84WKT = geometryWGS84.ToText(),
			});

			return sampleItemGeometries;
		}

		public static string ToText(this Geometry geometry)
		{
			if (geometry is null)
			{
				return null;
			}

			geometry.ExportToWkt(out string wkt);

			return wkt;
		}
	}
}