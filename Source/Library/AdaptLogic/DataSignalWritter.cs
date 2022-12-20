// ******************************************************************************************************
//  DataSignalWritter.tsx - Gbtc
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
    /// Writes an Adapt Data Signal to temporary Files for reduced RAM requirement
    /// Note that we assume the Data Comes in order. This is very important.
    /// </summary>
    public class DataSignalWritter: ISignalWritter
    {
        #region [ Internal Classes ]

        #endregion

        #region [ Members ]

        private string m_rootFolder;
        private string[] m_activeFolder;
        private GraphPoint[] m_activeSummary;

        private const int NLevels = 5;
        #endregion

        #region [ Constructor ]

        public DataSignalWritter(string rootFolder)
        {
            m_rootFolder = rootFolder;
            
            m_activeFolder = new string[NLevels] { "", "", "", "", "" };
            m_activeSummary = new GraphPoint[(NLevels + 1)] { new GraphPoint(), new GraphPoint(), new GraphPoint(), new GraphPoint(), new GraphPoint(), new GraphPoint() };
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
        /// <param name="forceIndexGen"></param>
        public void WriteSecond(List<ITimeSeriesValue> data, bool forceIndexGen=false)
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

            m_activeSummary[NLevels].Max = max;
            m_activeSummary[NLevels].Min = min;
            m_activeSummary[NLevels].N =  data.Count;
            m_activeSummary[NLevels].Sum = data.Sum(pt => pt.Value);
            m_activeSummary[NLevels].SquaredSum = data.Sum(pt => pt.Value * pt.Value);
            m_activeSummary[NLevels].Tmax = new DateTime(data.Max(item => item.Timestamp), DateTimeKind.Utc);
            m_activeSummary[NLevels].Tmin = new DateTime(data.Min(item => item.Timestamp), DateTimeKind.Utc);

            bool isInitial = m_activeFolder.Any(s => string.IsNullOrEmpty(s));

            if (isInitial)
            {
                m_activeFolder[0] = year;
                m_activeFolder[1] = month;
                m_activeFolder[2] = day;
                m_activeFolder[3] = hour;
                m_activeFolder[4] = minute;
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

            m_activeFolder[0] = year;
            m_activeFolder[1] = month;
            m_activeFolder[2] = day;
            m_activeFolder[3] = hour;
            m_activeFolder[4] = minute;
            updateIndexSummary(5);

            if (forceIndexGen)
            {
                string path;
                for (int i = (NLevels - 1); i >= 0; i--)
                {
                    path = $"{m_rootFolder}{Path.DirectorySeparatorChar}{string.Join(Path.DirectorySeparatorChar, m_activeFolder.Take(i + 1))}";
                    if (i != 0)
                        updateIndexSummary(i);
                    WriteActiveSummary(m_activeSummary[i], path);
                }
            }

            int nSize = GraphPoint.NSize + (8 + 8) * data.Count;
            byte[] rawData = new byte[nSize];

            // File format version 1 is GraphPoint -> Data (Ticks -> Value)
            GraphPoint fileSummary = new GraphPoint();
            fileSummary.N = data.Count();
            fileSummary.Sum = data.Sum(pt => pt.Value);
            fileSummary.SquaredSum = data.Sum(pt => pt.Value* pt.Value);
            fileSummary.Min = min;
            fileSummary.Max = max;
            fileSummary.Tmax = new DateTime(data.Max(item => item.Timestamp),DateTimeKind.Utc);
            fileSummary.Tmin = new DateTime(data.Min(item => item.Timestamp), DateTimeKind.Utc);

            fileSummary.ToByte().CopyTo(rawData, 0);


            int j = GraphPoint.NSize;

            for (int i = 0; i < data.Count; i++)
            {
                BitConverter.GetBytes(data[i].Timestamp.Value).CopyTo(rawData, j);
                BitConverter.GetBytes(data[i].Value).CopyTo(rawData, j + 8);
                j = j + 16;
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

        private void GenerateIndex(int activeFolderIndex)
        {

            if (activeFolderIndex > 0)
                updateIndexSummary(activeFolderIndex);

            if (activeFolderIndex == NLevels)
                return;

            string path = $"{m_rootFolder}{Path.DirectorySeparatorChar}{string.Join(Path.DirectorySeparatorChar, m_activeFolder.Take(activeFolderIndex + 1))}";


            if (m_activeSummary[activeFolderIndex].N > 0)
                WriteActiveSummary(m_activeSummary[activeFolderIndex], path);

            // reset lower level
            m_activeSummary[activeFolderIndex] = new GraphPoint();

        }

        private void WriteActiveSummary(GraphPoint summary, string folder)
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

        private void updateIndexSummary(int activeFolderIndex)
        {
            if (double.IsNaN(m_activeSummary[activeFolderIndex - 1].Min) || m_activeSummary[activeFolderIndex].Min < m_activeSummary[activeFolderIndex - 1].Min)
                m_activeSummary[activeFolderIndex - 1].Min = m_activeSummary[activeFolderIndex].Min;
            if (double.IsNaN(m_activeSummary[activeFolderIndex - 1].Max) || m_activeSummary[activeFolderIndex].Max > m_activeSummary[activeFolderIndex - 1].Max)
                m_activeSummary[activeFolderIndex - 1].Max = m_activeSummary[activeFolderIndex].Max;

            if (double.IsNaN(m_activeSummary[activeFolderIndex - 1].Sum))
                m_activeSummary[activeFolderIndex - 1].Sum = 0.0D;

            if (double.IsNaN(m_activeSummary[activeFolderIndex - 1].N))
                m_activeSummary[activeFolderIndex - 1].N = 0;

            if (double.IsNaN(m_activeSummary[activeFolderIndex - 1].SquaredSum))
                m_activeSummary[activeFolderIndex - 1].SquaredSum = 0;

            m_activeSummary[activeFolderIndex - 1].N += m_activeSummary[activeFolderIndex].N;
            m_activeSummary[activeFolderIndex - 1].Sum += m_activeSummary[activeFolderIndex].Sum;
            m_activeSummary[activeFolderIndex - 1].SquaredSum += m_activeSummary[activeFolderIndex].SquaredSum;


            if (m_activeSummary[activeFolderIndex].Tmin < m_activeSummary[activeFolderIndex - 1].Tmin)
                m_activeSummary[activeFolderIndex - 1].Tmin = m_activeSummary[activeFolderIndex].Tmin;

            if (m_activeSummary[activeFolderIndex].Tmax > m_activeSummary[activeFolderIndex - 1].Tmax)
                m_activeSummary[activeFolderIndex - 1].Tmax = m_activeSummary[activeFolderIndex].Tmax;
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
