﻿// ******************************************************************************************************
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

        private List<AnalyticProcessor> m_analyticProcesors;
        

        #endregion

        #region [ Constructor ]

        public SignalProcessor(Channel<IFrame> input, Channel<IFrame> output, TaskSection section, Dictionary<string,int> framesPerSecond)
        {
            m_queueInput = input;
            m_queueOutput = output;
            m_analyticProcesors = section.Analytics.Select(item => new AnalyticProcessor(item, framesPerSecond)).ToList();
           
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

                    await foreach (IFrame point in m_queueInput.Reader.ReadAllAsync(cancellationToken))
                    {
                        IFrame result = new Frame() {
                            Timestamp = point.Timestamp,
                            Published=point.Published,
                            Measurements = point.Measurements
                        };

                        Task<ITimeSeriesValue[]>[] analytics = m_analyticProcesors.Select(p => p.Run(point)).ToArray();

                        await Task.WhenAll(analytics).ConfigureAwait(false);

                        int i = 0;
                        foreach (Task<ITimeSeriesValue[]> analyticResult in analytics)
                        {
                            m_analyticProcesors[i].RouteOutput(point, analyticResult.Result);
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
