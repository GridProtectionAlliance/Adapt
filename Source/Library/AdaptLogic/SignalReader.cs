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

        private bool m_isEvent;

        private DataSignalReader m_dataReader;

        private EventSignalReader m_eventReader;

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

        public List<string> EventParameters { get; private set; }
        #endregion

        #region [ Methods ]

        private void ReadSignal(string guid)
        {
            SignalGuid = guid;
            EventParameters = new List<string>();
            m_rootFolder = $"{DataPath}{guid}";

            m_isEvent = false;

            string name = "";
            string deviceID = "";
            string signalID = guid;
            string description = "";
            Phase phase = Phase.A;
            MeasurementType type = MeasurementType.Analog;
            int framesPerSecond = 30;

            string line;

            // Read .config file that just contain some of the Signal Information;
            // Note that this may contain Event Information too.

            using (StreamReader reader = new StreamReader($"{m_rootFolder}{Path.DirectorySeparatorChar}Root.config"))
            {
                if ((line = reader.ReadLine()) != null)
                {
                    if (line == "Event Data Format")
                    {
                        m_isEvent = true;
                        line = reader.ReadLine();
                    }
                    name = line;
                }
                    
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

                if (m_isEvent && (line = reader.ReadLine()) != null)
                {
                    while ((line = reader.ReadLine()) != null)
                        EventParameters.Add(line);
                }
            }

            Signal = new AdaptSignal(signalID, name, deviceID, framesPerSecond);
            Signal.Description = description;
            Signal.Phase = phase;
            Signal.Type = type;

            if (!m_isEvent)
                m_dataReader = new DataSignalReader(m_rootFolder, SignalGuid, framesPerSecond);
            else
                m_eventReader = new EventSignalReader(m_rootFolder, SignalGuid, EventParameters);
        }

        /// <summary>
        /// Gets an avg Trend Series of up to 400 points
        /// </summary>
        /// <param name="start">the start Time of the Series.</param>
        /// <param name="end">The end time of the Series.</param>
        /// <returns></returns>
        public IEnumerable<ITimeSeriesValue> GetTrend(DateTime start, DateTime end)
        {
            if (m_isEvent)
                return new List<ITimeSeriesValue>();
            else
                return m_dataReader.GetTrend(start, end);
        }

        /// <summary>
        /// Gets an avg Trend Series of up to 400 points
        /// </summary>
        /// <param name="start">the start Time of the Series.</param>
        /// <param name="end">The end time of the Series.</param>
        /// <param name="points"> The minimum number of points requested.</param>
        /// <returns></returns>
        public IEnumerable<ITimeSeriesValue> GetTrend(DateTime start, DateTime end, int points)
        {
            if (m_isEvent)
                return new List<ITimeSeriesValue>();
            else
                return m_dataReader.GetTrend(start, end, points);
        }

        /// <summary>
        /// Gets an avg Trend Series of up to 400 points
        /// </summary>
        /// <param name="start">the start Time of the Series.</param>
        /// <param name="end">The end time of the Series.</param>
        /// <returns></returns>
        public IEnumerable<GraphPoint> GetRangeTrend(DateTime start, DateTime end)
        {
            if (m_isEvent)
                return new List<GraphPoint>();
            else
                return m_dataReader.GetRangeTrend(start, end);
        }

        /// <summary>
        /// Gets an avg Trend Series of up to 400 points
        /// </summary>
        /// <param name="start">the start Time of the Series.</param>
        /// <param name="end">The end time of the Series.</param>
        /// <param name="points"> The minimum number of points requested.</param>
        /// <returns></returns>
        public IEnumerable<GraphPoint> GetRangeTrend(DateTime start, DateTime end, int points)
        {
            if (m_isEvent)
                return new List<GraphPoint>();
            else
                return m_dataReader.GetRangeTrend(start, end, points);
        }

        /// <summary>
        /// Gets the Summary of this SIgnal for a specified Time range.
        /// </summary>
        /// <param name="start">The startTime.</param>
        /// <param name="end">The endTime.</param>
        /// <returns>An <see cref="AdaptPoint"/> specifying Min, Max and Avg</returns>
        public AdaptPoint GetStatistics(DateTime start, DateTime end)
        {
            if (m_isEvent)
                return new AdaptPoint(SignalGuid);
            else
                return m_dataReader.GetStatistics(start, end);
        }

        public EventSummary GetEventSummary(DateTime start, DateTime end)
        {
            if (!m_isEvent)
                return new EventSummary();
            else
                return m_eventReader.GetEventSummary(start, end);
        }

        public IEnumerable<AdaptEvent> GetEvents(DateTime start, DateTime end)
        {
            if (!m_isEvent)
                return new List<AdaptEvent>();
            else
                return m_eventReader.GetEvents(start, end);
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
