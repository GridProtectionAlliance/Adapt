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
        private string m_lastFile;
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
            m_lastFile = "";
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
        /// <summary>
        /// Logs Messages and exceptions
        /// </summary>
        public event EventHandler<MessageArgs> MessageRecieved;
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


            // If there is spillover and this is not the next second, the additional Files need to be written
            if (m_spillOver > 0)
            {
                DateTime currentTime = new DateTime(currentSecondTicks, DateTimeKind.Utc);
                DateTime writeTime = m_activeSummary[NLevels].Tmax;
                while (writeTime < currentTime && m_spillOver > 0)
                {

                    // write a new File....
                    m_activeSummary[NLevels].Continuation = true;
                    m_activeSummary[NLevels].Max = double.NaN;
                    m_activeSummary[NLevels].Min = double.NaN;
                    m_activeSummary[NLevels].Count = 0;
                    m_activeSummary[NLevels].Sum = Math.Min(m_spillOver, Gemstone.Ticks.PerSecond);
                    m_activeSummary[NLevels].Tmin = new DateTime(writeTime.Ticks);
                    m_activeSummary[NLevels].Tmax = new DateTime(writeTime.Ticks + Gemstone.Ticks.PerSecond, DateTimeKind.Utc);
                    m_spillOver -= Math.Min(m_spillOver, Gemstone.Ticks.PerSecond);

                    string sec = writeTime.ToString("ss");
                   

                    // Generate Summary Files for previous dataset and reset any summary we write

                    if (!CheckSummaryEqual(writeTime.ToString("yyyy"), writeTime.ToString("MM"), writeTime.ToString("dd"), writeTime.ToString("HH"), writeTime.ToString("mm")))
                    {
                        updateIndexSummary(4);
                        GenerateIndex(4);
                    }
                    if (!CheckSummaryEqual(writeTime.ToString("yyyy"), writeTime.ToString("MM"), writeTime.ToString("dd"), writeTime.ToString("HH")))
                    {
                        updateIndexSummary(3);
                        GenerateIndex(3);
                    }
                    if (!CheckSummaryEqual(writeTime.ToString("yyyy"), writeTime.ToString("MM"), writeTime.ToString("dd")))
                    {
                        updateIndexSummary(2);
                        GenerateIndex(2);
                    }
                    if (!CheckSummaryEqual(writeTime.ToString("yyyy"), writeTime.ToString("MM")))
                    {
                        updateIndexSummary(1);
                        GenerateIndex(1);
                    }
                    if (!CheckSummaryEqual(writeTime.ToString("yyyy")))
                        GenerateIndex(0);

                    m_activeFolder[0] = writeTime.ToString("yyyy");
                    m_activeFolder[1] = writeTime.ToString("MM");
                    m_activeFolder[2] = writeTime.ToString("dd");
                    m_activeFolder[3] = writeTime.ToString("HH");
                    m_activeFolder[4] = writeTime.ToString("mm");

                    int nSummarySize = EventSummary.NSize;
                    byte[] summaryData = new byte[nSummarySize];

                    m_activeSummary[NLevels].ToByte().CopyTo(summaryData, 0);

                    string summaryFolder = $"{m_rootFolder}{Path.DirectorySeparatorChar}{string.Join(Path.DirectorySeparatorChar, m_activeFolder)}";
                    Directory.CreateDirectory(summaryFolder);

                    using (BinaryWriter writer = new BinaryWriter(File.OpenWrite($"{summaryFolder}{Path.DirectorySeparatorChar}{sec}.bin")))
                    {  // Writer raw data                
                        writer.Write(summaryData);
                        writer.Flush();
                        writer.Close();
                    }
                    updateIndexSummary(5);
                    writeTime = new DateTime(writeTime.Ticks + Gemstone.Ticks.PerSecond, DateTimeKind.Utc);
                }
            }

            List<AdaptEvent> evtData = data.Select(item => processEvent(item)).ToList();

            bool isInitial = m_activeFolder.Any(s => string.IsNullOrEmpty(s));

            if (isInitial)
            {
                m_activeFolder[0] = year;
                m_activeFolder[1] = month;
                m_activeFolder[2] = day;
                m_activeFolder[3] = hour;
                m_activeFolder[4] = minute;
                m_lastFile = second;
            }
            else
            {

                // Generate Summary Files for previous dataset and reset any summary we write
                if (!CheckSummaryEqual(year, month, day, hour, minute))
                {
                    updateIndexSummary(4);
                    GenerateIndex(4);
                }
                if (!CheckSummaryEqual(year, month, day, hour))
                {
                    updateIndexSummary(3);
                    GenerateIndex(3);
                }
                if (!CheckSummaryEqual(year, month, day))
                {
                    updateIndexSummary(2);
                    GenerateIndex(2);
                }
                if (!CheckSummaryEqual(year, month))
                {
                    updateIndexSummary(1);
                    GenerateIndex(1);
                }
                if (!CheckSummaryEqual(year))
                    GenerateIndex(0);
            }


            

            m_activeSummary[NLevels].Continuation = m_spillOver > 0;
            m_activeSummary[NLevels].Max = max;
            m_activeSummary[NLevels].Min = min;
            m_activeSummary[NLevels].Count = data.Count;
            m_activeSummary[NLevels].Sum = data.Sum(pt => pt.Value) + m_spillOver - activeSpill;
            m_activeSummary[NLevels].Tmax = new DateTime((currentSecondTicks + Gemstone.Ticks.PerSecond), DateTimeKind.Utc);
            m_activeSummary[NLevels].Tmin = new DateTime(currentSecondTicks, DateTimeKind.Utc);

            m_activeFolder[0] = year;
            m_activeFolder[1] = month;
            m_activeFolder[2] = day;
            m_activeFolder[3] = hour;
            m_activeFolder[4] = minute;
            m_lastFile = second;

            updateIndexSummary(5);
            // Special case for forced Index Gen  needs to be run twice to ensure all levels have summary files
            if (forceIndexGen)
            {
                // at this point we need to write any leftover spill
                DateTime writeTime = m_activeSummary[NLevels].Tmax;
                while (activeSpill > 0)
                {
                    m_activeSummary[NLevels].Continuation = true;
                    m_activeSummary[NLevels].Max = double.NaN;
                    m_activeSummary[NLevels].Min = double.NaN;
                    m_activeSummary[NLevels].Count = 0;
                    m_activeSummary[NLevels].Sum = Math.Min(activeSpill, Gemstone.Ticks.PerSecond);
                    m_activeSummary[NLevels].Tmin = new DateTime(writeTime.Ticks);
                    m_activeSummary[NLevels].Tmax = new DateTime(writeTime.Ticks + Gemstone.Ticks.PerSecond, DateTimeKind.Utc);
                    activeSpill -= Math.Min(activeSpill, Gemstone.Ticks.PerSecond);

                    string sec = writeTime.ToString("ss");


                    // Generate Summary Files for previous dataset and reset any summary we write

                    if (!CheckSummaryEqual(writeTime.ToString("yyyy"), writeTime.ToString("MM"), writeTime.ToString("dd"), writeTime.ToString("HH"), writeTime.ToString("mm")))
                    {
                        updateIndexSummary(4);
                        GenerateIndex(4);
                    }
                    if (!CheckSummaryEqual(writeTime.ToString("yyyy"), writeTime.ToString("MM"), writeTime.ToString("dd"), writeTime.ToString("HH")))
                    {
                        updateIndexSummary(3);
                        GenerateIndex(3);
                    }
                    if (!CheckSummaryEqual(writeTime.ToString("yyyy"), writeTime.ToString("MM"), writeTime.ToString("dd")))
                    {
                        updateIndexSummary(2);
                        GenerateIndex(2);
                    }
                    if (!CheckSummaryEqual(writeTime.ToString("yyyy"), writeTime.ToString("MM")))
                    {
                        updateIndexSummary(1);
                        GenerateIndex(1);
                    }
                    if (!CheckSummaryEqual(writeTime.ToString("yyyy")))
                        GenerateIndex(0);

                    m_activeFolder[0] = writeTime.ToString("yyyy");
                    m_activeFolder[1] = writeTime.ToString("MM");
                    m_activeFolder[2] = writeTime.ToString("dd");
                    m_activeFolder[3] = writeTime.ToString("HH");
                    m_activeFolder[4] = writeTime.ToString("mm");

                    int nSummarySize = EventSummary.NSize;
                    byte[] summaryData = new byte[nSummarySize];

                    m_activeSummary[NLevels].ToByte().CopyTo(summaryData, 0);

                    string summaryFolder = $"{m_rootFolder}{Path.DirectorySeparatorChar}{string.Join(Path.DirectorySeparatorChar, m_activeFolder)}";
                    Directory.CreateDirectory(summaryFolder);

                    using (BinaryWriter writer = new BinaryWriter(File.OpenWrite($"{summaryFolder}{Path.DirectorySeparatorChar}{sec}.bin")))
                    {  // Writer raw data                
                        writer.Write(summaryData);
                        writer.Flush();
                        writer.Close();
                    }
                    updateIndexSummary(5);
                    writeTime = new DateTime(writeTime.Ticks + Gemstone.Ticks.PerSecond, DateTimeKind.Utc);
                }

                string path;
                for (int i=(NLevels-1); i >=0; i--)
                {
                    path = $"{m_rootFolder}{Path.DirectorySeparatorChar}{string.Join(Path.DirectorySeparatorChar, m_activeFolder.Take(i+1))}";
                    if (i != 0)
                        updateIndexSummary(i);
                    WriteActiveSummary(m_activeSummary[i], path);
                }
            }

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
                    BitConverter.GetBytes(evtData[i][m_parameters[k]]).CopyTo(rawData, j + 16 + k*8);
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
                return new AdaptEvent(val.ID, val.Timestamp, val.Value);
            }

        }

        /// <summary>
        /// Write the Index Files. This is called when either (a) a new Dataset came in [for the previous dataset] (b) if IndexGenForced is true
        /// [to write current Dataset]
        /// </summary>
        /// <param name="activeFolderIndex"></param>
        private void GenerateIndex(int activeFolderIndex)
        {
            if (activeFolderIndex == NLevels)
                return;

            string path = $"{m_rootFolder}{Path.DirectorySeparatorChar}{string.Join(Path.DirectorySeparatorChar, m_activeFolder.Take(activeFolderIndex + 1))}";

            if (m_activeSummary[activeFolderIndex].Sum > 0)
                WriteActiveSummary(m_activeSummary[activeFolderIndex], path);

            m_activeSummary[activeFolderIndex] = new EventSummary();

        }

        private void WriteActiveSummary(EventSummary summary, string folder)
        {
            Directory.CreateDirectory(folder);
            //write to file
            using (BinaryWriter writer = new BinaryWriter(File.OpenWrite($"{folder}{Path.DirectorySeparatorChar}summary.node")))
            {
                writer.Write(summary.ToByte());
                writer.Flush();
                writer.Close();
            }
        }


        /// <summary>
        /// This will update the summary. Called when a new dataset comes in only
        /// </summary>
        /// <param name="activeFolderIndex"></param>
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

        private bool CheckSummaryEqual(string year)
        {
            return year == m_activeFolder[0];
        }
        private bool CheckSummaryEqual(string year, string month)
        {
            return CheckSummaryEqual(year) && month == m_activeFolder[1];
        }
        private bool CheckSummaryEqual(string year, string month, string day)
        {
            return CheckSummaryEqual(year, month) && day == m_activeFolder[2];
        }
        private bool CheckSummaryEqual(string year, string month, string day, string hour)
        {
            return CheckSummaryEqual(year, month, day) && hour == m_activeFolder[3];
        }
        private bool CheckSummaryEqual(string year, string month, string day, string hour, string minute)
        {
            return CheckSummaryEqual(year, month, day, hour) && minute == m_activeFolder[4];
        }
        #endregion

        #region [ Static ]

        #endregion
    }
}
