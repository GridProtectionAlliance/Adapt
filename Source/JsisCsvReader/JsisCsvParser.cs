using Gemstone;
using Gemstone.Communication;
using Gemstone.Threading.SynchronizedOperations;
using GemstonePhasorProtocolls;
using Gemstone.StringExtensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using GemstoneCommon;
using Gemstone.Threading;
using System.Globalization;

namespace JsisCsvReader
{
    public class JsisCsvParser
    {
        #region [ Members ]


        // Fields
        private bool disposedValue;
        private JsisCsvRowParser m_rowParser;
        private JsisCsvHeaderParser m_headerParser;
        private JsisCsvFileClient m_dataChannel;
        private ShortSynchronizedOperation m_readNextBuffer;
        private bool m_skippedHeader;
        private long m_lastParsingExceptionTime;
        private int m_parsingExceptionCount;
        private string m_connectionString;
        private int m_connectionAttempts;
        private TransportProtocol m_transportProtocol;
        private PrecisionInputTimer m_inputTimer;
        private int m_definedFrameRate;
        private bool m_enabled;
        private SharedTimer m_rateCalcTimer;
        private IConfigurationFrame m_configurationFrame;
        private long m_initialBytesReceived;
        private bool m_initiatingDataStream;
        private int m_maximumConnectionAttempts;
        private int m_frameRateTotal;
        private DateTime m_baseDateTime;
        private string m_device;
        private JsisCsvHeader m_header;
        #endregion

        public int QueuedBuffers => m_rowParser?.QueuedBuffers ?? 0;

        public int QueuedOutputs => throw new NotImplementedException();

        public int RedundantFramesPerPacket => throw new NotImplementedException();

