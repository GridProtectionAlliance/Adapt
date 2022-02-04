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
        Analytic m_analytic;

        #endregion

        #region [ Constructor ]

        public AnalyticProcessor(Analytic anallytic, Dictionary<string,int> framesPerSecond)
        {
            m_nextTimeStamp = null;

            m_analytic = anallytic;
            m_instance = CreateAnalytic(m_analytic, framesPerSecond);

            InputNames = m_instance.InputNames().ToList();

            
        }

        #endregion

        #region [ Properties]

        #endregion

        #region [ Methods ]

        public Task<ITimeSeriesValue[]> Run(IFrame frame)
        {
            if (m_nextTimeStamp == null)
                m_nextTimeStamp = frame.Timestamp;

            if (m_nextTimeStamp >= frame.Timestamp)
            {
                IFrame input = RouteInput(frame);
                m_nextTimeStamp = m_nextTimeStamp + (long)(Gemstone.Ticks.PerSecond * 1.0 / ((double)m_instance.FramesPerSecond));
                return m_instance.Run(input, new IFrame[0] { }, new IFrame[0] { });
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
            int i = 0;
            foreach (ITimeSeriesValue val in result)
            {
                frame.Measurements.AddOrUpdate(m_analytic.Outputs[i].Name, (key) => new AdaptValue(key, val.Value, val.Timestamp), (key, old) => val);
                i++;
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
