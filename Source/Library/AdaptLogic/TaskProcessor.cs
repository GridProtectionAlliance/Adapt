// ******************************************************************************************************
//  TaskProcessor.tsx - Gbtc
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
using Gemstone;
using GemstoneCommon;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AdaptLogic
{
    /// <summary>
    /// Process a Task from loading the Data through to writing it to the temporary Files
    /// </summary>
    public class TaskProcessor
    {
        #region [ Members ]
        private IDataSource m_Source;
        private Channel<IFrame> m_sourceQueue;
        private ConcurrentDictionary<string, SignalWritter> m_writers;
        private List<AdaptSignal> m_sourceSignals;
        private DateTime m_start;
        private DateTime m_end;
        private Task<bool> m_mainProcess;

        private CancellationTokenSource m_cancelationSource;
        private int m_DataSourceProgress;
        #endregion

        #region [ Properties ]
        /// <summary>
        /// Event that Reports the Progress of Processing this Task. 
        /// </summary>
        /// <remarks>
        /// Accuracy depends on DataSource implementation.
        /// </remarks>
        public event EventHandler<ProgressArgs> ReportProgress;

        #endregion
        #region [ Constructor ]

        /// <summary>
        /// Create as new <see cref="TaskProcessor"/> that only grabs the data from the <see cref="IDataSource"/> and saves it.
        /// No Processing is done in between.
        /// </summary>
        /// <param name="Signals"> A list of <see cref="AdaptSignal"/> to grab.</param>
        /// <param name="Source"> The <see cref="DataSource"/> used to get the Data</param>
        public TaskProcessor(List<AdaptSignal> Signals, DataSource Source, DateTime start, DateTime end)
        {
            CreateSourceInstance(Source);
            m_writers = new ConcurrentDictionary<string,SignalWritter>(Signals.ToDictionary(signal => signal.ID, signal => new SignalWritter(signal)));
            m_sourceQueue = Channel.CreateUnbounded<IFrame>();
            m_start = start;
            m_end = end;
            m_sourceSignals = Signals;
            m_cancelationSource = new CancellationTokenSource();
            m_DataSourceProgress = 0;
        }

        #endregion

        #region [ Methods ]
        private bool CreateSourceInstance(DataSource source)
        {
            try
            {
                Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(source.AssemblyName));
                Type type = assembly.GetType(source.TypeName);
                m_Source = (IDataSource)Activator.CreateInstance(type);
                IConfiguration config = new ConfigurationBuilder().AddGemstoneConnectionString(source.ConnectionString).Build();
                m_Source.Configure(config);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        /// <summary>
        /// Starts the Task
        /// </summary>
        /// <returns>A Task returning <see cref="true"/> if the Task was run successfully and <see cref="false"/> if it failed</returns>
        public Task<bool> StartTask()
        {
            if (m_Source == null)
                return new Task<bool>(() => false);

            m_mainProcess = new Task<bool>(() =>
            {
               
                Task getData = new Task(() => GetData(m_cancelationSource.Token));
                Task writeData = new Task(() => WriteData(m_cancelationSource.Token));
                getData.Start();
                writeData.Start();

                Task[] writterTasks = m_writers.Select(item => item.Value.StartWritter(m_cancelationSource.Token)).ToArray();

                getData.Wait();
                writeData.Wait();
                Task.WaitAll(writterTasks);

                ReportComplete();
                return true;
            });

            m_mainProcess.Start();

            return m_mainProcess;
        }

        private async void GetData(CancellationToken cancelationToken)
        {
            try
            {
                int count = 0;
                foreach (IFrame frame in m_Source.GetData(m_sourceSignals, m_start, m_end))
                {
                    await m_sourceQueue.Writer.WriteAsync(frame, cancelationToken);
                    count++;
                    if (count % 1000 == 0 && m_Source.SupportProgress())
                        ReportDatasourceProgress(m_Source.GetProgress());
                }
            }
            finally
            {
                m_sourceQueue.Writer.Complete();
            }
        }

        private async void WriteData(CancellationToken cancelationToken)
        {
            try
            {
                await foreach (IFrame frame in m_sourceQueue.Reader.ReadAllAsync(cancelationToken))
                {

                    foreach (KeyValuePair<string, ITimeSeriesValue> value in frame.Measurements)
                    {
                        SignalWritter writer;
                        if (m_writers.TryGetValue(value.Key, out writer))
                            writer.AddPoint(value.Value);
                    }
                   
                }
            }
            finally
            {
                m_writers.Values.ToList().ForEach(item => item.Complete());
            }
        }

        /// <summary>
        /// Cancels the Current Task.
        /// </summary>
        public void CancelTask()
        {
            m_cancelationSource.Cancel();
        }

        private void ReportComplete()
        {
            ProgressArgs args = new ProgressArgs("Completed Task.", true, 0);
            ReportProgress?.Invoke(this,args);
        }

        private void ReportDatasourceProgress(double dataSourceProgress)
        {
            double totalProgress = dataSourceProgress* 0.5D;
            double wFactor = 1.0D / (double)m_writers.Count()* 0.5D;
            foreach (KeyValuePair<string,SignalWritter> writer in m_writers)
            {
                totalProgress += dataSourceProgress * wFactor * ((-1.0D / 10000.0D) * (double)writer.Value.Backlog + 1.0D);
            }

            ProgressArgs args = new ProgressArgs("Loading Data.", false, (int)totalProgress);
            ReportProgress?.Invoke(this, args);
        }

         #endregion
    }
}
