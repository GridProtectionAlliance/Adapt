// ******************************************************************************************************
//  SignalReader.tsx - Gbtc
//
//  Copyright © 2021, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may not use this
//  file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  03/25/2022 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************

using Adapt.Models;
using GemstoneCommon;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AdaptLogic
{
    /// <summary>
    /// Reads an Adapt Data Signal from temporary Files for reduced RAM requirement
    /// </summary>
    public class DataSignalReader
    {
        #region [ Internal Classes ]

        #endregion

        #region [ Members ]

        private string m_rootFolder;
        private string m_guid;

        private int m_framesPerSecond;
        private const int NLevels = 5;

        /// <summary>
        /// The minimum number of Points to pull.
        /// </summary>
        private const int NPoints = 100;
        #endregion

        #region [ Constructor ]

        /// <summary>
        /// This generates a new <see cref="DataSignalReader"/> .
        /// </summary>
        /// <param name="RootFolder">The root Folder with all the Data.</param>
        /// <param name="SignalGuid">The Guid of the associated Signal</param>
        public DataSignalReader(string RootFolder, string SignalGuid, int FramesPerSecond)
        {
            m_rootFolder = RootFolder;
            m_guid = SignalGuid;
            m_framesPerSecond = FramesPerSecond;
        }

        #endregion

        #region [ Properties]
        
        #endregion

        #region [ Methods ]

        /// <summary>
        /// Gets an avg Trend Series of up to 400 points
        /// </summary>
        /// <param name="start">the start Time of the Series.</param>
        /// <param name="end">The end time of the Series.</param>
        /// <param name="points"> The minimum number of points requested.</param>
        /// <returns></returns>
        public IEnumerable<ITimeSeriesValue> GetTrend(DateTime start, DateTime end, int points=NPoints)
        {
            return GetRangeTrend(start, end, points).Select(item => new AdaptValue(m_guid, item.Avg, item.Tmin.Add(item.Tmax - item.Tmin)));
        }

        /// <summary>
        /// Gets an avg Trend Series of up to 400 points
        /// </summary>
        /// <param name="start">the start Time of the Series.</param>
        /// <param name="end">The end time of the Series.</param>
        /// <param name="points"> The minimum number of points requested.</param>
        /// <returns></returns>
        public IEnumerable<GraphPoint> GetRangeTrend(DateTime start, DateTime end, int points = NPoints)
        {
            // Estimate how many levels we need to go down to get as many points as required
            double seconds = (end - start).TotalSeconds;
            int nPoints = (int)Math.Floor(seconds * m_framesPerSecond);
            int pointsPerLevel = (int)m_framesPerSecond;
            if (pointsPerLevel == 0)
                pointsPerLevel = 30;
            int requiredLevels = NLevels + 1;

            if (nPoints > (pointsPerLevel * points))
                requiredLevels--;

            nPoints = (int)Math.Floor(seconds);
            pointsPerLevel = 60;
            if (nPoints > (pointsPerLevel * points))
                requiredLevels--;

            nPoints = (int)Math.Floor((end - start).TotalMinutes);
            pointsPerLevel = 60;
            if (nPoints > (pointsPerLevel * points))
                requiredLevels--;

            nPoints = (int)Math.Floor((end - start).TotalHours);
            pointsPerLevel = 24;
            if (nPoints > (pointsPerLevel * points))
                requiredLevels--;

            nPoints = (int)Math.Floor((end - start).TotalDays);
            pointsPerLevel = 30;
            if (nPoints > (pointsPerLevel * points))
                requiredLevels--;

            nPoints = (int)Math.Floor((end - start).TotalDays / 30.0);
            pointsPerLevel = 12;
            if (nPoints > (pointsPerLevel * points))
                requiredLevels--;

            if (points == 0)
                requiredLevels = NLevels + 1;

            return GetPoints(m_rootFolder, requiredLevels, 0, start, end);

        }

        /// <summary>
        /// Gets the Summary of this SIgnal for a specified Time range.
        /// </summary>
        /// <param name="start">The startTime.</param>
        /// <param name="end">The endTime.</param>
        /// <returns>An <see cref="AdaptPoint"/> specifying Min, Max and Avg</returns>
        public AdaptPoint GetStatistics(DateTime start, DateTime end)
        {
            List<GraphPoint> point = GetPoints(m_rootFolder, 0, 0, start, end);
            if (point.Count == 0)
                return new AdaptPoint(m_guid, double.NaN, start.Add(end - start), double.NaN, double.NaN, m_framesPerSecond);

            return new AdaptPoint(m_guid, point.Sum(p => p.Sum), point.Sum(p => p.SquaredSum), point.Sum(p => p.N), start, end, point.Min(p => p.Min), point.Max(p => p.Max), m_framesPerSecond);
        }

        private List<GraphPoint> GetPoints(string root, int depth, int currentLevel, DateTime start, DateTime end)
        {
            List< GraphPoint> results = new List<GraphPoint>();

            // If we grab Summary points from .bin files (1 second of Data)
            if (currentLevel == NLevels)
            {
                foreach (string file in Directory.GetFiles(root, "*.bin").OrderBy(item => item))
                {
                    byte[] data = File.ReadAllBytes(file);
                    GraphPoint pt = new GraphPoint(data);

                    if (pt.Tmin > end)
                        continue;
                    if (pt.Tmax < start)
                        continue;

                    if ((pt.Tmin >= start && pt.Tmax <= end) && depth == NLevels)
                        results.Add(pt);
                    else if (depth == NLevels)
                        results.Add(Aggregate(GetPoints(file, NLevels +1, NLevels + 1, start, end)));
                    else
                        results.AddRange(GetPoints(file, depth, currentLevel + 1, start, end));
                }
                return results;
            }
            //If we grab actual points from .bin File
            if (currentLevel > NLevels)
            {
                
                byte[] data = File.ReadAllBytes(root);
                GraphPoint pt = new GraphPoint(data);

                if (pt.Tmin > end)
                    return new List<GraphPoint>();
                if (pt.Tmax < start)
                    return new List<GraphPoint>();

                int index = GraphPoint.NSize;

                
                while (index < data.Length )
                {
                    long ts = BitConverter.ToInt64(data, index);
                    double value = BitConverter.ToDouble(data, index + 8);
                    Gemstone.Ticks ticks = new Gemstone.Ticks(ts);

                    index = index + 8 + 8;

                    if (ticks.Value < start.Ticks || ticks.Value > end.Ticks)
                        continue;
                    
                    GraphPoint point = new GraphPoint();
                    point.N = 1;
                    point.Max = value;
                    point.Min = value;
                    point.Sum = value;
                    point.SquaredSum = value * value;
                    point.Tmax = ticks;
                    point.Tmin = ticks;

                    results.Add(point);
                }

                return results;
            }

            // if we grab summary points from .summary files
            int nextLevel = currentLevel + 1;

            foreach (string folder in Directory.GetDirectories(root).OrderBy(item => item))
            {
                byte[] data = File.ReadAllBytes(folder + Path.DirectorySeparatorChar + "summary.node");
                GraphPoint pt = new GraphPoint(data);

                if (pt.Tmin > end)
                    continue;
                if (pt.Tmax < start)
                    continue;

                if ((pt.Tmin >= start && pt.Tmax <= end)  && currentLevel == depth)
                    results.Add(pt);
                else if (currentLevel >= depth)
                    results.Add( Aggregate(GetPoints(folder, NLevels+1, nextLevel, start, end)));
                else 
                    results.AddRange(GetPoints(folder, depth, nextLevel, start, end));
            }

            return results;
        }

        private GraphPoint Aggregate( IEnumerable<GraphPoint> points)
        {
            GraphPoint pt = new GraphPoint();
            pt.N = points.Sum(item => item.N);
            pt.Min = points.Min(item => item.Min);
            pt.Max = points.Max(item => item.Max);
            pt.Sum = points.Sum(item => item.Sum);
            pt.SquaredSum = points.Sum(item => item.SquaredSum);
            pt.Tmax = points.Max(item => item.Tmax);
            pt.Tmin = points.Min(item => item.Tmin);

            return pt;
        }
        #endregion

        #region [ Static ]

        #endregion
    }
}
