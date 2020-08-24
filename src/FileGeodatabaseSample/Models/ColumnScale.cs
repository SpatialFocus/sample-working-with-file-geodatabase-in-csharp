// <copyright file="ColumnScale.cs" company="Spatial Focus GmbH">
// Copyright (c) Spatial Focus GmbH. All rights reserved.
// </copyright>

namespace FileGeodatabaseSample.Models
{
	using System.Collections.Generic;
	using System.Linq;

	public class ColumnScale<T>
	{
		public Dictionary<T, ColumnValueScale> ColumnValueScales { get; } = new Dictionary<T, ColumnValueScale>();

		public double TotalSize { get; set; }

		public void AddFeature(T classification, FeatureScale feature)
		{
			if (!ColumnValueScales.ContainsKey(classification))
			{
				ColumnValueScales.Add(classification, new ColumnValueScale());
			}

			ColumnValueScales[classification].Features.Add(feature);
		}

		public void CalculateProbabilities()
		{
			foreach (T key in ColumnValueScales.Keys)
			{
				// First calculate the probabilities per classification (key)
				ColumnValueScales[key].CalculateProbabilities();
			}

			TotalSize = ColumnValueScales.Values.Sum(x => x.ColumnValueSize);
			double limit = 0d;

			foreach (T key in ColumnValueScales.Keys)
			{
				limit += (double)ColumnValueScales[key].ColumnValueSize / TotalSize;
				ColumnValueScales[key].ProbabilityLimit = limit;
			}
		}
	}
}