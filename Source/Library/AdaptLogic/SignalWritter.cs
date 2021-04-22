// ******************************************************************************************************
//  SignalWritter.tsx - Gbtc
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
//  04/04/2021 - C. Lackner
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
    /// Writes an Adapt Signal to temporary Files for reduced RAM requirement
    /// Note that we assume the Data Comes in order. This is very important.
    /// </summary>
    public class SignalWritter
    {
        #region [ Internal Classes ]

        #endregion

        #region [ Members ]

        private string m_rootFolder;
        private string[] m_activeFolder;
        private GraphPoint[] m_activeSummary;
        private long m_currentSecond;


        private List<ITimeSeriesValue> m_data;
        private List<ITimeSeriesValue> m_BadTSData;
        private Channel<ITimeSeriesValue> m_queue;

        private const int NLevels = 5;
        #endregion

        #region [ Constructor ]

        public SignalWritter(AdaptSignal Signal)
        {
            GenerateRoot();
            WriteGeneralFile(Signal);
            m_queue = Channel.CreateUnbounded<ITimeSeriesValue>();

          
            m_data = new List<ITimeSeriesValue>();
            m_BadTSData = new List<ITimeSeriesValue>();

            m_activeFolder = new string[NLevels] { "", "", "", "", "" };
            m_activeSummary = new GraphPoint[(NLevels+1)] { new GraphPoint(), new GraphPoint(), new GraphPoint(), new GraphPoint(), new GraphPoint(), new GraphPoint() };
            m_currentSecond = 0;
        }

        #endregion

        #region [ Properties]
        /// <summary>
        /// Gets the number of Frames in queue to be written to the file.
        /// </summary>
        public int Backlog => m_queue?.Reader?.Count ?? 0;
        #endregion

        #region [ Methods ]
        private void GenerateRoot()
        {
            string guid = Guid.NewGuid().ToString();
            m_rootFolder = $"{DataPath}{guid}";
            Directory.CreateDirectory(m_rootFolder);

        }

        private void WriteGeneralFile(AdaptSignal signal)
        {
            // Write .config file that just contains a bunch of Signal Information
            using (StreamWriter writer = new StreamWriter($"{m_rootFolder}{Path.DirectorySeparatorChar}Root.config"))
            {
                writer.WriteLine(signal.Name);
                writer.WriteLine(signal.Device);
                writer.WriteLine(signal.ID);
                writer.WriteLine(signal.Description);
                writer.WriteLine(signal.Phase);
                writer.WriteLine(signal.Type);
                writer.WriteLine(signal.FramesPerSecond);
            }
        }

        /// <summary>
        /// Adds a <see cref="ITimeSeriesValue"/> to be processed by this <see cref="SignalWritter"/>
        /// </summary>
        /// <param name="Value">The <see cref="ITimeSeriesValue"/> to be written to the Files</param>
        public async void AddPoint(ITimeSeriesValue Value)
        {
            await m_queue.Writer.WriteAsync(Value);
        }

        /// <summary>
        /// Signals the Writer that the last point has been added
        /// </summary>
        public void Complete()
        {
            m_queue.Writer.Complete();
        }


        /// <summary>
        /// Starts the Process of writing points to Files.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartWritter(CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                try
                {

                    await foreach (ITimeSeriesValue point in m_queue.Reader.ReadAllAsync(cancellationToken))
                    {
                        if (double.IsNaN(point.Value))
                            continue;

                        long second = (long)Math.Floor(point.Timestamp.ToSeconds());

                        if (second > m_currentSecond)
                            WriteSecond();

                        if (second < m_currentSecond)
                        {
                            m_BadTSData.Add(point);
                            continue;
                        }

                        m_data.Add(point);
                        m_currentSecond = second;


                    }

                    WriteSecond(true);

                }
                catch (Exception ex)
                {
                    int T = 1;
                }
            }, cancellationToken);

        }

        private void WriteSecond(bool forceIndexGen=false)
        {
            if (m_data.Count == 0)
                return;

            string year = m_data.FirstOrDefault()?.Timestamp.ToString("yyyy");
            string month = m_data.FirstOrDefault()?.Timestamp.ToString("MM");
            string day = m_data.FirstOrDefault()?.Timestamp.ToString("dd");
            string hour = m_data.FirstOrDefault()?.Timestamp.ToString("HH");
            string minute = m_data.FirstOrDefault()?.Timestamp.ToString("mm");
            string second = m_data.FirstOrDefault()?.Timestamp.ToString("ss");
           
            double min = m_data.Min(pt => pt.Value);
            double max = m_data.Max(pt => pt.Value);
            double avg = m_data.Average(pt => pt.Value);

            m_activeSummary[NLevels].Max = max;
            m_activeSummary[NLevels].Min = min;
            m_activeSummary[NLevels].N =  m_data.Count;
            m_activeSummary[NLevels].Sum = m_data.Sum(pt => pt.Value);
            m_activeSummary[NLevels].Tmax = m_data.Max(item => item.Timestamp);
            m_activeSummary[NLevels].Tmin = m_data.Min(item => item.Timestamp);

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

            int nSize = GraphPoint.NSize + (8 + 8) * m_data.Count;
            byte[] data = new byte[nSize];

            // File format version 1 is GraphPoint -> Data (Ticks -> Value)
            GraphPoint fileSummary = new GraphPoint();
            fileSummary.N = m_data.Count();
            fileSummary.Sum = m_data.Sum(pt => pt.Value);
            fileSummary.Min = min;
            fileSummary.Max = max;
            fileSummary.Tmax = m_data.Max(item => item.Timestamp);
            fileSummary.Tmin = m_data.Min(item => item.Timestamp);

            fileSummary.ToByte().CopyTo(data, 0);


            int j = GraphPoint.NSize;

            for (int i = m_data.Count; i > 0; i--)
            {
                BitConverter.GetBytes(m_data[i - 1].Timestamp.Value).CopyTo(data, j);
                BitConverter.GetBytes(m_data[i - 1].Value).CopyTo(data, j + 8);
                j = j + 16;
            }

            //Generate Folder if required

            string folder = $"{m_rootFolder}{Path.DirectorySeparatorChar}{string.Join(Path.DirectorySeparatorChar,m_activeFolder)}";
            Directory.CreateDirectory(folder);
            using (BinaryWriter writer = new BinaryWriter(File.OpenWrite($"{folder}{Path.DirectorySeparatorChar}{second}.bin")))
            {  // Writer raw data                
                writer.Write(data);
                writer.Flush();
                writer.Close();
            }

            m_data = new List<ITimeSeriesValue>();
        }

        private void GenerateIndex(int activeFolderIndex)
        {

            if (activeFolderIndex > 0)
                updateIndexSummary(activeFolderIndex);

            if (activeFolderIndex == NLevels)
                return;

            string path = $"{m_rootFolder}{Path.DirectorySeparatorChar}{string.Join(Path.DirectorySeparatorChar, m_activeFolder.Take(activeFolderIndex + 1))}";

            //write to file
            using (BinaryWriter writer = new BinaryWriter(File.OpenWrite($"{path}{Path.DirectorySeparatorChar}summary.node")))
            {              
                writer.Write(m_activeSummary[activeFolderIndex].ToByte());
                writer.Flush();
                writer.Close();
            }


            // reset lower level
            m_activeSummary[activeFolderIndex + 1] = new GraphPoint();

        }

        private void updateIndexSummary(int activeFolderIndex)
        {
            if (double.IsNaN(m_activeSummary[activeFolderIndex - 1].Min) || m_activeSummary[activeFolderIndex].Min < m_activeSummary[activeFolderIndex - 1].Min)
                m_activeSummary[activeFolderIndex - 1].Min = m_activeSummary[activeFolderIndex].Min;
            if (double.IsNaN(m_activeSummary[activeFolderIndex - 1].Max) || m_activeSummary[activeFolderIndex].Max > m_activeSummary[activeFolderIndex - 1].Max)
                m_activeSummary[activeFolderIndex - 1].Max = m_activeSummary[activeFolderIndex - 1].Max;

            if (double.IsNaN(m_activeSummary[activeFolderIndex - 1].Sum))
                m_activeSummary[activeFolderIndex - 1].Sum = 0.0D;

            if (double.IsNaN(m_activeSummary[activeFolderIndex - 1].N))
                m_activeSummary[activeFolderIndex - 1].N = 0;

            m_activeSummary[activeFolderIndex - 1].N += m_activeSummary[activeFolderIndex].N;
            m_activeSummary[activeFolderIndex - 1].Sum += m_activeSummary[activeFolderIndex].Sum;


            if (m_activeSummary[activeFolderIndex].Tmin < m_activeSummary[activeFolderIndex - 1].Tmin)
                m_activeSummary[activeFolderIndex - 1].Tmin = m_activeSummary[activeFolderIndex].Tmin;

            if (m_activeSummary[activeFolderIndex].Tmax > m_activeSummary[activeFolderIndex - 1].Tmax)
                m_activeSummary[activeFolderIndex - 1].Tmax = m_activeSummary[activeFolderIndex].Tmax;
        }
        #endregion

        #region [ Static ]

        private static readonly string DataPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}{Path.DirectorySeparatorChar}ADAPT{Path.DirectorySeparatorChar}dataTree{Path.DirectorySeparatorChar}";
        
        /// <summary>
        /// Removes all Signal data from <see cref="DataPath"/>
        /// </summary>
        public static void CleanAppData() 
        {
            Directory.Delete(DataPath, true);
        }
        #endregion
    }
}
