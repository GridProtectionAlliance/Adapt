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
using static AdaptLogic.AdaptTask;

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
        private string m_mappingID;
        private Dictionary<string, string> InputMappings;
        private Dictionary<string, string> OutputMappings;

        private Queue<IFrame> m_pastPoints;

        private int NProcessed;
        public int FramesPerSecond => m_instance?.FramesPerSecond ?? -1;

        /// <summary>
        /// Returns the number of future Frames required for this Analytic
        /// </summary>
        public int NFutureFrames => m_instance?.FutureFrames ?? 0;

        /// <summary>
        /// Event that raises a Message from Processing this Analytic. 
        /// </summary>
        public event EventHandler<MessageArgs> MessageRecieved;
        #endregion

        #region [ Constructor ]

        /// <summary>
        /// Creates a new <see cref="AnalyticProcessor"/>
        /// </summary>
        /// <param name="analytic"> The <see cref="TaskAnalytic"/></param>
        /// <param name="task"> The <see cref="AdaptTask"/> this analytic is included in </param>
        /// <param name="TemplateSignalMapping">The Signal Mapping used for this instance </param>
        /// <param name="templateMappingID"> The ID of the associated <paramref name="TemplateSignalMapping"/></param>
        /// <param name="FramesPerSecond">set of the FPS of all Signals by ID</param>
        public AnalyticProcessor(TaskAnalytic analytic, AdaptTask task, string templateMappingID, Dictionary<int, AdaptSignal> TemplateSignalMapping, Dictionary<string, InternalSigDescriptor> InternalSigDescriptor)
        {
            m_nextTimeStamp = Gemstone.Ticks.MinValue;
            m_mappingID = templateMappingID;

            m_instance = CreateAnalytic(analytic, InternalSigDescriptor, TemplateSignalMapping);

            GenerateRoutes(analytic, TemplateSignalMapping);

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
                IFrame[] forwardFrames = futureFrames.Skip(futureFrames.Length - NFutureFrames).Select(f => RouteInput(f)).ToArray();
                m_nextTimeStamp = m_nextTimeStamp + (long)(Gemstone.Ticks.PerSecond * 1.0 / ((double)m_instance.FramesPerSecond));
                
                Task<ITimeSeriesValue[]> task = m_instance.Run(input, m_pastPoints.ToArray(), forwardFrames);

                if (m_instance.PrevFrames > 0)
                {
                    m_pastPoints.Enqueue(input);
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
                            m_instance.InputNames().Select(item => new
                            {
                                Name = item,
                                Key = InputMappings[item]
                            }).Select(s =>
                            {
                                if (frame.Measurements.ContainsKey(s.Key))
                                    return AdjustSignal(frame.Measurements[s.Key], s.Name);
                                return new AdaptValue(s.Name, double.NaN, frame.Timestamp);
                            }).ToDictionary(item => item.ID, item => item))
            };        
        }

        public void RouteOutput(IFrame frame, ITimeSeriesValue[] result)
        {
            foreach (ITimeSeriesValue val in result)
            {
                if (OutputMappings.ContainsKey(val.ID))
                    frame.Measurements.AddOrUpdate(OutputMappings[val.ID], 
                        (key) => AdjustSignal(val, OutputMappings[val.ID]),
                        (key, old) => val);
            }
        }

        public Task<ITimeSeriesValue[]> RunCleanup(Gemstone.Ticks ticks)
        {
            return m_instance.CompleteComputation(ticks);
        }

        private ITimeSeriesValue AdjustSignal (ITimeSeriesValue original, string key)
        {
            if (original.IsEvent)
            {
                try 
                {
                    AdaptEvent originalEvt = (AdaptEvent)original;
                    return new AdaptEvent(key, original.Timestamp, originalEvt.Value,
                        originalEvt.ParameterNames
                        .Select(item => new KeyValuePair<string, double>(item, originalEvt[item])).ToArray()
                        );

                }
                catch
                {
                    return new AdaptValue(key, original.Value, original.Timestamp);
                }
               
            }
            return new AdaptValue(key, original.Value, original.Timestamp);
        }

        private IAnalytic CreateAnalytic(TaskAnalytic analytic, Dictionary<string, InternalSigDescriptor> internalSigDesc, Dictionary<int, AdaptSignal> templateSignalMapping)
        {
            try
            {
                IAnalytic instance = (IAnalytic)Activator.CreateInstance(analytic.AnalyticType);
                instance.Configure(analytic.Configuration);
                instance.SetInputFPS(analytic.InputModel.Select(item => {
                    if (item.IsInputSignal)
                        return templateSignalMapping[item.SignalID].FramesPerSecond;
                    string signalID = m_mappingID + "AnalyticOutput-" + item.SignalID;
                    return internalSigDesc[signalID].FramesPerSecond;
                }));

                List<AnalyticOutputDescriptor> outputs = instance.Outputs().ToList();
                analytic.OutputModel.ForEach((item) => internalSigDesc.Add(m_mappingID + "AnalyticOutput-" + item.ID, new InternalSigDescriptor(instance.FramesPerSecond, outputs[item.OutputIndex].Phase, outputs[item.OutputIndex].Type)));
                return instance;
            }
            catch (Exception ex)
            {
                MessageRecieved?.Invoke(this, new MessageArgs("Unable to generate or configure Analytic", ex, MessageArgs.MessageLevel.Error));
                return null;
            }
        }

        private void GenerateRoutes(TaskAnalytic analytic,  Dictionary<int, AdaptSignal> TemplateSignalMapping)
        {
            InputMappings = new Dictionary<string, string>();

            int i = 0;
            foreach (string inputName in m_instance.InputNames())
            {
                string key = "";
                AnalyticInput input = analytic.InputModel.Where(item => item.InputIndex == i).FirstOrDefault();
                if (input is null)
                {
                    i++;
                    continue;
                }

                if (input.IsInputSignal)
                    key = m_mappingID + "-" + TemplateSignalMapping[input.SignalID].ID;
                else
                    key = m_mappingID + "AnalyticOutput-" + input.SignalID;

                InputMappings.Add(inputName, key);
                i++;
            }

            OutputMappings = new Dictionary<string, string>();
            i = 0;
            foreach (AnalyticOutputDescriptor outputName in m_instance.Outputs())
            {
                string key = "";
                AnalyticOutputSignal output = analytic.OutputModel.Where(item => item.OutputIndex == i).FirstOrDefault();
                if (output is null)
                {
                    i++;
                    continue;
                }

                key = m_mappingID + "AnalyticOutput-" + output.ID;

                OutputMappings.Add(outputName.Name, key);
                i++;
            }
        }
        #endregion

        #region [ Static ]



        #endregion
    }
}
