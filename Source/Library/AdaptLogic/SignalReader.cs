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
//  04/19/2021 - C. Lackner
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
    /// Reads an Adapt Signal from temporary Files for reduced RAM requirement
    /// </summary>
    public class SignalReader: IReader
    {
        #region [ Internal Classes ]

        #endregion

        #region [ Members ]

        private string m_rootFolder;
       

        private const int NLevels = 5;

        /// <summary>
        /// The minimum number of Points to pull.
        /// </summary>
        private const int NPoints = 100;
        #endregion

        #region [ Constructor ]

        /// <summary>
        /// This generates a new <see cref="SignalReader"/> for the GUID specified.
        /// </summary>
        /// <param name="Guid">The Guid associated with the temporary files.</param>
        public SignalReader(string Guid)
        {
            
            ReadSignal(Guid);

        }

        #endregion

        #region [ Properties]
        /// <summary>
        /// Gets the <see cref="AdaptSignal"/> This reader is attached to
        /// </summary>
        public AdaptSignal Signal { get; private set; }

        public string SignalGuid { get; private set; }
        #endregion

        #region [ Methods ]

        private void ReadSignal(string guid)
        {
            SignalGuid = guid;
            m_rootFolder = $"{DataPath}{guid}";
            string name = "";
            string deviceID = "";
            string signalID = guid;
            string description = "";
            Phase phase = Phase.A;
            MeasurementType type = MeasurementType.Analog;
            int framesPerSecond = 30;

            string line;
            // Read .config file that just contain some of the Signal Information

            using (StreamReader reader = new StreamReader($"{m_rootFolder}{Path.DirectorySeparatorChar}Root.config"))
            {
                if ((line = reader.ReadLine()) != null)
                    name = line;
                if ((line = reader.ReadLine()) != null)
                    deviceID = line;
                if ((line = reader.ReadLine()) != null)
                    signalID = line;
                if ((line = reader.ReadLine()) != null)
                    description = line;
                if ((line = reader.ReadLine()) != null)
                    Enum.TryParse<Phase>(line, out phase);
                if ((line = reader.ReadLine()) != null)
                    Enum.TryParse<MeasurementType>(line, out type);
                if ((line = reader.ReadLine()) != null)
                    framesPerSecond = Convert.ToInt32(line);
            }

            Signal = new AdaptSignal(signalID, name, deviceID, framesPerSecond);
            Signal.Description = description;
            Signal.Phase = phase;
            Signal.Type = type;
        }

        /// <summary>
        /// Gets an avg Trend Series of up to 400 points
        /// </summary>
        /// <param name="start">the start Time of the Series.</param>
        /// <param name="end">The end time of the Series.</param>
        /// <param name="points"> The minimum number of points requested.</param>
        /// <returns></returns>
        public IEnumerable<ITimeSeriesValue> GetTrend(DateTime start, DateTime end, int points=NPoints)
        {
            // Estimate how many levels we need to go down to get as many points as required
            double seconds = (end - start).TotalSeconds;
            int nPoints = (int)Math.Floor(seconds * Signal.FramesPerSecond);
            int pointsPerLevel = (int)Signal.FramesPerSecond;
            if (pointsPerLevel == 0)
                pointsPerLevel = 30;
            int requiredLevels = NLevels + 1;

            if (nPoints > (pointsPerLevel* points))
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

            return GetPoints(m_rootFolder, requiredLevels, 0,start, end).Select(item => new AdaptValue(Signal.ID, item.Avg, item.Tmin.Add(item.Tmax - item.Tmin)));
            
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
                return new AdaptPoint(Signal.ID, double.NaN, start.Add(end - start), double.NaN, double.NaN, Signal.FramesPerSecond);

            return new AdaptPoint(Signal.ID, point.Sum(p => p.Sum), point.Sum(p => p.SquaredSum), point.Sum(p => p.N), start, end, point.Min(p => p.Min), point.Max(p => p.Max), Signal.FramesPerSecond);
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
            pt.Min = points.Min(item => item.N);
            pt.Max = points.Max(item => item.N);

            pt.Tmax = points.Max(item => item.Tmax);
            pt.Tmin = points.Min(item => item.Tmin);

            return pt;
        }
        #endregion

        #region [ Static ]

        private static readonly string DataPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}{Path.DirectorySeparatorChar}ADAPT{Path.DirectorySeparatorChar}dataTree{Path.DirectorySeparatorChar}";

        public static List<SignalReader> GetAvailableReader()
        {
            List<SignalReader> readers = new List<SignalReader>();
            foreach (string folder in Directory.GetDirectories(DataPath))
            {
                readers.Add(new SignalReader(new DirectoryInfo(folder).Name));
            }
            return readers;
        }
        #endregion
    }
}
