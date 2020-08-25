// <copyright file="Program.cs" company="Spatial Focus GmbH">
// Copyright (c) Spatial Focus GmbH. All rights reserved.
// </copyright>

namespace FileGeodatabaseSample
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text.Json;
	using FileGeodatabaseSample.Models;
	using MaxRev.Gdal.Core;
	using OSGeo.OGR;
	using SpatialReference = OSGeo.OSR.SpatialReference;

	public static class Program
	{
		private const string TestDataSetPath = @"../../../../../data/UA_AT.gdb.zip";

		public static void Main()
		{
			GdalBase.ConfigureAll();

			string dataSetPath = Path.GetFullPath(Program.TestDataSetPath, Directory.GetCurrentDirectory());

			Program.Open(dataSetPath);

			Program.Details(dataSetPath);

			Program.AttributeIndex(dataSetPath);

			Program.SpatialIndex(dataSetPath);

			Program.SpatialIndexAdvanced(dataSetPath);

			Program.SelectRandomFeatures(dataSetPath, 3);

			Program.SelectRandomFeatureWeighted(dataSetPath, 3);

			Coordinate coordinate = new Coordinate(4793000, 2809000); // Vienna Town Hall (Rathaus)
			Program.SelectFeatureAtLocations(dataSetPath, new List<Coordinate> { coordinate });
		}

		private static void AttributeIndex(string dataSetPath)
		{
			Dictionary<string, int> values = new Dictionary<string, int>();

			Driver fileGdbDriver = Ogr.GetDriverByName("OpenFileGDB");
			DataSource dataSource = fileGdbDriver.Open(dataSetPath, 0);
			Layer layer = dataSource.GetLayerByIndex(0);

			Feature feature = layer.GetNextFeature();

			while (feature != null)
			{
				string classification = feature.GetFieldAsString("CODE2012");

				if (!values.ContainsKey(classification))
				{
					values.Add(classification, 1);
				}
				else
				{
					values[classification]++;
				}

				feature = layer.GetNextFeature();
			}

			IOrderedEnumerable<KeyValuePair<string, int>> result = values.OrderBy(x => x.Key);

			foreach (KeyValuePair<string, int> keyValuePair in result)
			{
				Console.WriteLine($"Classification {keyValuePair.Key} occurs {keyValuePair.Value,5} times.");
			}
		}

		private static void Details(string dataSetPath)
		{
			Driver fileGdbDriver = Ogr.GetDriverByName("OpenFileGDB");
			DataSource dataSource = fileGdbDriver.Open(dataSetPath, 0);
			Layer layer = dataSource.GetLayerByIndex(0);

			string shapeType = layer.GetGeomType().ToString("G").Substring(3);
			Console.WriteLine($"Shape Type: {shapeType}");

			SpatialReference spatialReference = layer.GetSpatialRef();
			string projectionName = spatialReference.GetName();
			Console.WriteLine($"Projection: {projectionName}");

			using Envelope extent = new Envelope();
			layer.GetExtent(extent, 0);
			var dataSetExtent = new
			{
				XMin = extent.MinX, XMax = extent.MaxX, YMin = extent.MinY, YMax = extent.MaxY,
			};

			Console.WriteLine($"Extent: {JsonSerializer.Serialize(dataSetExtent, new JsonSerializerOptions { WriteIndented = true })}");

			int featureCount = (int)layer.GetFeatureCount(0);
			Console.WriteLine($"Feature Count: {featureCount}");

			List<dynamic> columns = new List<dynamic>();

			FeatureDefn layerDefinition = layer.GetLayerDefn();

			for (int j = 0; j < layerDefinition.GetFieldCount(); j++)
			{
				FieldDefn field = layerDefinition.GetFieldDefn(j);
				columns.Add(new { Name = field.GetName(), DataType = field.GetFieldTypeName(field.GetFieldType()), });
			}

			Console.WriteLine($"Columns: {JsonSerializer.Serialize(columns, new JsonSerializerOptions { WriteIndented = true })}");
		}

		private static void Open(string dataSetPath)
		{
			Driver fileGdbDriver = Ogr.GetDriverByName("OpenFileGDB");
			DataSource dataSource = fileGdbDriver.Open(dataSetPath, 0);

			int layerCount = dataSource.GetLayerCount();

			for (int i = 0; i < layerCount; i++)
			{
				Layer layer = dataSource.GetLayerByIndex(i);

				Console.WriteLine($"Layer: {layer.GetName()}");
			}
		}

		private static void SelectFeatureAtLocations(string dataSetPath, IEnumerable<Coordinate> locations)
		{
			List<dynamic> result = new List<dynamic>();
			Driver fileGdbDriver = Ogr.GetDriverByName("OpenFileGDB");
			DataSource dataSource = fileGdbDriver.Open(dataSetPath, 0);

			Layer layer = dataSource.GetLayerByIndex(0);
			SpatialReference spatialReference = layer.GetSpatialRef();

			foreach (Coordinate location in locations)
			{
				using Geometry point = new Geometry(wkbGeometryType.wkbPoint);
				point.AddPoint(location.X, location.Y, 0);

				Feature feature = layer.GetNextFeature();

				while (feature != null)
				{
					Geometry geometry = feature.GetGeometryRef();

					if (geometry.Intersects(point))
					{
						break;
					}

					feature = layer.GetNextFeature();
				}

				if (feature != null)
				{
					result.Add(new
					{
						FeatureId = feature.GetFID(),
						Classification = feature.GetFieldAsString("CODE2012"),
						FeatureGeometry = Tools.GetSampleItemGeometriesPolygon(feature, spatialReference),
					});
				}
			}

			Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
		}

		private static void SelectRandomFeatures(string dataSetPath, int sampleSize)
		{
			List<dynamic> result = new List<dynamic>();
			Driver fileGdbDriver = Ogr.GetDriverByName("OpenFileGDB");
			DataSource dataSource = fileGdbDriver.Open(dataSetPath, 0);

			Layer layer = dataSource.GetLayerByIndex(0);
			SpatialReference spatialReference = layer.GetSpatialRef();

			// A list of all the feature indices in the layer from 0 to featureCount-1
			List<int> featureList = Enumerable.Range(0, (int)layer.GetFeatureCount(0)).ToList();
			Random random = new Random();

			for (int i = 0; i < sampleSize; i++)
			{
				int index = random.Next(0, featureList.Count);

				// Get the feature at the selected index position
				Feature feature = layer.GetFeature(featureList[index]);
				featureList.RemoveAt(index);

				result.Add(new
				{
					FeatureId = feature.GetFID(),
					Classification = feature.GetFieldAsString("CODE2012"),
					FeatureGeometry = Tools.GetSampleItemGeometriesPolygon(feature, spatialReference),
				});
			}

			Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
		}

		private static void SelectRandomFeatureWeighted(string dataSetPath, int sampleSize)
		{
			ColumnScale<string> columnScale = Program.SpatialIndexAdvanced(dataSetPath);

			List<dynamic> result = new List<dynamic>();
			Driver fileGdbDriver = Ogr.GetDriverByName("OpenFileGDB");
			DataSource dataSource = fileGdbDriver.Open(dataSetPath, 0);

			Layer layer = dataSource.GetLayerByIndex(0);
			SpatialReference spatialReference = layer.GetSpatialRef();

			List<int> featureIds = new List<int>();
			Random random = new Random();

			for (int i = 0; i < sampleSize; i++)
			{
				double randomValue = random.NextDouble();
				KeyValuePair<string, ColumnValueScale> columnValueScale = columnScale.ColumnValueScales
					.Where(x => x.Value.ProbabilityLimit > randomValue)
					.Where(x => x.Value.Features.Any(y => !featureIds.Contains(y.FeatureId)))
					.OrderBy(x => x.Value.ProbabilityLimit)
					.FirstOrDefault();

				if (columnValueScale.Value != null)
				{
					FeatureScale featureScale;

					do
					{
						randomValue = random.NextDouble();

						featureScale = columnValueScale.Value.Features.Where(x => x.ProbabilityLimit > randomValue)
							.Where(x => !featureIds.Contains(x.FeatureId))
							.OrderBy(x => x.ProbabilityLimit)
							.FirstOrDefault();
					}
					while (featureScale != null && featureIds.Contains(featureScale.FeatureId));

					Feature feature = layer.GetFeature(featureScale.FeatureId);
					featureIds.Add(featureScale.FeatureId);

					result.Add(new
					{
						FeatureId = feature.GetFID(),
						Classification = feature.GetFieldAsString("CODE2012"),
						FeatureGeometry = Tools.GetSampleItemGeometriesPolygon(feature, spatialReference),
					});
				}
			}

			Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
		}

		private static void SpatialIndex(string dataSetPath)
		{
			Dictionary<string, double> values = new Dictionary<string, double>();

			Driver fileGdbDriver = Ogr.GetDriverByName("OpenFileGDB");
			DataSource dataSource = fileGdbDriver.Open(dataSetPath, 0);
			Layer layer = dataSource.GetLayerByIndex(0);

			Feature feature = layer.GetNextFeature();

			while (feature != null)
			{
				string classification = feature.GetFieldAsString("CODE2012");
				Geometry geometry = feature.GetGeometryRef();
				double area = geometry.Area();

				if (!values.ContainsKey(classification))
				{
					values.Add(classification, area);
				}
				else
				{
					values[classification] += area;
				}

				feature = layer.GetNextFeature();
			}

			double totalArea = values.Sum(x => x.Value);

			Dictionary<string, double> result = values.OrderBy(x => x.Key).ToDictionary(o => o.Key, o => o.Value / totalArea);

			foreach (KeyValuePair<string, double> keyValuePair in result)
			{
				Console.WriteLine($"Classification {keyValuePair.Key} occupies {keyValuePair.Value,7:P} of the total area.");
			}
		}

		private static ColumnScale<string> SpatialIndexAdvanced(string dataSetPath)
		{
			ColumnScale<string> columnScale = new ColumnScale<string>();

			Driver fileGdbDriver = Ogr.GetDriverByName("OpenFileGDB");
			DataSource dataSource = fileGdbDriver.Open(dataSetPath, 0);
			Layer layer = dataSource.GetLayerByIndex(0);

			Feature feature = layer.GetNextFeature();

			while (feature != null)
			{
				string classification = feature.GetFieldAsString("CODE2012");
				Geometry geometry = feature.GetGeometryRef();
				double size = geometry.Area();

				columnScale.AddFeature(classification, new FeatureScale() { FeatureId = (int)feature.GetFID(), Size = size });

				feature = layer.GetNextFeature();
			}

			columnScale.CalculateProbabilities();

			foreach ((string classification, ColumnValueScale columnValueScale) in columnScale.ColumnValueScales)
			{
				Console.WriteLine(
					$"Classification {classification} has an aggregated scale limit of {columnValueScale.ProbabilityLimit,8:P}.");
			}

			return columnScale;
		}
	}
}