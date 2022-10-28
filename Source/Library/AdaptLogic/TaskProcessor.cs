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
using GemstoneAnalytic;
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

        private Dictionary<string, List<AdaptSignal>> m_TemplateInputs;
        private Channel<IFrame> m_writeQueue;


        private ConcurrentDictionary<string, SignalWritter> m_writers;
        private Dictionary<string,List<SectionProcessor>> m_processors;

        private List<AdaptSignal> m_sourceSignals;
        private DateTime m_start;
        private DateTime m_end;
        private Task m_mainProcess;

        private int m_commonFrameRate;

        private Dictionary<string, List<Channel<IFrame>>> m_sectionQueue;

        private CancellationTokenSource m_cancelationSource;
        
        #endregion

        #region [ Properties ]
        /// <summary>
        /// Event that Reports the Progress of Processing this Task. 
        /// </summary>
        /// <remarks>
        /// Accuracy depends on DataSource implementation.
        /// </remarks>
        public event EventHandler<ProgressArgs> ReportProgress;

        /// <summary>
        /// Event that raises a Message from Processing this Task. 
        /// </summary>
        public event EventHandler<MessageArgs> MessageRecieved;

        /// <summary>
        /// All Devices associated with the Outputs of this Task
        /// </summary>
        public IEnumerable<IDevice> Devices { get; private set; }
        #endregion

        #region [ Constructor ]       
        /// <summary>
        /// Creates a new <see cref="TaskProcessor"/> based on a <see cref="Task"/>
        /// </summary>
        /// <param name="task"> The <see cref="Task"/></param>
        /// <returns></returns>
        public TaskProcessor(AdaptTask task)
        {
            SignalWritter.CleanAppData();
            m_Source = CreateSourceInstance(task.DataSourceModel);

            if (m_Source == null)
                return;

            List<AdaptSignal> inputSignals = task.SignalMappings.SelectMany(s => s.Values).GroupBy((a) => a.ID).Select(g => g.FirstOrDefault()).ToList();
            List<AdaptSignal> sourceSignals = m_Source.GetSignals().ToList();

            if (inputSignals.Select((s) => sourceSignals.FindIndex((ss) => ss.ID == s.ID)).Any(s => s < 0))
            {
                MessageRecieved?.Invoke(this, new MessageArgs($"The following Signals are not found in the datasource: {string.Join(",", inputSignals.Where((s) => sourceSignals.FindIndex((ss) => ss.ID == s.ID) < 0).Select(s => s.Name))}",
                    MessageArgs.MessageLevel.Error));
            }

            m_sourceQueue = Channel.CreateUnbounded<IFrame>();
            m_start = task.Start;
            m_end = task.End;
            m_sourceSignals = inputSignals;
            m_cancelationSource = new CancellationTokenSource();
            m_sectionQueue = new Dictionary<string, List<Channel<IFrame>>>();
            m_writeQueue = Channel.CreateUnbounded<IFrame>();
            m_TemplateInputs = new Dictionary<string, List<AdaptSignal>>();

            m_processors = new Dictionary<string, List<SectionProcessor>>();

            List<AdaptSignal> outputSignals = new List<AdaptSignal>();

            //Frames Per Seconds are computed based on Input Signal FPS
            Dictionary<string, InternalSigDescriptor> signalDesc = new Dictionary<string, InternalSigDescriptor>(inputSignals.Select(item => new KeyValuePair<string, InternalSigDescriptor>(
                item.ID, new InternalSigDescriptor(item.FramesPerSecond,item.Phase,item.Type))));

            if (!(task.TemplateModel is null))
            {
                List<string> templateIDs = task.DeviceMappings.Select(d => Guid.NewGuid().ToString()).ToList();
                m_sectionQueue = templateIDs.ToDictionary((c) => c, (c) => {
                    List<Channel<IFrame>> queues = new List<Channel<IFrame>>();
                    for (int i = 0; i < task.Sections.Count; i++)
                        queues.Add(Channel.CreateUnbounded<IFrame>());
                    return queues;
                });

                Dictionary<Tuple<string, int>, string> deviceKeyMappings = new Dictionary<Tuple<string,int>, string>();
                Dictionary<string, string> variableNames = new Dictionary<string, string>();
                List<string> existingDeviceNames = new List<string>();
                List<AdaptDevice> existingDevices = new List<AdaptDevice>();

                Func<string, string> processVars = (string text) => {
                    if (text.Contains("{"))
                        foreach (KeyValuePair<string, string> var in variableNames)
                            text = text.Replace("{" + var.Key + "}", var.Value);
                    return text;
                };

                for (int i = 0; i < templateIDs.Count; i++)
                {

                    m_processors.Add(templateIDs[i], task.Sections.Select((s, j) =>
                    {
                        Channel<IFrame> inQueue = m_sectionQueue[templateIDs[i]][j];
                        Channel<IFrame> outQueue;
                        if (j < (task.Sections.Count - 1))
                            outQueue = m_sectionQueue[templateIDs[i]][j + 1];
                        else
                            outQueue = m_writeQueue;
                        return new SectionProcessor(inQueue, outQueue, templateIDs[i], s, task, task.SignalMappings[i], signalDesc);
                    }).ToList());


                    variableNames = task.DevicesModels.ToDictionary((d) => d.Name, (d) => task.DeviceMappings[i][d.ID].Name);
                    task.DevicesModels.ForEach((d) =>
                    {
                        task.SignalModels.Where((s) => s.DeviceID == d.ID).ToList().ForEach((s) =>
                        {
                            if (s.DeviceID == d.ID)
                                return;
                            variableNames.Add(d.Name + "." + s.Name, task.SignalMappings[i][d.ID].Name);
                        });
                    });

                    task.DevicesModels.ForEach((d) =>
                    {
                        bool isOut = task.OutputSignalModels.Any((o) => o.IsInputSignal && !(task.SignalModels.Find(s => s.ID == o.SignalID && d.ID == s.DeviceID) is null));
                        isOut = isOut || task.OutputSignalModels.Any((o) => !o.IsInputSignal && task.Sections.SelectMany(sec => sec.Analytics.SelectMany(a => a.OutputModel).Where(s => s.DeviceID == d.ID)).Count() > 0);

                        if (!isOut)
                            return;

                        string name = d.OutputName;
                        if (!variableNames.TryAdd("Name", task.DeviceMappings[i][d.ID].Name))
                            variableNames["Name"] = task.DeviceMappings[i][d.ID].Name;

                        name = processVars(name);

                        if (existingDeviceNames.Contains(name))
                        {
                            deviceKeyMappings.Add(new Tuple<string, int>(templateIDs[i], d.ID), existingDevices.First(item => item.Name == name).ID);
                            return;
                        }

                        existingDeviceNames.Add(name);
                        string key = Guid.NewGuid().ToString();

                        existingDevices.Add(new AdaptDevice(key, name));
                        deviceKeyMappings.Add(new Tuple<string, int>(templateIDs[i], d.ID), key);

                    });
                    Devices = existingDevices;
                }

                outputSignals = templateIDs.SelectMany((id,i) => task.OutputSignalModels.Select(s => {
                    AdaptSignal sig;
                    int deviceID;
                    if (s.IsInputSignal)
                        deviceID = task.SignalModels.Find(sig => sig.ID == s.SignalID).DeviceID;
                    else
                        deviceID = task.Sections.SelectMany(sec => sec.Analytics.SelectMany(a => a.OutputModel).Where(si => si.ID == s.SignalID)).First().DeviceID;

                    if (s.IsInputSignal)
                        sig =  new AdaptSignal(id + "-" + task.SignalMappings[i][s.SignalID].ID, task.SignalMappings[i][s.SignalID]);
                    else
                        sig = new AdaptSignal(id + "AnalyticOutput-" + s.SignalID, s.Name, "", signalDesc[id + "AnalyticOutput-" + s.SignalID].FramesPerSecond);
                    sig.Device = deviceKeyMappings[new Tuple<string,int>(id, deviceID)];

                    if (!variableNames.TryAdd("Name", sig.Name))
                        variableNames["Name"] = sig.Name;

                    if (!variableNames.TryAdd("DeviceName", task.DeviceMappings[i][deviceID].Name))
                        variableNames["DeviceName"] = task.DeviceMappings[i][deviceID].Name;

                    sig.Name = processVars(s.Name);
                    if (!s.IsInputSignal)
                    {
                        sig.Type = signalDesc[id + "AnalyticOutput-" + s.SignalID].Type;
                        sig.Phase = signalDesc[id + "AnalyticOutput-" + s.SignalID].Phase;
                    }

                    return sig;
                })).ToList();

                m_TemplateInputs = templateIDs.Select((k,i) => new { Key=k, Val=task.SignalMappings[i].Values.ToList() }).ToDictionary((i) => i.Key,(i) => i.Val);

            }
            else
            {
                outputSignals = task.SignalMappings.SelectMany(s => s.Values).GroupBy((a) => a.ID).Select(g => g.FirstOrDefault()).ToList();
                List<string> deviceIDs = outputSignals.Select(item => item.Device).Distinct().ToList();
                Devices = m_Source.GetDevices().Where(d => deviceIDs.Contains(d.ID));
            }

            if (m_processors.SelectMany(item => item.Value).Where(item => item.FramesPerSecond > 0).Any())
                m_commonFrameRate = TimeAlignment.Combine(m_processors.SelectMany(item => item.Value).Select(item => item.FramesPerSecond).Where(fps => fps > 0).ToArray());
            else
                m_commonFrameRate = TimeAlignment.Combine(inputSignals.Select(item => item.FramesPerSecond).ToArray());

            m_writers = new ConcurrentDictionary<string, SignalWritter>(outputSignals.ToDictionary(signal => signal.ID, signal => new SignalWritter(signal)));
              
        }
        #endregion

        #region [ Methods ]
        private IDataSource CreateSourceInstance(DataSource source)
        {
            try
            {
                Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(source.AssemblyName));
                Type type = assembly.GetType(source.TypeName);
                IDataSource dataSource = (IDataSource)Activator.CreateInstance(type);
                IConfiguration config = new ConfigurationBuilder().AddGemstoneConnectionString(source.ConnectionString).Build();
                dataSource.Configure(config);
                return dataSource;
            }
            catch (Exception ex)
            {
                MessageRecieved?.Invoke(this, new MessageArgs("An error occurred creating the DataSource", ex, MessageArgs.MessageLevel.Error));
                return null;
            }

        }

        /// <summary>
        /// Starts the Task
        /// </summary>
        /// <returns> A Task</returns>
        public Task StartTask()
        {
            if (m_Source == null)
                return new Task<bool>(() => false);

            m_mainProcess = Task.Run(() =>
            {

                Task getData = Task.Run(() => GetData(m_cancelationSource.Token));
                Task[] processTask = m_processors.SelectMany(item => item.Value.Select(p => p.StartProcessor(m_cancelationSource.Token))).ToArray();

                Task writeData;
                Task distributeData;

               writeData = Task.Run(() => WriteData(m_cancelationSource.Token));
               distributeData = Task.Run(() => DistributeTemplates(m_cancelationSource.Token));

                Task[] writterTasks = m_writers.Select(item => item.Value.StartWritter(m_cancelationSource.Token)).ToArray();

                getData.Wait();
                distributeData.Wait();
                Task.WaitAll(processTask);

                writeData.Wait();
                Task.WaitAll(writterTasks);

                ReportComplete();
            });

          
            return m_mainProcess;
        }

        private async void GetData(CancellationToken cancelationToken)
        {
            try
            { 
                m_Source.MessageRecieved += ProcessMessage;
            
                int count = 0;
                if (!m_Source.SupportProgress)
                {
                    ProgressArgs args = new ProgressArgs("This DataSource does not support Progress updates.", false, (int)50);
                    ReportProgress?.Invoke(this, args);
                }

                await foreach (IFrame frame in m_Source.GetData(m_sourceSignals, m_start, m_end))
                {
                    frame.Timestamp = Ticks.AlignToMicrosecondDistribution(frame.Timestamp, m_commonFrameRate);
                    await m_sourceQueue.Writer.WriteAsync(frame, cancelationToken);
                    count++;
                    if (count % 1000 == 0 && m_Source.SupportProgress)
                        ReportDatasourceProgress(m_Source.GetProgress());
                }
            }
            catch (Exception ex)
            {
                MessageRecieved?.Invoke(this, new MessageArgs("An Error occurred when trying to load data.", ex, MessageArgs.MessageLevel.Error));
            }
            finally
            {
                MessageRecieved?.Invoke(this, new MessageArgs("Finished Loading Data from DataSource", MessageArgs.MessageLevel.Info));
                m_sourceQueue.Writer.Complete();
            }
        }

        private async void DistributeTemplates(CancellationToken cancellationToken)
        {
            int Nprocesssed = 0;
            try
            {
                IFrame frame;
                
                while (await m_sourceQueue.Reader.WaitToReadAsync(cancellationToken))
                {
                    if (!m_sourceQueue.Reader.TryRead(out frame))
                        continue;
                    Nprocesssed++;

                    if (m_sectionQueue.Count > 0)
                        foreach (KeyValuePair<string,List<Channel<IFrame>>> template in m_sectionQueue)
                        {
                            List<string> signals = m_TemplateInputs[template.Key].Select(s => s.ID).ToList();
                            IFrame templateFrame = new Frame() {
                                Timestamp = frame.Timestamp,
                                Published = frame.Published,
                                Measurements = new ConcurrentDictionary<string, ITimeSeriesValue>()
                            };

                            // This maps the signal into the corresponding Template queues... 
                            foreach (KeyValuePair<string, ITimeSeriesValue> value in frame.Measurements)
                            {
                                if (!signals.Contains(value.Key))
                                    continue;

                                templateFrame.Measurements.AddOrUpdate(template.Key + "-" + value.Key, (id) => value.Value.Clone(id), (id, original) => value.Value.Clone(id));
                            }

                            template.Value[0].Writer.TryWrite(templateFrame);
                        } 
                    else
                        m_writeQueue.Writer.TryWrite(frame);

                }
            }
            catch (Exception ex)
            {
                MessageRecieved?.Invoke(this, new MessageArgs("An Error occurred when routing Templates.", ex, MessageArgs.MessageLevel.Error));
            }
            finally
            {
                if (m_sectionQueue.Count > 0)
                    foreach (KeyValuePair<string, List<Channel<IFrame>>> template in m_sectionQueue)
                        template.Value[0].Writer.Complete();
                else
                    m_writeQueue.Writer.Complete();
            }
        }

        private async void WriteData(CancellationToken cancelationToken)
        {
            try
            {

                IFrame frame;
                int Nprocesssed = 0;
                while (await m_writeQueue.Reader.WaitToReadAsync(cancelationToken))
                {
                    if (!m_writeQueue.Reader.TryRead(out frame))
                        continue;
                    Nprocesssed++;
                    foreach (KeyValuePair<string, ITimeSeriesValue> value in frame.Measurements)
                    {
                        SignalWritter writer;
                        if (m_writers.TryGetValue(value.Key, out writer))
                            writer.AddPoint(value.Value);
                    }

                   
                }
            }
            catch (Exception ex)
            {
                MessageRecieved?.Invoke(this, new MessageArgs("An error occurred writing data to the temporary files", ex, MessageArgs.MessageLevel.Error));
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

        private void ProcessMessage(object sender, MessageArgs args)
        {
            MessageRecieved?.Invoke(sender, args);
        }
            
         #endregion
    }
}
