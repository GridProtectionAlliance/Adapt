// ******************************************************************************************************
//  EventSignalReader.tsx - Gbtc
//
//  Copyright © 2022, Grid Protection Alliance.  All Rights Reserved.
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
    /// Reads an Adapt Event Signal from temporary Files for reduced RAM requirement
    /// </summary>
    public class EventSignalReader
    {
        #region [ Internal Classes ]

        #endregion

        #region [ Members ]

        private string m_rootFolder;
        private string m_guid;

        private List<string> m_parameters;

        private const int NLevels = 5;


        #endregion

        #region [ Constructor ]

        /// <summary>
        /// This generates a new <see cref="EventSignalReader"/> .
        /// </summary>
        /// <param name="RootFolder">The root Folder with all the Data.</param>
        /// <param name="SignalGuid">The Guid of the associated Signal</param>
        public EventSignalReader(string RootFolder, string SignalGuid, List<string> Parameters)
        {
            m_rootFolder = RootFolder;
            m_guid = SignalGuid;
            m_parameters = Parameters;
        }

        #endregion

        #region [ Properties]

        #endregion

        #region [ Methods ]
        public EventSummary GetEventSummary(DateTime start, DateTime end)
        {
            List<EventSummary> point = GetSummaryPoints(m_rootFolder, 0, 0, start, end);
            if (point.Count == 0)
                return new EventSummary()
                {
                    Sum = 0,
                    Count = 0,
                    Continuation = false,
                    Min = 0,
                    Max = 0
                };

            return new EventSummary()
            {
                Sum = point.Sum(p => p.Sum),
                Count = point.Sum(p => p.Count),
                Continuation = false,
                Min = point.Min(p => p.Min),
                Max = point.Max(p => p.Max),
            };
        }

        public IEnumerable<AdaptEvent> GetEvents(DateTime start, DateTime end)
        {
            return ReadAllPoints(m_rootFolder, 0, start, end);
        }

        private List<AdaptEvent> ReadAllPoints(string root, int currentLevel, DateTime start, DateTime end)
        {
            List<AdaptEvent> results = new List<AdaptEvent>();

            if (currentLevel == NLevels)
            {
                foreach (string file in Directory.GetFiles(root, "*.bin").OrderBy(item => item))
                {
                    byte[] data = File.ReadAllBytes(file);
                    EventSummary pt = new EventSummary(data);

                    if (pt.Tmin > end)
                        continue;
                    if (pt.Tmax < start)
                        continue;
                    results.AddRange(GetPoints(file,start, end));
                  
                }
                return results;
            }

            int nextLevel = currentLevel + 1;


            foreach (string folder in Directory.GetDirectories(root).OrderBy(item => item))
            {
                byte[] data = File.ReadAllBytes(folder + Path.DirectorySeparatorChar + "summary.node");
                EventSummary pt = new EventSummary(data);

                if (pt.Tmin > end)
                    continue;
                if (pt.Tmax < start)
                    continue;

                results.AddRange(ReadAllPoints(folder, nextLevel, start, end));
            }

            return results;

        }
        private List<EventSummary> GetSummaryPoints(string root, int depth, int currentLevel, DateTime start, DateTime end)
        {
            List<EventSummary> results = new List<EventSummary>();

            // If we grab Summary points from .bin files (1 second of Data)
            if (currentLevel == NLevels)
            {
                foreach (string file in Directory.GetFiles(root, "*.bin").OrderBy(item => item))
                {
                    byte[] data = File.ReadAllBytes(file);
                    EventSummary pt = new EventSummary(data);

                    if (pt.Tmin > end)
                        continue;
                    if (pt.Tmax < start)
                        continue;

                    if ((pt.Tmin >= start && pt.Tmax <= end) && depth == NLevels)
                        results.Add(pt);
                    else if (depth == NLevels)
                        results.Add(Aggregate(GetSummaryPoints(file, NLevels + 1, NLevels + 1, start, end)));
                    else
                        results.AddRange(GetSummaryPoints(file, depth, currentLevel + 1, start, end));
                }
                return results;
            }
            //If we grab actual points from .bin File
            if (currentLevel > NLevels)
            {
                return GetPoints(root, start, end).Select(item => new EventSummary() { 
                    Max=item.Value, 
                    Min = item.Value,
                    Count=1,
                    Sum=item.Value,
                    Tmax=item.Timestamp,
                    Tmin= item.Timestamp,
                    Continuation=false
                }).ToList();
            }

            // if we grab summary points from .summary files
            int nextLevel = currentLevel + 1;

            foreach (string folder in Directory.GetDirectories(root).OrderBy(item => item))
            {
                byte[] data = File.ReadAllBytes(folder + Path.DirectorySeparatorChar + "summary.node");
                EventSummary pt = new EventSummary(data);

                if (pt.Tmin > end)
                    continue;
                if (pt.Tmax < start)
                    continue;

                if ((pt.Tmin >= start && pt.Tmax <= end) && currentLevel == depth)
                    results.Add(pt);
                else if (currentLevel >= depth)
                    results.Add(Aggregate(GetSummaryPoints(folder, NLevels + 1, nextLevel, start, end)));
                else
                    results.AddRange(GetSummaryPoints(folder, depth, nextLevel, start, end));
            }

            return results;
        }

        private List<AdaptEvent> GetPoints(string root, DateTime start, DateTime end)
        {
            List<AdaptEvent> results = new List<AdaptEvent>(); 

            byte[] data = File.ReadAllBytes(root);
            EventSummary pt = new EventSummary(data);

            if (pt.Tmin > end)
                return new List<AdaptEvent>();
            if (pt.Tmax < start)
                return new List<AdaptEvent>();

            int index = EventSummary.NSize;


            while (index < data.Length)
            {
                long ts = BitConverter.ToInt64(data, index);
                double value = BitConverter.ToDouble(data, index + 8);
                double[] parameters = new double[m_parameters.Count()];

                for (int i= 0; i < parameters.Count(); i++)
                {
                    parameters[i] = BitConverter.ToDouble(data, index + 8 + 8 + i*8);

                }

                Gemstone.Ticks ticks = new Gemstone.Ticks(ts);

                index = index + 8 + 8 + parameters.Count()*8;

                if (ticks.Value < start.Ticks || ticks.Value > end.Ticks)
                    continue;

                AdaptEvent point = new AdaptEvent(m_guid,ticks,value,m_parameters.Select((key,i) => new KeyValuePair<string,double>(key,parameters[i])).ToArray());
                point.Parameters = parameters;

                results.Add(point);
            }

            return results;
        }
        #endregion

        #region [ Static ]
        private EventSummary Aggregate(IEnumerable<EventSummary> points)
        {
            EventSummary pt = new EventSummary();
            pt.Count = points.Sum(item => item.Count);
            pt.Min = points.Min(item => item.Min);
            pt.Max = points.Max(item => item.Max);
            pt.Sum = points.Sum(item => item.Sum);
           
            pt.Tmax = points.Max(item => item.Tmax);
            pt.Tmin = points.Min(item => item.Tmin);

            pt.Continuation = points.Where(item => item.Tmin == pt.Tmin).FirstOrDefault()?.Continuation ?? false;

            return pt;
        }
        #endregion
    }
}
