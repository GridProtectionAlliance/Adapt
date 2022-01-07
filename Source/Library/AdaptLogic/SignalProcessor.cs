// ******************************************************************************************************
//  SignalProcessor.tsx - Gbtc
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

namespace AdaptLogic
{
    /// <summary>
    /// Processes an Adapt Signal through a Section of Analytics
    /// </summary>
    public class SignalProcessor
    {
        #region [ Internal Classes ]

        #endregion

        #region [ Members ]

        private Channel<IFrame> m_queueInput;
        private Channel<IFrame> m_queueOutput;
        private List<IAnalytic> m_analytics;
        private List<Gemstone.Ticks> m_nextTimeStamp;
        private List<Analytic> m_analyticDefinitions;
        private List<Func<IFrame, Analytic, IFrame>> m_inputRouter;
        private List<Action<IFrame, ITimeSeriesValue[], Analytic>> m_outputRouter;

        #endregion

        #region [ Constructor ]

        public SignalProcessor(Channel<IFrame> input, Channel<IFrame> output, TaskSection section, Dictionary<string,int> framesPerSecond)
        {
            m_queueInput = input;
            m_queueOutput = output;
            m_analyticDefinitions = section.Analytics;
            m_analytics = section.Analytics.Select(a => CreateAnalytic(a, framesPerSecond)).ToList();
            m_inputRouter = section.Analytics.Select((a,i) => {
                List<string> inputNames = m_analytics[i].InputNames().ToList();
                return new Func<IFrame, Analytic, IFrame>((fullFrame, analytic) => {
                    return new Frame()
                    {
                        Timestamp = fullFrame.Timestamp,
                        Published = fullFrame.Published,

                        Measurements = new ConcurrentDictionary<string, ITimeSeriesValue>(
                            a.Inputs.Select(item => fullFrame.Measurements[item]).ToDictionary(item => inputNames[a.Inputs.FindIndex((s) => s == item.ID)], item => item))
                        
                    };
                    });

                }).ToList();

            // Adjust FPS
            m_outputRouter = section.Analytics.Select(a => 
            
            new Action<IFrame,ITimeSeriesValue[], Analytic>((frame, values, analytic) => {
                int i = 0;
                foreach(ITimeSeriesValue val in values)
                {
                    frame.Measurements.AddOrUpdate(analytic.Outputs[i], (key) => new AdaptValue(key,val.Value, val.Timestamp), (key, old) => val);
                    i++;
                }
                                  
            })).ToList();
            
        }
        
        #endregion

        #region [ Properties]
        /// <summary>
        /// Gets the number of Frames in queue to be processed.
        /// </summary>
        public int Backlog => m_queueInput?.Reader?.Count ?? 0;
        #endregion

        #region [ Methods ]
        private IAnalytic CreateAnalytic(Analytic analytic, Dictionary<string, int> framesPerSecond)
        {
            try
            {
                IAnalytic Instance = (IAnalytic)Activator.CreateInstance(analytic.AdapterType);
                Instance.Configure(analytic.Configuration);
                Instance.SetInputFPS(analytic.Inputs.Select(item => framesPerSecond[item]));

                analytic.Outputs.ForEach((item) => framesPerSecond.Add(item, Instance.FramesPerSecond));
                return Instance;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

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
                m_nextTimeStamp = null;
                try
                {

                    await foreach (IFrame point in m_queueInput.Reader.ReadAllAsync(cancellationToken))
                    {
                        if (m_nextTimeStamp == null)
                        {
                            m_nextTimeStamp = m_analytics.Select(a => point.Timestamp).ToList();
                        }

                        IFrame result = new Frame() {
                            Timestamp = point.Timestamp,
                            Published=point.Published,
                            Measurements = point.Measurements
                        };

                        Task<ITimeSeriesValue[]>[] analytics = m_analytics.Select((analytic, index) => Task<ITimeSeriesValue[]>.Run(() =>
                        {
                            if (m_nextTimeStamp[index] >= point.Timestamp)
                            {
                                IFrame input = m_inputRouter[index](point, m_analyticDefinitions[index]);
                                m_nextTimeStamp[index] = m_nextTimeStamp[index] + (long)(Gemstone.Ticks.PerSecond * 1.0 / ((double)analytic.FramesPerSecond));
                                return analytic.Run(input, new IFrame[0] { }, new IFrame[0] { });
                            }
                            else
                                return Task<ITimeSeriesValue[]>.FromResult(new ITimeSeriesValue[0]);

                        })).ToArray();

                        await Task.WhenAll(analytics).ConfigureAwait(false);

                        int i = 0;
                        foreach (Task<ITimeSeriesValue[]> analyticResult in analytics)
                        {
                            if (analyticResult.Result.Length > 0)
                                m_outputRouter[i](result, analyticResult.Result, m_analyticDefinitions[i]);
                            i++;
                        }

                        m_queueOutput.Writer.TryWrite(result);
                    }

                    Complete();
                }
                catch (Exception ex)
                {
                    int T = 1;
                }
            }, cancellationToken);

        }

        #endregion

        #region [ Static ]

        
        #endregion
    }
}