        public IConfigurationFrame ConfigurationFrame { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IConnectionParameters ConnectionParameters { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public CheckSumValidationFrameTypes CheckSumValidationFrameTypes { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <summary>
        /// Gets or sets <see cref="TransportProtocol"/> to use with this <see cref="MultiProtocolFrameParser"/>.
        /// </summary>
        public TransportProtocol TransportProtocol
        {
            get => m_transportProtocol;
            set
            {
                m_transportProtocol = value;
                DeviceSupportsCommands = false;

                //// File based input connections are handled more carefully
                //if (m_transportProtocol != TransportProtocol.File)
                //    return;

                if (m_maximumConnectionAttempts < 1)
                    m_maximumConnectionAttempts = 1;
            }
        }
        public bool TrustHeaderLength { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool Enabled { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string Name => throw new NotImplementedException();

        public string Status => throw new NotImplementedException();

        /// <summary>
        /// Gets or sets the key/value pair based connection information required by the <see cref="MultiProtocolFrameParser"/> to connect to a device.
        /// </summary>
        public string ConnectionString
        {
            get => m_connectionString;
            set
            {
                m_connectionString = value;

                // Parse connection string to get base datetime and device of the csv file
                Dictionary<string, string> settings = m_connectionString.ParseKeyValuePairs();
                if(settings.TryGetValue("file", out string filename))
                {
                    m_baseDateTime = m_getFileDateTime(filename);
                    m_device = m_getPMUName(filename);
                }
            }
        }

        private string m_getPMUName(string filename)
        {
            return Path.GetFileNameWithoutExtension(filename).Split('_')[0];
        }

        public Action<object, EventArgs> ConnectionTerminated { get; set; }
        public int DefinedFrameRate { get; set; }
        /// <summary>
        /// Gets or sets flag that determines if a high-resolution precision timer should be used for file based input.
        /// </summary>
        /// <remarks>
        /// Useful when input frames need be accurately time-aligned to the local clock to better simulate
        /// an input device and calculate downstream latencies.<br/>
        /// This is only applicable when connection is made to a file for replay purposes.
        /// </remarks>
        public bool UseHighResolutionInputTimer
        {
            get => !(m_inputTimer is null);
            set
            {
                // Note that a 1-ms timer and debug mode don't mix, so the high-resolution timer is disabled while debugging
                if (value && m_inputTimer is null && !Debugger.IsAttached)
                    m_inputTimer = PrecisionInputTimer.Attach(m_definedFrameRate, OnParsingException);
                else if (!value && !(m_inputTimer is null))
                    PrecisionInputTimer.Detach(ref m_inputTimer);
            }
        }

        /// <summary>
        /// Gets or sets number of parsing exceptions allowed during <see cref="ParsingExceptionWindow"/> before connection is reset.
        /// Defaults to <see cref="DefaultAllowedParsingExceptions"/>.
        /// </summary>
        public int AllowedParsingExceptions { get; set; } = DefaultAllowedParsingExceptions;
        /// <summary>
        /// Gets or sets time duration, in <see cref="Ticks"/>, to monitor parsing exceptions.
        /// Defaults to <see cref="DefaultParsingExceptionWindow"/>.
        /// </summary>
        public Ticks ParsingExceptionWindow { get; set; } = DefaultParsingExceptionWindow;

        public void GetHeaders()
        {

            // Stop parser if it is already running - thus calling start after already started will have the effect
            // of "restarting" the parsing engine...
            Stop();

            // Parse connection string to check for special parameters
            Dictionary<string, string> settings = m_connectionString.ParseKeyValuePairs();

            // Reset connection attempt counter
            m_connectionAttempts = 0;

            // Validate that the high-precision input timer is necessary
            if (m_transportProtocol != TransportProtocol.File && UseHighResolutionInputTimer)
                UseHighResolutionInputTimer = false;
            m_headerParser = new JsisCsvHeaderParser();
            m_headerParser.BaseDateTime = m_baseDateTime;
            m_headerParser.Device = m_device;
            m_headerParser.HeaderParsingDone += m_headerParser_ReceiveCsvHeader;
            //m_rowParser.ReceivedDataFrame += m_frameParser_ReceivedDataFrame;
            // Start parsing engine
            //m_rowParser.Start();
            m_dataChannel = new JsisCsvFileClient();
            JsisCsvFileClient fileClient = m_dataChannel;
            fileClient.FileOpenMode = FileMode.Open;
            fileClient.FileAccessMode = FileAccess.Read;
            fileClient.FileShareMode = FileShare.Read;
            fileClient.ReceiveOnDemand = true;
            fileClient.ReceiveBufferSize = 1;
            //fileClient.AutoRepeat = AutoRepeatCapturedPlayback;
            m_skippedHeader = false;
            // Setup synchronized read operation for file client operations
            m_readNextBuffer = new ShortSynchronizedOperation(ReadHeader, ex => OnParsingException(new InvalidOperationException($"Encountered an exception while reading file data: {ex.Message}", ex)));

            if (!(m_dataChannel is null))
            {
                // Setup event handlers
                m_dataChannel.ConnectionEstablished += m_dataChannel_ConnectionEstablished;
                m_dataChannel.ReceiveCsvHeader += m_dataChannel_ReceiveCsvHeader;
                m_dataChannel.ConnectionString = m_connectionString;
                m_dataChannel.ConnectAsync();
            }
        }

        private void m_headerParser_ReceiveCsvHeader(object sender, EventArgs<JsisCsvHeader> e)
        {
            m_header = e.Argument;
            ReceivedHeaderFrame?.Invoke(this, e);
        }

        private void m_dataChannel_ReceiveCsvHeader(object sender, EventArgs<List<string[]>> e)
        {
            m_headerParser.ParseHeader(e);
            //EventArgs<JsisCsvHeader> outputArgs = new EventArgs<JsisCsvHeader>(m_header);
            //ReceivedHeaderFrame?.Invoke(this, outputArgs);
            //string[] signalNames = e.Argument1;
            //string[] signalTypes = e.Argument2;
            //string[] signalUnits = e.Argument3;             
        }

        private void ReadHeader()
        {
            if (!(m_dataChannel is JsisCsvFileClient fileClient))
                return;

            if (fileClient.CurrentState == ClientState.Connected)
                fileClient.ReadHeader();
        }
        private DateTime m_getFileDateTime(string filename)
        {
            var nameStrings = Path.GetFileNameWithoutExtension(filename).Split('_');
            DateTime date = DateTime.MinValue;
            TimeSpan time = TimeSpan.Zero;
            string dateStr = "", timeStr = "";
            foreach (var str in nameStrings)
            {
                if (str.Length == 8)
                {
                    try
                    {
                        date = DateTime.ParseExact(str, "yyyyMMdd", CultureInfo.InvariantCulture);
                        dateStr = str;
                    }
                    catch (Exception)
                    {

                    }
                }
                else if (str.Length == 6)
                {
                    try
                    {
                        time = TimeSpan.ParseExact(str, "hhmmss", CultureInfo.InvariantCulture);
                        timeStr = str;
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
            //DateTime rslt = DateTime.ParseExact(dateStr + "_" + timeStr, "yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            return date.Add(time);
        }
        //private double m_getSamplingRate()
        //{
        //    return 0d;
        //}
        /// <summary>
        /// Gets or sets flag to disable real-time data on stop.
        /// Defaults to <see cref="DefaultDisableRealTimeDataOnStop"/>.
        /// </summary>
        /// <remarks>
        /// If <c>false</c>, disable real-time command will not be sent when parser is stopped regardless
        /// of <see cref="SkipDisableRealTimeData"/> value.
        /// </remarks>
        public bool DisableRealTimeDataOnStop { get; set; } = DefaultDisableRealTimeDataOnStop;
        /// <summary>
        /// Gets or sets flag that determines if a device supports commands.
        /// </summary>
        /// <remarks>
        /// This property is automatically derived based on the selected <see cref="PhasorProtocol"/>, <see cref="TransportProtocol"/>
        /// and <see cref="ConnectionString"/>, but can be overridden if the consumer already knows that a device supports commands.
        /// </remarks>
        public bool DeviceSupportsCommands { get; set; } = false;
        /// <summary>
        /// Gets or sets flag to automatically send the ConfigFrame2 and EnableRealTimeData command frames used to start a typical data parsing sequence.
        /// Defaults to <see cref="DefaultAutoStartDataParsingSequence"/>.
        /// </summary>
        /// <remarks>
        /// For devices that support IEEE commands, setting this property to true will automatically start the data parsing sequence.
        /// </remarks>
        public bool AutoStartDataParsingSequence { get; set; } = DefaultAutoStartDataParsingSequence;
        /// <summary>
        /// Gets or sets flag to skip automatic disabling of the real-time data stream on shutdown or startup.
        /// Defaults to <see cref="DefaultSkipDisableRealTimeData"/>.
        /// </summary>
        /// <remarks>
        /// This flag may important when using UDP multicast with several subscribed clients.
        /// </remarks>
        public bool SkipDisableRealTimeData { get; set; } = DefaultSkipDisableRealTimeData;
        /// <summary>
        /// Specifies the default value for the <see cref="ParsingExceptionWindow"/> property.
        /// </summary>
        public const long DefaultParsingExceptionWindow = 50000000L; // 5 seconds
        /// <summary>
        /// Specifies the default value for the <see cref="AllowedParsingExceptions"/> property.
        /// </summary>
        public const int DefaultAllowedParsingExceptions = 10;
        /// <summary>
        /// Specifies the default value for the <see cref="SkipDisableRealTimeData"/> property.
        /// </summary>
        public const bool DefaultSkipDisableRealTimeData = false;
        /// <summary>
        /// Specifies the default value for the <see cref="DisableRealTimeDataOnStop"/> property.
        /// </summary>
        public const bool DefaultDisableRealTimeDataOnStop = true;
        /// <summary>
        /// Specifies the default value for the <see cref="AutoStartDataParsingSequence"/> property.
        /// </summary>
        public const bool DefaultAutoStartDataParsingSequence = true;

        public event EventHandler<EventArgs<ICommandFrame>> ReceivedCommandFrame;
        public event EventHandler<EventArgs<IConfigurationFrame>> ReceivedConfigurationFrame;
        public event EventHandler<EventArgs<JsisCsvDataRow>> ReceivedDataFrame;
        public event EventHandler<EventArgs<JsisCsvHeader>> ReceivedHeaderFrame;
        public event EventHandler<EventArgs<IChannelFrame>> ReceivedUndeterminedFrame;
        public event EventHandler<EventArgs<FundamentalFrameType, int>> ReceivedFrameImage;
        public event EventHandler<EventArgs<FundamentalFrameType, byte[], int, int>> ReceivedFrameBufferImage;
        public event EventHandler ConfigurationChanged;
        public event EventHandler<EventArgs<Exception>> ParsingException;
        public event EventHandler BufferParsed;
        /// <summary>
        /// Occurs when number of parsing exceptions exceed <see cref="AllowedParsingExceptions"/> during <see cref="ParsingExceptionWindow"/>.
        /// </summary>
        public event EventHandler ExceededParsingExceptionThreshold;
        /// <summary>
        /// Occurs when <see cref="MultiProtocolFrameParser"/> has established a connection to a device.
        /// </summary>
        public event EventHandler ConnectionEstablished;

        public JsisCsvParser()
        {

            m_rateCalcTimer = TimerScheduler.CreateTimer();
            //m_rateCalcTimer.Elapsed += m_rateCalcTimer_Elapsed;
            m_rateCalcTimer.Interval = 5000;
            m_rateCalcTimer.AutoReset = true;
            m_rateCalcTimer.Enabled = false;

            m_skippedHeader = false;
        }
        public void Parse(SourceChannel source, byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public void Start()
        {

            // Stop parser if it is already running - thus calling start after already started will have the effect
            // of "restarting" the parsing engine...
            Stop();

            // Parse connection string to check for special parameters
            Dictionary<string, string> settings = m_connectionString.ParseKeyValuePairs();
            m_header = new JsisCsvHeader(m_device);
            // Reset connection attempt counter
            m_connectionAttempts = 0;

            // Validate that the high-precision input timer is necessary
            if (m_transportProtocol != TransportProtocol.File && UseHighResolutionInputTimer)
                UseHighResolutionInputTimer = false;
            m_rowParser = new JsisCsvRowParser(m_device);
            m_rowParser.BaseDateTime = m_baseDateTime;
            //m_rowParser.Device = m_device;
            //m_headerParser.HeaderParsingDone += m_headerParser_ReceiveCsvHeader;
            m_rowParser.DataParsed += m_frameParser_ReceivedDataFrame;
            m_rowParser.HeaderParsed += m_rowPraser_receivedHeaderRow;
            m_rowParser.ReadNextRow += m_frameParser_ReceivedHeaderRow;
            // Start parsing engine
            m_rowParser.Start();
            m_dataChannel = new JsisCsvFileClient();
            JsisCsvFileClient fileClient = m_dataChannel;
            fileClient.FileOpenMode = FileMode.Open;
            fileClient.FileAccessMode = FileAccess.Read;
            fileClient.FileShareMode = FileShare.Read;
            fileClient.ReceiveOnDemand = true;
            fileClient.ReceiveBufferSize = 1;
            //fileClient.AutoRepeat = AutoRepeatCapturedPlayback;
            m_skippedHeader = false;
            // Setup synchronized read operation for file client operations
            m_readNextBuffer = new ShortSynchronizedOperation(ReadNextFileBuffer, ex => OnParsingException(new InvalidOperationException($"Encountered an exception while reading file data: {ex.Message}", ex)));

            if (!(m_dataChannel is null))
            {
                // Setup event handlers
                m_dataChannel.ConnectionEstablished += m_dataChannel_ConnectionEstablished;
                m_dataChannel.ReceiveCsvData += m_dataChannel_ReceiveData;
                m_dataChannel.ConnectionString = m_connectionString;
                m_dataChannel.ConnectAsync();
            }
        }

        private void m_frameParser_ReceivedHeaderRow(object sender, EventArgs e)
        {
            m_readNextBuffer?.TryRunAsync();
        }

        private void m_rowPraser_receivedHeaderRow(object sender, EventArgs<int, Dictionary<int, string>> e)
        {
            int rowNumber = e.Argument1;
            Dictionary<int, string> fields = e.Argument2;
            if (rowNumber == 1)
            {
                foreach (var item in fields)
                {
                    int columnNumber = item.Key;
                    string channelName = item.Value;
                    if (!m_header.ColumnSignalDict.ContainsKey(columnNumber))
                    {

                    }
                }
            }
        }

        private void ReadNextFileBuffer()
        {
            if (!(m_dataChannel is JsisCsvFileClient fileClient))
                return;

            if (fileClient.CurrentState == ClientState.Connected)
                fileClient.ReadNextBuffer();
        }
        private void OnParsingException(Exception ex)
        {
            if (!(ex is ThreadAbortException) && !(ex is ObjectDisposedException))
                ParsingException?.Invoke(this, new EventArgs<Exception>(ex));

            if (DateTime.UtcNow.Ticks - m_lastParsingExceptionTime > ParsingExceptionWindow)
            {
                // Exception window has passed since last exception, so we reset counters
                m_lastParsingExceptionTime = DateTime.UtcNow.Ticks;
                m_parsingExceptionCount = 0;
            }

            m_parsingExceptionCount++;

            if (m_parsingExceptionCount <= AllowedParsingExceptions)
                return;

            try
            {
                // When the parsing exception threshold has been exceeded, connection is stopped
                Stop();
            }
            finally
            {
                // Notify consumer of parsing exception threshold deviation
                OnExceededParsingExceptionThreshold();
                m_lastParsingExceptionTime = 0;
                m_parsingExceptionCount = 0;
            }
        }
        /// <summary>
        /// Raises the <see cref="ParsingException"/> event.
        /// </summary>
        /// <param name="innerException">Actual exception to send as inner exception to <see cref="ParsingException"/> event.</param>
        /// <param name="message">Message of new exception to send to <see cref="ParsingException"/> event.</param>
        /// <param name="args">Arguments of message of new exception to send to <see cref="ParsingException"/> event.</param>
        private void OnParsingException(Exception innerException, string message, params object[] args)
        {
            if (!(innerException is ThreadAbortException) && !(innerException is ObjectDisposedException))
                OnParsingException(new Exception(string.Format(message, args), innerException));
        }
        /// <summary>
        /// Raises the <see cref="ExceededParsingExceptionThreshold"/> event.
        /// </summary>
        private void OnExceededParsingExceptionThreshold() => ExceededParsingExceptionThreshold?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// Stops the <see cref="MultiProtocolFrameParser"/>.
        /// </summary>
        public void Stop()
        {
            m_enabled = false;
            m_rateCalcTimer.Enabled = false;
            m_configurationFrame = null;

            // Make sure data stream is disabled
            if (!SkipDisableRealTimeData && DisableRealTimeDataOnStop)
            {
                //WaitHandle commandWaitHandle = SendDeviceCommand(DeviceCommand.DisableRealTimeData);
                //commandWaitHandle?.WaitOne(1000);
            }

            if (!(m_dataChannel is null))
            {
                try
                {
                    m_dataChannel.Disconnect();
                }
                catch (Exception ex)
                {
                    OnParsingException(new ConnectionException($"Failed to properly disconnect data channel: {ex.Message}", ex));
                }
                finally
                {
                    m_dataChannel.ConnectionEstablished -= m_dataChannel_ConnectionEstablished;
                    //m_dataChannel.ConnectionEstablished -= m_dataChannel_ConnectionEstablished;
                    //m_dataChannel.ConnectionAttempt -= m_dataChannel_ConnectionAttempt;
                    //m_dataChannel.ConnectionException -= m_dataChannel_ConnectionException;
                    //m_dataChannel.ConnectionTerminated -= m_dataChannel_ConnectionTerminated;
                    m_dataChannel.ReceiveCsvData -= m_dataChannel_ReceiveData;
                    //m_dataChannel.ReceiveDataException -= m_dataChannel_ReceiveDataException;
                    //m_dataChannel.SendDataException -= m_dataChannel_SendDataException;
                    m_dataChannel.Dispose();
                }

                m_dataChannel = null;
            }

            m_readNextBuffer = null;

            if (!(m_rowParser is null))
            {
                try
                {
                    m_rowParser.Stop();
                }
                catch (Exception ex)
                {
                    OnParsingException(ex, "Failed to properly stop csv row parser: {0}", ex.Message);
                }
                finally
                {
                    m_rowParser.DataParsed -= m_frameParser_ReceivedDataFrame;
                    //m_rowParser.ReceivedCommandFrame -= m_frameParser_ReceivedCommandFrame;
                    //m_rowParser.ReceivedConfigurationFrame -= m_frameParser_ReceivedConfigurationFrame;
                    //m_rowParser.ReceivedDataFrame -= m_frameParser_ReceivedDataFrame;
                    //m_rowParser.ReceivedHeaderFrame -= m_frameParser_ReceivedHeaderFrame;
                    //m_rowParser.ReceivedUndeterminedFrame -= m_frameParser_ReceivedUndeterminedFrame;
                    //m_rowParser.ReceivedFrameImage -= m_frameParser_ReceivedFrameImage;
                    //m_rowParser.ConfigurationChanged -= m_frameParser_ConfigurationChanged;
                    //m_rowParser.ParsingException -= m_frameParser_ParsingException;
                    //m_rowParser.BufferParsed -= m_frameParser_BufferParsed;

                    //if (!(ReceivedFrameBufferImage is null))
                    //    m_rowParser.ReceivedFrameBufferImage -= m_frameParser_ReceivedFrameBufferImage;

                    m_rowParser.Dispose();
                }

                m_rowParser = null;
            }
            if (!(m_headerParser is null))
            {
                try
                {
                    m_headerParser.stop();
                }
                catch (Exception ex)
                {
                    OnParsingException(ex, "Failed to properly stop csv header parser: {0}", ex.Message);
                }
                finally
                {
                    m_headerParser.HeaderParsingDone -= m_headerParser_ReceiveCsvHeader;
                }
                m_headerParser = null;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~JsisCsvRowParser()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        private void m_frameParser_ReceivedDataFrame(object sender, EventArgs<JsisCsvDataRow> e)
        {
            //m_frameRateTotal++;

            // We don't stop parsing for exceptions thrown in consumer event handlers
            try
            {
                //bool publishFrame = true;
                JsisCsvDataRow dataFrame = e.Argument;

                DateTime timestamp = dataFrame.Timestamp;

                //if (timestamp >= ReplayStartTime && timestamp < ReplayStopTime)
                //{
                //    MaintainCapturedFrameReplayTiming(dataFrame);
                //}
                //else
                //{
                //    publishFrame = false;

                // Read next buffer if output frames are almost all processed
                if (QueuedBuffers < 2)
                    m_readNextBuffer?.TryRunAsync();
                //}

                //if (publishFrame)
                ReceivedDataFrame?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                OnParsingException(ex, "MultiProtocolFrameParser \"ReceivedDataFrame\" consumer event handler exception: {0}", ex.Message);
            }
        }
        private void m_dataChannel_ReceiveData(object sender, EventArgs<string[], int> e)
        {
            //string[] data = e.Argument1;
            //int line = e.Argument2;
            m_rowParser.ParseDataRow(e);
            
            //// Logic to skip Header on .pdat Files
            //if (!m_skippedHeader)
            //{
            //    m_rowParser.ParseHeaderRow(e.Argument1);


            //    m_skippedHeader = true;

            //    //Parse(SourceChannel.Data, buffer, dataOffset, length - dataOffset);

            //    return;
            //}
            //if (line == 1)
            //{
            //    m_header.SignalNames = data;
            //    m_readNextBuffer?.TryRunAsync();
            //    return;
            //}
            //else if (line == 2)
            //{
            //    m_header.SignalTypes = data;
            //    m_readNextBuffer?.TryRunAsync();
            //    return;
            //}
            //else if (line == 3)
            //{
            //    m_header.SignalUnits = data;
            //    m_readNextBuffer?.TryRunAsync();
            //    return;
            //}
            //else if (line == 4)
            //{
            //    m_header.SignalDescription = data;
            //    m_readNextBuffer?.TryRunAsync();
            //    return;
            //}
            //else
            //{
            //    if (line == 5)
            //    {
            //        m_header.ParseChannels();
            //    }
            //    m_rowParser.ParseDataRow(data, line);
            //}

            //length = m_dataChannel.Read(buffer, 0, length);
            //Parse(SourceChannel.Data, buffer, 0, length);
        }
        private void m_dataChannel_ConnectionEstablished(object sender, EventArgs e)
        {
            // Handle client connection from data channel
            ClientConnectedHandler();

            try
            {
                // Start reading file data
                if (m_transportProtocol == TransportProtocol.File)
                    m_readNextBuffer?.TryRunAsync();
            }
            catch (Exception ex)
            {
                // Process exception for logging
                OnParsingException(new InvalidOperationException($"Failed to queue file read operation due to exception: {ex.Message}", ex));
            }
        }        // Handles needed start-up actions once a client is connected
        private void ClientConnectedHandler()
        {
            try
            {
                ConnectionEstablished?.Invoke(this, EventArgs.Empty);

                if (!DeviceSupportsCommands || !AutoStartDataParsingSequence)
                    return;

                m_initialBytesReceived = 0L;
                m_initiatingDataStream = true;

                // Begin data parsing sequence to handle reception of configuration frame
                Thread startDataParsingThread = new Thread(StartDataParsingSequence)
                {
                    IsBackground = true
                };

                startDataParsingThread.Start();
            }
            catch (Exception ex)
            {
                OnParsingException(ex);
            }
        }

        // Starts data parsing sequence.
        private void StartDataParsingSequence()
        {
            try
            {
                //// Attempt to stop real-time data, waiting a maximum of three seconds for this activity
                //if (!SkipDisableRealTimeData && m_phasorProtocol != PhasorProtocol.IEC61850_90_5)
                //{
                //    // Some devices will only send a config frame once data streaming has been disabled, so
                //    // we use this code to disable real-time data and wait for data to stop streaming...
                //    int attempts = 0;

                //    // Make sure data stream is disabled
                //    SendDeviceCommand(DeviceCommand.DisableRealTimeData);

                //    Thread.Sleep(1000);

                //    // Wait for real-time data stream to cease for up to two seconds
                //    while (m_initialBytesReceived > 0)
                //    {
                //        m_initialBytesReceived = 0;
                //        Thread.Sleep(100);

                //        attempts++;

                //        if (attempts >= 20)
                //            break;
                //    }
                //}

                m_initiatingDataStream = false;

                //// Request configuration frame once real-time data has been disabled. Note that data stream
                //// will be enabled when we receive a configuration frame. 
                //switch (m_phasorProtocol)
                //{
                //    case PhasorProtocol.SelFastMessage:
                //        // SEL Fast Message doesn't define a binary configuration frame so we skip
                //        // requesting one and jump straight to enabling the data stream.
                //        SendDeviceCommand(DeviceCommand.EnableRealTimeData);
                //        break;
                //    case PhasorProtocol.Macrodyne:
                //        // We collect the station name (i.e. the unit ID via 0xBB 0x48)) from the Macrodyne
                //        // protocol interpreted as a header frame before we get the configuration frame
                //        bool sendCommand = true;

                //        //if (m_connectionParameters is Macrodyne.ConnectionParameters parameters)
                //        //sendCommand = parameters.ProtocolVersion != ProtocolVersion.G;

                //        if (sendCommand)
                //            SendDeviceCommand(DeviceCommand.SendHeaderFrame);

                //        break;
                //    default:
                //        // Otherwise we just request the configuration frame
                //        SendDeviceCommand(DeviceCommand.SendConfigurationFrame3);
                //        break;
                //}
            }
            catch (Exception ex)
            {
                OnParsingException(ex);
            }
        }

        //public List<Signal> Read(string filename)
        //{
        //    var signalList = new List<Signal>();
        //    var baseTime = _getFileDateTime(filename);
        //    var timeSpanRelativeToBaseTime = new List<double>();
        //    var timeStamps = new List<DateTime>();
        //    var timeStampeNumberInDays = new List<double>();
        //    //Console.WriteLine(Environment.CurrentDirectory);
        //    using (TextFieldParser reader = new TextFieldParser(filename))
        //    {
        //        reader.TextFieldType = FieldType.Delimited;
        //        reader.SetDelimiters(",");
        //        reader.HasFieldsEnclosedInQuotes = true;


        //        string pmuName = Path.GetFileNameWithoutExtension(filename).Split('_')[0];

        //        List<string> signalNames = reader.ReadFields().Skip(1).ToList();

        //        List<string> signalTypes = reader.ReadFields().Skip(1).ToList();

        //        List<string> signalUnits = reader.ReadFields().Skip(1).ToList();
        //        //DataTable dt = new DataTable();

        //        reader.ReadLine(); //skip the 4th line.
        //        //var data = reader.ReadToEnd();
        //        //var c = data.Length;
        //        //var d = data.ToArray();
        //        //var e = data.Sum(x => x);
        //        //var f = data.Skip(1).ToArray();
        //        for (int i = 0; i < signalNames.Count; i++)
        //        {
        //            var newSignal = new Signal();
        //            newSignal.SignalName = signalNames[i];
        //            newSignal.Unit = signalUnits[i];
        //            newSignal.TypeAbbreviation = _getSignalType(signalTypes[i]);
        //            newSignal.PMUName = pmuName;
        //            newSignal.TimeStampNumber = timeStampeNumberInDays;
        //            newSignal.TimeStamps = timeStamps;
        //            signalList.Add(newSignal);
        //        }
        //        while (!reader.EndOfData)
        //        {
        //            //can read line by line
        //            var row = reader.ReadFields();
        //            _numberOfDataPointInFile++;
        //            //DataRow dr = dt.NewRow();
        //            for (int i = 0; i < row.Length; i++)
        //            {
        //                var success = double.TryParse(row[i], out double value);
        //                if (!success)
        //                {
        //                    value = double.NaN; //if conversion fail, set value to NAN, is this the behaviour we want?
        //                }
        //                if (i == 0)
        //                {
        //                    timeSpanRelativeToBaseTime.Add(value);
        //                    var datetimed = baseTime.AddSeconds(value);
        //                    timeStamps.Add(datetimed); //might need to change if the first time column changes
        //                    timeStampeNumberInDays.Add(datetimed.ToOADate());
        //                }
        //                else
        //                {
        //                    signalList[i - 1].Data.Add(value);
        //                }
        //            }
        //            //dt.Rows.Add(dr);

        //        }
        //        //var a = dt.Columns[0];
        //        var time1 = timeSpanRelativeToBaseTime[0];
        //        var time2 = timeSpanRelativeToBaseTime[1];
        //        _samplingRate = (int)Math.Round((1 / (time2 - time1)) / 10) * 10;
        //        List<UInt16> stats = (new UInt16[timeStamps.Count]).ToList();
        //        //try
        //        //{
        //        //    //double t1 = Convert.ToDouble(time1);

        //        //    //double t2 = Convert.ToDouble(time2);

        //        //    samplingRate = Math.Round((1 / (time1 - time2)) / 10) * 10;
        //        //}

        //        //catch (Exception)
        //        //{

        //        //    //DateTime t1 = DateTime.Parse(time1);

        //        //    //DateTime t2 = DateTime.Parse(time2);

        //        //    //double dif = t2.Subtract(t1).TotalSeconds;

        //        //    //double SamplingRate = Math.Round((1 / dif) / 10) * 10;

        //        //}
        //        foreach (var sig in signalList)
        //        {
        //            sig.SamplingRate = _samplingRate;
        //            sig.Stat = stats;
        //        }
        //    }

        //    //for (var index = 0; index <= signalNames.Count - 1; index++)
        //    //    {
        //    //        var newSignal = new SignalViewModel();
        //    //        newSignal.PMUName = pmuName;
        //    //        newSignal.Unit = signalUnits[index];
        //    //        newSignal.SignalName = signalNames[index];
        //    //        newSignal.SamplingRate = (int)SamplingRate;
        //    //        signalList.Add(signalNames[index]);
        //    //        switch (signalTypes[index])
        //    //        {
        //    //            case "VPM":
        //    //                {
        //    //                    // signalName = signalNames(index).Split(".")(0) & ".VMP"
        //    //                    // signalName = signalNames(index)
        //    //                    newSignal.TypeAbbreviation = "VMP";
        //    //                    break;
        //    //                }

        //    //            case "VPA":
        //    //                {
        //    //                    // signalName = signalNames(index).Split(".")(0) & ".VAP"
        //    //                    // signalName = signalNames(index)
        //    //                    newSignal.TypeAbbreviation = "VAP";
        //    //                    break;
        //    //                }

        //    //            case "IPM":
        //    //                {
        //    //                    // signalName = signalNames(index).Split(".")(0) & ".IMP"
        //    //                    // signalName = signalNames(index)
        //    //                    newSignal.TypeAbbreviation = "IMP";
        //    //                    break;
        //    //                }

        //    //            case "IPA":
        //    //                {
        //    //                    // signalName = signalNames(index).Split(".")(0) & ".IAP"
        //    //                    // signalName = signalNames(index)
        //    //                    newSignal.TypeAbbreviation = "IAP";
        //    //                    break;
        //    //                }

        //    //            case "F":
        //    //                {
        //    //                    // signalName = signalNames(index)
        //    //                    newSignal.TypeAbbreviation = "F";
        //    //                    break;
        //    //                }

        //    //            case "P":
        //    //                {
        //    //                    // signalName = signalNames(index)
        //    //                    newSignal.TypeAbbreviation = "P";
        //    //                    break;
        //    //                }

        //    //            case "Q":
        //    //                {
        //    //                    // signalName = signalNames(index)
        //    //                    newSignal.TypeAbbreviation = "Q";
        //    //                    break;
        //    //                }

        //    //            default:
        //    //                {
        //    //                    throw new Exception("Error! Invalid signal type " + signalTypes[index] + " found in file: " + aFileInfo.ExampleFile + " !");
        //    //                }
        //    //        }
        //    //        newSignal.OldSignalName = newSignal.SignalName;
        //    //        newSignal.OldTypeAbbreviation = newSignal.TypeAbbreviation;
        //    //        newSignal.OldUnit = newSignal.Unit;
        //    //        signalSignatureList.Add(newSignal);
        //    //    }
        //    //    aFileInfo.SignalList = signalList;
        //    //    aFileInfo.TaggedSignals = signalSignatureList;
        //    //    aFileInfo.SamplingRate = (int)SamplingRate;
        //    //    var newSig = new SignalViewModel(aFileInfo.FileDirectory + ", Sampling Rate: " + aFileInfo.SamplingRate + "/Second");
        //    //    newSig.SamplingRate = (int)SamplingRate;
        //    //    var a = new SignalTree(newSig);
        //    //    a.SignalList = SortSignalByPMU(signalSignatureList);
        //    //    GroupedRawSignalsByPMU.Add(a);
        //    //    //newSig = new SignalViewModel(aFileInfo.FileDirectory + ", Sampling Rate: " + aFileInfo.SamplingRate + "/Second");
        //    //    //newSig.SamplingRate = (int)SamplingRate;
        //    //    var b = new SignalTree(newSig);
        //    //    b.SignalList = SortSignalByType(signalSignatureList);
        //    //    GroupedRawSignalsByType.Add(b);
        //    //    ReGroupedRawSignalsByType = GroupedRawSignalsByType;
        //    //}

        //    return signalList;
        //}

        //public List<Signal> Read2(string filename)
        //{
        //    //var l = File.ReadAllLines(filename).Select(x => x.Split(',')).ToArray();
        //    var signalList = new List<Signal>();
        //    var timeSpanRelativeToBaseTime = new List<double>();
        //    var timeStamps = new List<DateTime>();
        //    var timeStampeNumberInDays = new List<double>();
        //    using (TextFieldParser reader = new TextFieldParser(filename))
        //    {
        //        reader.TextFieldType = FieldType.Delimited;
        //        reader.SetDelimiters(",");
        //        reader.HasFieldsEnclosedInQuotes = true;


        //        string pmuName = filename.Split('\\').Last().Split('_')[0];

        //        List<string> signalNames = reader.ReadFields().ToList();

        //        List<string> signalTypes = reader.ReadFields().ToList();

        //        List<string> signalUnits = reader.ReadFields().ToList();
        //        DataTable dt = new DataTable();

        //        reader.ReadLine(); //skip the 4th line.
        //        //var data = reader.ReadToEnd();
        //        //var c = data.Length;
        //        //var d = data.ToArray();
        //        //var e = data.Sum(x => x);
        //        //var f = data.Skip(1).ToArray();

        //        foreach (string header in signalNames)
        //        {
        //            dt.Columns.Add(header);
        //        }

        //        while (!reader.EndOfData)
        //        {
        //            //can read line by line
        //            var row = reader.ReadFields();
        //            DataRow dr = dt.Rows.Add();
        //            //DataRow dr = dt.NewRow();
        //            dr.ItemArray = row;
        //            //for (int i = 0; i < row.Length; i++)
        //            //{
        //            //    dr[i] = row[i];
        //            //}
        //            //dt.Rows.Add(dr);

        //        }
        //        for (int i = 0; i < dt.Columns.Count; i++)
        //        {
        //            if (i == 0)
        //            {
        //                for (int j = 0; j < dt.Rows.Count; j++)
        //                {
        //                    var value = double.Parse(dt.Rows[j][i].ToString());

        //                    timeSpanRelativeToBaseTime.Add(value);
        //                }
        //            }
        //            else
        //            {
        //                var newSignal = new Signal();
        //                newSignal.SignalName = signalNames[i];
        //                newSignal.Unit = signalUnits[i];
        //                newSignal.TypeAbbreviation = _getSignalType(signalTypes[i]);
        //                newSignal.PMUName = pmuName;
        //                newSignal.TimeStampNumber = timeStampeNumberInDays;
        //                newSignal.TimeStamps = timeStamps;
        //                signalList.Add(newSignal);
        //                for (int j = 0; j < dt.Rows.Count; j++)
        //                {
        //                    var value = double.Parse(dt.Rows[j][i].ToString());
        //                    signalList[i - 1].Data.Add(value);
        //                }
        //            }
        //        }
        //        //foreach (var c in dt.Columns)
        //        //{
        //        //    foreach (var r in dt.Rows)
        //        //    {
        //        //        var vv = dt[r][c];
        //        //    }
        //        //}
        //        var ds = dt.DataSet;

        //        var a = dt.Columns[0];
        //        var b = dt.Columns["Time"];
        //        //var r = dt.Rows[0];
        //        //var v = r["Time"];
        //        var time1 = timeSpanRelativeToBaseTime[0];
        //        var time2 = timeSpanRelativeToBaseTime[1];
        //        int samplingRate = (int)Math.Round((1 / (time2 - time1)) / 10) * 10;
        //        foreach (var sig in signalList)
        //        {
        //            sig.SamplingRate = samplingRate;
        //        }

        //        //var time1 = reader.ReadFields()[0];
        //        //var time2 = reader.ReadFields()[0];

        //        //try
        //        //{
        //        //    double t1 = Convert.ToDouble(time1);

        //        //    double t2 = Convert.ToDouble(time2);

        //        //    double SamplingRate = Math.Round((1 / (t2 - t1)) / 10) * 10;
        //        //}

        //        //catch (Exception)
        //        //{

        //        //    DateTime t1 = DateTime.Parse(time1);

        //        //    DateTime t2 = DateTime.Parse(time2);

        //        //    double dif = t2.Subtract(t1).TotalSeconds;

        //        //    double SamplingRate = Math.Round((1 / dif) / 10) * 10;

        //        //}
        //    }


        //    return signalList;
        //}

        //private string _getSignalType(string type)
        //{
        //    switch (type)
        //    {
        //        case "VPM":
        //            {
        //                return "VMP";
        //            }

        //        case "VPA":
        //            {
        //                return "VAP";
        //            }

        //        case "IPM":
        //            {
        //                return "IMP";
        //            }

        //        case "IPA":
        //            {
        //                return "IAP";
        //            }

        //        case "F":
        //            {
        //                return "F";
        //            }

        //        case "P":
        //            {
        //                return "P";
        //            }

        //        case "Q":
        //            {
        //                return "Q";
        //            }

        //        default:
        //            {
        //                return "Other";
        //            }
        //    }
        //}

        //private DateTime _getFileDateTime(string filename)
        //{
        //    var nameStrings = Path.GetFileNameWithoutExtension(filename).Split('_');
        //    DateTime date = DateTime.MinValue;
        //    TimeSpan time = TimeSpan.Zero;
        //    string dateStr = "", timeStr = "";
        //    foreach (var str in nameStrings)
        //    {
        //        if (str.Length == 8)
        //        {
        //            try
        //            {
        //                date = DateTime.ParseExact(str, "yyyyMMdd", CultureInfo.InvariantCulture);
        //                dateStr = str;
        //            }
        //            catch (Exception)
        //            {

        //            }
        //        }
        //        else if (str.Length == 6)
        //        {
        //            try
        //            {
        //                time = TimeSpan.ParseExact(str, "hhmmss", CultureInfo.InvariantCulture);
        //                timeStr = str;
        //            }
        //            catch (Exception ex)
        //            {

        //            }
        //        }
        //    }
        //    //DateTime rslt = DateTime.ParseExact(dateStr + "_" + timeStr, "yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        //    return date.Add(time);
        //}

        //public int GetSamplingRate()
        //{
        //    return _samplingRate;
        //}

        //public int GetNumberOfDataPointInFile()
        //{
        //    return _numberOfDataPointInFile;
        //}

        //public List<DateTime> ReadSignatureCSV(string filename)
        //{
        //    var startTimes = new List<DateTime>();
        //    var endTimes = new List<DateTime>();
        //    //var baseTime = _getFileDateTime(filename);
        //    //var timeSpanRelativeToBaseTime = new List<double>();
        //    //var timeStamps = new List<DateTime>();
        //    //var timeStampeNumberInDays = new List<double>();
        //    //Console.WriteLine(Environment.CurrentDirectory);
        //    using (TextFieldParser reader = new TextFieldParser(filename))
        //    {
        //        reader.TextFieldType = FieldType.Delimited;
        //        reader.SetDelimiters(",");
        //        reader.HasFieldsEnclosedInQuotes = true;


        //        //string pmuName = Path.GetFileNameWithoutExtension(filename).Split('_')[0];

        //        List<string> signalNames = reader.ReadFields().Skip(1).ToList();

        //        List<string> signalTypes = reader.ReadFields().Skip(1).ToList();

        //        List<string> signalUnits = reader.ReadFields().Skip(1).ToList();

        //        //reader.ReadLine(); //skip the 4th line.
        //        //for (int i = 0; i < signalNames.Count; i++)
        //        //{
        //        //    var newSignal = new Signal();
        //        //    newSignal.SignalName = signalNames[i];
        //        //    newSignal.Unit = signalUnits[i];
        //        //    newSignal.TypeAbbreviation = _getSignalType(signalTypes[i]);
        //        //    newSignal.PMUName = pmuName;
        //        //    newSignal.TimeStampNumber = timeStampeNumberInDays;
        //        //    newSignal.TimeStamps = timeStamps;
        //        //    signalList.Add(newSignal);
        //        //}
        //        while (!reader.EndOfData)
        //        {
        //            //can read line by line
        //            var row = reader.ReadFields();
        //            _numberOfDataPointInFile++;
        //            var start = DateTime.ParseExact(row[0], "yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture);
        //            startTimes.Add(start);
        //            var end = DateTime.ParseExact(row[1], "yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture);
        //            endTimes.Add(end);
        //            //DataRow dr = dt.NewRow();
        //            //for (int i = 0; i < row.Length; i++)
        //            //{
        //            //    var success = double.TryParse(row[i], out double value);
        //            //    if (!success)
        //            //    {
        //            //        value = double.NaN; //if conversion fail, set value to NAN, is this the behaviour we want?
        //            //    }
        //            //    if (i == 0)
        //            //    {
        //            //        timeSpanRelativeToBaseTime.Add(value);
        //            //        var datetimed = baseTime.AddSeconds(value);
        //            //        timeStamps.Add(datetimed); //might need to change if the first time column changes
        //            //        timeStampeNumberInDays.Add(datetimed.ToOADate());
        //            //    }
        //            //    else
        //            //    {
        //            //        signalList[i - 1].Data.Add(value);
        //            //    }
        //            //}
        //            //dt.Rows.Add(dr);

        //        }
        //        //var a = dt.Columns[0];
        //        //var time1 = timeSpanRelativeToBaseTime[0];
        //        //var time2 = timeSpanRelativeToBaseTime[1];
        //        //_samplingRate = (int)Math.Round((1 / (time2 - time1)) / 10) * 10;

        //        //foreach (var sig in signalList)
        //        //{
        //        //    sig.SamplingRate = _samplingRate;
        //        //}
        //    }


        //    return startTimes;
        //}
        #region [ Static ]

        private static readonly SharedTimerScheduler TimerScheduler;

        static JsisCsvParser()
        {
            TimerScheduler = new SharedTimerScheduler();
        }

        #endregion
    }
}
