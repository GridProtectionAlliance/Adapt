// ******************************************************************************************************
//  SectionProcessor.tsx - Gbtc
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
//  10/19/2021 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************

using Adapt.Models;
using Gemstone;
using GemstoneAnalytic;
using GemstoneCommon;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static AdaptLogic.AdaptTask;

namespace AdaptLogic
{
    /// <summary>
    /// Processes an Adapt Signal through a Section of Analytics
    /// </summary>
    public class SectionProcessor
    {
        #region [ Internal Classes ]

        #endregion

        #region [ Members ]

        private Channel<IFrame> m_queueInput;
        private Channel<IFrame> m_queueOutput;

        private List<AnalyticProcessor> m_analyticProcesors;
        private int m_futureFrameBufferSize;
        private Queue<IFrame> m_futureFrameBuffer;

        private Ticks m_lastProcessedTS;
        public int FramesPerSecond { get; }
        #endregion

        #region [ Constructor ]

        /// <summary>
        /// Generates a new <see cref="SectionProcessor"/> to process an instance of a <see cref="TaskSection"/>
        /// </summary>
        /// <param name="inputQueue"> The Queue to Dequeue frames from</param>
        /// <param name="outputQueue"> The queue to Queue frames onto </param>
        /// <param name="templateMappingID"> The String identifier used for identifying Signals</param>
        /// <param name="section"> The <see cref="TaskSection"/> to be processed </param>
        /// <param name="template">The <see cref="AdaptTask"/> containing the <paramref name="section"/></param>
        /// <param name="signalMapping">The mapping used for InputSignals.</param>
        public SectionProcessor(Channel<IFrame> inputQueue, Channel<IFrame> outputQueue, string templateMappingID, TaskSection section, AdaptTask template, Dictionary<int,AdaptSignal> signalMapping, Dictionary<string,int> framesPerSecond  )
        {
            m_queueInput = inputQueue;
            m_queueOutput = outputQueue;
            m_analyticProcesors = section.Analytics.Select(item => new AnalyticProcessor(item,template,templateMappingID,signalMapping, framesPerSecond)).ToList();
            m_futureFrameBufferSize = m_analyticProcesors.Max(a => a.NFutureFrames);
            m_futureFrameBuffer = new Queue<IFrame>(m_futureFrameBufferSize);
            FramesPerSecond = TimeAlignment.Combine(m_analyticProcesors.Select(item => item.FramesPerSecond).Where(fps => fps > 0).ToArray());
            m_lastProcessedTS = Ticks.MinValue;

        }
        #endregion

        #region [ Properties]
        /// <summary>
        /// Gets the number of Frames in queue to be processed.
        /// </summary>
        public int Backlog => m_queueInput?.Reader?.Count ?? 0;
        #endregion

        #region [ Methods ]

        /// <summary>
        /// Signals the Writer that the last point has been added
        /// </summary>
        private void Complete()
        {
            m_queueOutput.Writer.Complete();
        }


        /// <summary>
        /// Starts the Process of computing any analytics.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartProcessor(CancellationToken cancellationToken)
        {

            return Task.Run(async () =>
            {
                try
                {
                    IFrame point;
                    m_futureFrameBuffer = new Queue<IFrame>(m_futureFrameBufferSize + 1);
                    int nPoints = 0;
                    Gemstone.Ticks lastProcessed = Ticks.MinValue;

                    while (await m_queueInput.Reader.WaitToReadAsync(cancellationToken))
                    {
                        if (!m_queueInput.Reader.TryRead(out point))
                            continue;

                       
                        // Tolerance withing a few Ticks of expected TimeStamp
                        Ticks aligned = Ticks.AlignToMicrosecondDistribution(point.Timestamp, FramesPerSecond);
                        long diff = aligned - point.Timestamp;

                        if (diff > Ticks.PerMillisecond || diff < -Ticks.PerMillisecond)
                            continue;

                        // Generate Timestamps in between if necessary
                        while (aligned - (m_lastProcessedTS + (long)(Ticks.PerSecond * (1.0D / (double)FramesPerSecond))) > (long)(Ticks.PerSecond*(0.5D/ (double)FramesPerSecond)) 
                            && m_lastProcessedTS != Ticks.MinValue)
                        {
                            m_lastProcessedTS = m_lastProcessedTS + (long)(Ticks.PerSecond * (1.0D / (double)FramesPerSecond));
                            IFrame frame = new Frame()
                            {
                                Timestamp = Ticks.AlignToMicrosecondDistribution(m_lastProcessedTS, FramesPerSecond),
                                Published = point.Published,
                                Measurements = new ConcurrentDictionary<string, ITimeSeriesValue>()
                            };
                            lastProcessed = frame.Timestamp;
                            nPoints++;
                            m_futureFrameBuffer.Enqueue(frame);
                            if (nPoints <= m_futureFrameBufferSize)
                                continue;

                            frame = m_futureFrameBuffer.Dequeue();

                            await ProcessPoint(frame);
                        }

                        nPoints++;
                        lastProcessed = point.Timestamp;
                        m_futureFrameBuffer.Enqueue(point);
                        if (nPoints <= m_futureFrameBufferSize)
                            continue;

                        point = m_futureFrameBuffer.Dequeue();
                        m_lastProcessedTS = aligned;
                        await ProcessPoint(point);

                    }

                    // Run through the last set of Points 
                    int i = 0;
                    while (i < m_futureFrameBuffer.Count)
                    {
                        point = m_futureFrameBuffer.Dequeue();
                        lastProcessed = point.Timestamp;
                        await ProcessPoint(point);
                        i++;
                    }

                    Task[] analyticCleanups = m_analyticProcesors.Select(p => p.RunCleanup(lastProcessed)).ToArray();
                    await Task.WhenAll(analyticCleanups).ConfigureAwait(false);

                    int j = 0;
                    IFrame cleanupFrame = new Frame() {
                        Measurements = new ConcurrentDictionary<string, ITimeSeriesValue>(),
                        Published = false,
                        Timestamp = Ticks.MaxValue

                    };

                    foreach (Task<ITimeSeriesValue[]> analyticResult in analyticCleanups)
                    {
                        m_analyticProcesors[j].RouteOutput(cleanupFrame, analyticResult.Result);
                        j++;
                    }

                    m_queueOutput.Writer.TryWrite(cleanupFrame);

                    Complete();
                }
                catch (Exception ex)
                {
                    int T = 1;
                }
            }, cancellationToken);

        }

        private async Task ProcessPoint(IFrame point)
        {

            IFrame result = new Frame()
            {
                Timestamp = Ticks.AlignToMicrosecondDistribution(point.Timestamp, FramesPerSecond),
                Published = point.Published,
                Measurements = point.Measurements
            };

            Task<ITimeSeriesValue[]>[] analytics = m_analyticProcesors.Select(p => p.Run(point, m_futureFrameBuffer.ToArray())).ToArray();

            await Task.WhenAll(analytics).ConfigureAwait(false);

            int i = 0;
            foreach (Task<ITimeSeriesValue[]> analyticResult in analytics)
            {
                m_analyticProcesors[i].RouteOutput(point, analyticResult.Result);
                i++;
            }

            m_queueOutput.Writer.TryWrite(result);
        }

        #endregion

        #region [ Static ]

        
        #endregion
    }
}
