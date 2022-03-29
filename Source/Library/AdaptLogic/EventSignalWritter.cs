// ******************************************************************************************************
//  EventSignalWritter.tsx - Gbtc
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
//  03/20/2022 - C. Lackner
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
    /// Writes an Adapt Event Signal to temporary Files for reduced RAM requirement
    /// Note that we assume the Data Comes in order. This is very important.
    /// </summary>
    public class EventSignalWritter: ISignalWritter
    {
        #region [ Internal Classes ]
    
        #endregion

        #region [ Members ]

        private string m_rootFolder;
        private string[] m_activeFolder;
        private EventSummary[] m_activeSummary;
        private List<string> m_parameters;
        private long m_spillOver;

        private const int NLevels = 5;
        #endregion

        #region [ Constructor ]

        public EventSignalWritter(string RootFolder, List<string> Parameters)
        {
            m_rootFolder = RootFolder;
            m_parameters = Parameters;

            m_activeFolder = new string[NLevels] { "", "", "", "", "" };
            m_activeSummary = new EventSummary[(NLevels + 1)] { 
                new EventSummary(),
                new EventSummary(),
                new EventSummary(),
                new EventSummary(),
                new EventSummary(),
                new EventSummary()
            };
            m_spillOver = 0;
        }

        #endregion

        #region [ Properties]

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Writes a full Second of Data from 
        /// </summary>
        /// <param name="data"> The Data to be written to the File</param>
        /// <param name="forceIndexGen"> Forces the writer to update the Index Files </param>
        public void WriteSecond(List<ITimeSeriesValue> data, bool forceIndexGen = false)
        {
            if (data.Count == 0)
                return;

            string year = data.FirstOrDefault()?.Timestamp.ToString("yyyy");
            string month = data.FirstOrDefault()?.Timestamp.ToString("MM");
            string day = data.FirstOrDefault()?.Timestamp.ToString("dd");
            string hour = data.FirstOrDefault()?.Timestamp.ToString("HH");
            string minute = data.FirstOrDefault()?.Timestamp.ToString("mm");
            string second = data.FirstOrDefault()?.Timestamp.ToString("ss");

            double min = data.Min(pt => pt.Value);
            double max = data.Max(pt => pt.Value);
            double avg = data.Average(pt => pt.Value);

            // Update Lowest Summaries
            // active Spill will hold the Ticks moving on to the next Window
            double currentSecond = Math.Floor(data.First().Timestamp.ToSeconds());
            long currentSecondTicks = (long)currentSecond * Gemstone.Ticks.PerSecond;
            long activeSpill = data.Where(item => Math.Floor((item.Timestamp + (long)item.Value).ToSeconds()) > currentSecond)
                .Sum(item => item.Timestamp + (long)item.Value - (currentSecondTicks + Gemstone.Ticks.PerSecond));

            m_activeSummary[NLevels].Continuation = m_spillOver > 0;

            m_activeSummary[NLevels].Max = max;
            m_activeSummary[NLevels].Min = min;
            m_activeSummary[NLevels].Count = data.Count;
            m_activeSummary[NLevels].Sum = data.Sum(pt => pt.Value) + m_spillOver - activeSpill;
            m_activeSummary[NLevels].Tmax = new DateTime((currentSecondTicks + Gemstone.Ticks.PerSecond), DateTimeKind.Utc);
            m_activeSummary[NLevels].Tmin = new DateTime(currentSecondTicks, DateTimeKind.Utc);

            List<AdaptEvent> evtData = data.Select(item => processEvent(item)).ToList();
           

            GenerateIndex(5);

            if (string.IsNullOrEmpty(m_activeFolder[0]))
                m_activeFolder[0] = year;

            if (string.IsNullOrEmpty(m_activeFolder[1]))
                m_activeFolder[1] = month;

            if (string.IsNullOrEmpty(m_activeFolder[2]))
                m_activeFolder[2] = day;

            if (string.IsNullOrEmpty(m_activeFolder[3]))
                m_activeFolder[3] = hour;

            if (string.IsNullOrEmpty(m_activeFolder[4]))
                m_activeFolder[4] = minute;

            if (minute != m_activeFolder[4] || forceIndexGen)
               GenerateIndex(4);
            if (hour != m_activeFolder[3] || forceIndexGen)
                GenerateIndex(3);
            if (day != m_activeFolder[2] || forceIndexGen)
                GenerateIndex(2);
            if (month != m_activeFolder[1] || forceIndexGen)
                GenerateIndex(1);
            if (year != m_activeFolder[0] || forceIndexGen)
                GenerateIndex(0);

            m_activeFolder[0] = year;
            m_activeFolder[1] = month;
            m_activeFolder[2] = day;
            m_activeFolder[3] = hour;
            m_activeFolder[4] = minute;


            // File format version 1 is Summary -> Data (Ticks -> Value)
            EventSummary fileSummary = new EventSummary();

            int nSize = EventSummary.NSize + (8 + 8 + 8*m_parameters.Count()) * data.Count;
            byte[] rawData = new byte[nSize];

            fileSummary.Max = max;
            fileSummary.Min = min;
            fileSummary.Count = data.Count;
            fileSummary.Sum = data.Sum(pt => pt.Value) + m_spillOver - activeSpill;
            fileSummary.Continuation = m_spillOver > 0;
            fileSummary.Tmax = new DateTime((currentSecondTicks + Gemstone.Ticks.PerSecond), DateTimeKind.Utc);
            fileSummary.Tmin = new DateTime(currentSecondTicks, DateTimeKind.Utc);


            m_spillOver = activeSpill;

            fileSummary.ToByte().CopyTo(rawData, 0);

            int j = EventSummary.NSize;

            for (int i = 0; i < data.Count; i++)
            {
                BitConverter.GetBytes(data[i].Timestamp.Value).CopyTo(rawData, j);
                BitConverter.GetBytes(data[i].Value).CopyTo(rawData, j + 8);
                for (int k = 0; k < m_parameters.Count(); k++)
                {
                    BitConverter.GetBytes(evtData[i].Parameters[j]).CopyTo(rawData, j + 16+k*8);
                }
                j = j + 16 + m_parameters.Count*8;
            }

            //Generate Folder if required

            string folder = $"{m_rootFolder}{Path.DirectorySeparatorChar}{string.Join(Path.DirectorySeparatorChar,m_activeFolder)}";
            Directory.CreateDirectory(folder);
         
            using (BinaryWriter writer = new BinaryWriter(File.OpenWrite($"{folder}{Path.DirectorySeparatorChar}{second}.bin")))
            {  // Writer raw data                
                writer.Write(rawData);
                writer.Flush();
                writer.Close();
            }
        }

        private AdaptEvent processEvent(ITimeSeriesValue val)
        {
            try
            {
                return (AdaptEvent)val;
            }
            catch (Exception ex)
            {
                return new AdaptEvent(val.ID, val.Timestamp, val.Value, m_parameters) 
                { 
                    Parameters  = m_parameters.Select(item => double.NaN).ToArray()
                };
            }

        }
        private void GenerateIndex(int activeFolderIndex)
        {

            if (activeFolderIndex > 0)
                updateIndexSummary(activeFolderIndex);

            if (activeFolderIndex == NLevels)
                return;

            string path = $"{m_rootFolder}{Path.DirectorySeparatorChar}{string.Join(Path.DirectorySeparatorChar, m_activeFolder.Take(activeFolderIndex + 1))}";

            
            if (m_activeSummary[activeFolderIndex].Count > 0)
            {
                Directory.CreateDirectory(path);
                //write to file
                using (BinaryWriter writer = new BinaryWriter(File.OpenWrite($"{path}{Path.DirectorySeparatorChar}summary.node")))
                {
                    writer.Write(m_activeSummary[activeFolderIndex].ToByte());
                    writer.Flush();
                    writer.Close();
                }
            }
            
            // reset lower level
            m_activeSummary[activeFolderIndex] = new EventSummary();

        }

        private void updateIndexSummary(int activeFolderIndex)
        {
            if (double.IsNaN(m_activeSummary[activeFolderIndex - 1].Min) || m_activeSummary[activeFolderIndex].Min < m_activeSummary[activeFolderIndex - 1].Min)
                m_activeSummary[activeFolderIndex - 1].Min = m_activeSummary[activeFolderIndex].Min;
            if (double.IsNaN(m_activeSummary[activeFolderIndex - 1].Max) || m_activeSummary[activeFolderIndex].Max > m_activeSummary[activeFolderIndex - 1].Max)
                m_activeSummary[activeFolderIndex - 1].Max = m_activeSummary[activeFolderIndex].Max;

            if (double.IsNaN(m_activeSummary[activeFolderIndex - 1].Sum))
                m_activeSummary[activeFolderIndex - 1].Sum = 0.0D;

            if (double.IsNaN(m_activeSummary[activeFolderIndex - 1].Count))
                m_activeSummary[activeFolderIndex - 1].Count = 0;




            if (m_activeSummary[activeFolderIndex].Tmin < m_activeSummary[activeFolderIndex - 1].Tmin)
                m_activeSummary[activeFolderIndex - 1].Tmin = m_activeSummary[activeFolderIndex].Tmin;

            if (m_activeSummary[activeFolderIndex].Tmax > m_activeSummary[activeFolderIndex - 1].Tmax)
                m_activeSummary[activeFolderIndex - 1].Tmax = m_activeSummary[activeFolderIndex].Tmax;

            m_activeSummary[activeFolderIndex - 1].Count += m_activeSummary[activeFolderIndex].Count;
            m_activeSummary[activeFolderIndex - 1].Sum += m_activeSummary[activeFolderIndex].Sum;

        }
        #endregion

        #region [ Static ]

        #endregion
    }
}
