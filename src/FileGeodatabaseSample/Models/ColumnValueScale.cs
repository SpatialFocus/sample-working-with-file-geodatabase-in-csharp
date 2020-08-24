// <copyright file="ColumnValueScale.cs" company="Spatial Focus GmbH">
// Copyright (c) Spatial Focus GmbH. All rights reserved.
// </copyright>

namespace FileGeodatabaseSample.Models
{
	using System.Collections.Generic;
	using System.Linq;

	public class ColumnValueScale
	{
		public double ColumnValueSize { get; set; }

		public List<FeatureScale> Features { get; } = new List<FeatureScale>();

		public double ProbabilityLimit { get; set; }

		public void CalculateProbabilities()
		{
			ColumnValueSize = Features.Sum(x => x.Size);
			double limit = 0d;

			foreach (FeatureScale val in Features)
			{
				limit += val.Size / ColumnValueSize;
				val.ProbabilityLimit = limit;
			}
		}
	}
}