// ******************************************************************************************************
//  AnalyticProcessor.tsx - Gbtc
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
    /// Processes an Analytic
    /// </summary>
    public class AnalyticProcessor
    {
        #region [ Internal Classes ]

        #endregion

        #region [ Members ]

        private Gemstone.Ticks m_nextTimeStamp;
        private IAnalytic m_instance;

        private List<string> InputNames;
      
        private Analytic m_analytic;

        private Queue<IFrame> m_pastPoints;

        private int NProcessed;

        /// <summary>
        /// Returns the number of future Frames required for this Analytic
        /// </summary>
        public int NFutureFrames => m_instance?.FutureFrames ?? 0;
        #endregion

        #region [ Constructor ]

        public AnalyticProcessor(Analytic analytic, Dictionary<string,int> framesPerSecond)
        {
            m_nextTimeStamp = Gemstone.Ticks.MinValue;

            m_analytic = analytic;
            m_instance = CreateAnalytic(m_analytic, framesPerSecond);

            InputNames = m_instance.InputNames().ToList();

            m_pastPoints = new Queue<IFrame>(m_instance.PrevFrames);
            NProcessed = 0;
        }

        #endregion

        #region [ Properties]
        private List<AnalyticOutputDescriptor> OutputDescriptions => (m_instance?.Outputs().ToList() ?? new List<AnalyticOutputDescriptor>());

        #endregion

        #region [ Methods ]

        public Task<ITimeSeriesValue[]> Run(IFrame frame, IFrame[] futureFrames)
        {
            NProcessed++;
            if (m_nextTimeStamp == Gemstone.Ticks.MinValue)
                m_nextTimeStamp = frame.Timestamp;

            if (m_nextTimeStamp <= frame.Timestamp)
            {

                IFrame input = RouteInput(frame);
                m_nextTimeStamp = m_nextTimeStamp + (long)(Gemstone.Ticks.PerSecond * 1.0 / ((double)m_instance.FramesPerSecond));
                
                Task<ITimeSeriesValue[]> task = m_instance.Run(input, m_pastPoints.ToArray(), futureFrames.Skip(futureFrames.Length - NFutureFrames).ToArray());

                if (m_instance.PrevFrames > 0)
                {
                    m_pastPoints.Enqueue(frame);
                    if (m_pastPoints.Count > m_instance.PrevFrames)
                        m_pastPoints.Dequeue();
                }

                return task;
            }
            else
                return Task<ITimeSeriesValue[]>.FromResult(new ITimeSeriesValue[0]);
        }

        private IFrame RouteInput(IFrame frame)
        {
            return new Frame()
            {
                Timestamp = frame.Timestamp,
                Published = frame.Published,

                Measurements = new ConcurrentDictionary<string, ITimeSeriesValue>(
                            m_analytic.Inputs.Select(item => frame.Measurements[item])
                            .ToDictionary(item => InputNames[m_analytic.Inputs.FindIndex((s) => s == item.ID)], item => item)
                            )
            };
        }

        public void RouteOutput(IFrame frame, ITimeSeriesValue[] result)
        {
            foreach (ITimeSeriesValue val in result)
            {
                int i = OutputDescriptions.FindIndex(item => item.Name == val.ID);
                if (i < 0)
                    continue;
                frame.Measurements.AddOrUpdate(m_analytic.Outputs[i].Name, 
                    (key) => new AdaptValue(key, val.Value, val.Timestamp),
                    (key, old) => val);
            }
        }

        #endregion

        #region [ Static ]

        public static IAnalytic CreateAnalytic(Analytic analytic, Dictionary<string, int> framesPerSecond)
        {
            try
            {
                IAnalytic Instance = (IAnalytic)Activator.CreateInstance(analytic.AdapterType);
                Instance.Configure(analytic.Configuration);
                Instance.SetInputFPS(analytic.Inputs.Select(item => framesPerSecond[item]));

                analytic.Outputs.ForEach((item) => framesPerSecond.Add(item.Name, Instance.FramesPerSecond));
                return Instance;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        #endregion
    }
}
