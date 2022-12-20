﻿// ******************************************************************************************************
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


        private long m_currentSecond;
        private string m_rootFolder;

        private int NProcessed;
        private int NTotalProcessed;

        private List<ITimeSeriesValue> m_data;
        private List<ITimeSeriesValue> m_BadTSData;
        private Channel<ITimeSeriesValue> m_queue;

        private AdaptSignal m_signal;
        private List<string> m_eventParameters;

        #endregion

        #region [ Constructor ]

        public SignalWritter(AdaptSignal Signal): this(Signal, new Dictionary<string, Tuple<string, string>[]>())  {  }

        public SignalWritter(AdaptSignal Signal, Dictionary<string,Tuple<string,string>[]> Variables )
        {
            m_signal = Signal;
            m_queue = Channel.CreateUnbounded<ITimeSeriesValue>();

            GenerateRoot();
            WriteGeneralFile();

            m_data = new List<ITimeSeriesValue>();
            m_BadTSData = new List<ITimeSeriesValue>();

            m_currentSecond = 0;
            m_eventParameters = new List<string>();

            NTotalProcessed = 0;
            NProcessed = 0;
        }

        #endregion

        #region [ Properties]
        /// <summary>
        /// Gets the number of Frames in queue to be written to the file.
        /// </summary>
        public int Backlog => m_queue?.Reader?.Count ?? 0;

        /// <summary>
        /// Logs Messages and exceptions
        /// </summary>
        public event EventHandler<MessageArgs> MessageRecieved;

        /// <summary>
        /// Tracks Progress every 1000 Points
        /// </summary>
        public event EventHandler<ProcessedEventArgs> ProcessedUpdate;
        #endregion

        #region [ Methods ]
        private void GenerateRoot()
        {
            string guid = Guid.NewGuid().ToString();
            m_rootFolder = $"{DataPath}{guid}";
            Directory.CreateDirectory(m_rootFolder);

        }

        private void WriteGeneralFile()
        {
            if (m_signal.Type == MeasurementType.EventFlag)
                return;
            // Write .config file that just contains a bunch of Signal Information
            using (StreamWriter writer = new StreamWriter($"{m_rootFolder}{Path.DirectorySeparatorChar}Root.config"))
            {
                writer.WriteLine(m_signal.Name);
                writer.WriteLine(m_signal.Device);
                writer.WriteLine(m_signal.ID);
                writer.WriteLine(m_signal.Description);
                writer.WriteLine(m_signal.Phase);
                writer.WriteLine(m_signal.Type);
                writer.WriteLine(m_signal.FramesPerSecond);
            }
        }

        private void WriteGeneralEventFile()
        {
            if (m_signal.Type != MeasurementType.EventFlag)
                return;

            // Write .config file that just contains a bunch of Signal Information
            using (StreamWriter writer = new StreamWriter($"{m_rootFolder}{Path.DirectorySeparatorChar}Root.config"))
            {
                writer.WriteLine("Event Data Format");
                writer.WriteLine(m_signal.Name);
                writer.WriteLine(m_signal.Device);
                writer.WriteLine(m_signal.ID);
                writer.WriteLine(m_signal.Description);
                writer.WriteLine(m_signal.Phase);
                writer.WriteLine(m_signal.Type);
                writer.WriteLine(m_signal.FramesPerSecond);
                writer.WriteLine("Characteristics");
                foreach (string p in m_eventParameters)
                    writer.WriteLine(p);
            }
        }

        private void GenerateEventParameters(ITimeSeriesValue Value) 
        {
            if (!Value.IsEvent)
                return;
            AdaptEvent evt = (AdaptEvent)Value;
            m_eventParameters = evt.ParameterNames;
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
                    ISignalWritter writer = new DataSignalWritter(m_rootFolder);
                    writer.MessageRecieved += (object source, MessageArgs args) => { MessageRecieved?.Invoke(source, args); };

                    ITimeSeriesValue point;
                    bool processFirst = true;
                    while (await m_queue.Reader.WaitToReadAsync(cancellationToken))
                    {
                        if (!m_queue.Reader.TryRead(out point))
                            continue;

                        NTotalProcessed++;
                        NProcessed++;

                        if (NProcessed > 1000)
                        {
                            NProcessed = 0;
                            ProcessedUpdate?.Invoke(this, new ProcessedEventArgs(NTotalProcessed, point.Timestamp));
                        }
                        if (processFirst && m_signal.Type == MeasurementType.EventFlag)
                        {
                            GenerateEventParameters(point);
                            processFirst = false;
                            writer = new EventSignalWritter(m_rootFolder, m_eventParameters);
                            writer.MessageRecieved += (object source, MessageArgs args) => { MessageRecieved?.Invoke(source, args); };
                        }    

                        if (double.IsNaN(point.Value))
                            continue;

                        
                        long second = (long)Math.Floor(point.Timestamp.ToSeconds());

                        if (second > m_currentSecond)
                        {
                            writer.WriteSecond(m_data, false);
                            m_data = new List<ITimeSeriesValue>();
                        }

                        if (second < m_currentSecond)
                        {
                            MessageRecieved?.Invoke(this, new MessageArgs($"Identified out of order Timestamp: {point.Timestamp.ToString()}", MessageArgs.MessageLevel.Info));
                            m_BadTSData.Add(point);
                            continue;
                        }

                        m_data.Add(point);
                        m_currentSecond = second;
                    }

                    writer.WriteSecond(m_data,true);                    
                }
                catch (Exception ex)
                {
                    MessageRecieved?.Invoke(this, new MessageArgs("Exception occured writting a Signal", ex, MessageArgs.MessageLevel.Error));
                }
                if (m_signal.Type == MeasurementType.EventFlag)
                    WriteGeneralEventFile();
            }, cancellationToken);

        }

        #endregion

        #region [ Static ]

        private static readonly string DataPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}{Path.DirectorySeparatorChar}ADAPT{Path.DirectorySeparatorChar}dataTree{Path.DirectorySeparatorChar}";
        
        /// <summary>
        /// Removes all Signal data from <see cref="DataPath"/>
        /// </summary>
        public static void CleanAppData() 
        {
            if (Directory.Exists(DataPath))
                Directory.Delete(DataPath, true);
        }
        #endregion
    }
}
