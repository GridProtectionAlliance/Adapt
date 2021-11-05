﻿using Gemstone;
using Gemstone.EventHandlerExtensions;
using Gemstone.Threading.Collections;
using GemstonePhasorProtocolls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsisCsvReader
{
    public class JsisCsvRowParser : IFrameParser
    {
        #region [ Members ]
        // Fields
        private readonly AsyncQueue<EventArgs<JsisCsvDataRow>> m_outputQueue;
        private readonly AsyncDoubleBufferedQueue<List<string>> m_bufferQueue;
        //private readonly List<JsisCsvDataRow> m_parsedSourceData;
        private bool m_disposed;
        private bool m_enabled;
        // Events

        /// <summary>
        /// Occurs when a data image is deserialized successfully to one or more of the output types that the data image represents.
        /// </summary>
        /// <remarks>
        /// <see cref="EventArgs{T1,T2}.Argument1"/> is the ID of the source for the data image.<br/>
        /// <see cref="EventArgs{T1,T2}.Argument2"/> is a list of objects deserialized from the data image.
        /// </remarks>
        [Description("Occurs when a data image is deserialized successfully to one or more object of the Type that the data image was for.")]
        public event EventHandler<EventArgs<IList<JsisCsvDataRow>>>? SourceDataParsed;
        public event EventHandler<EventArgs<ICommandFrame>> ReceivedCommandFrame;
        public event EventHandler<EventArgs<IConfigurationFrame>> ReceivedConfigurationFrame;
        public event EventHandler<EventArgs<IDataFrame>> ReceivedDataFrame;
        public event EventHandler<EventArgs<IHeaderFrame>> ReceivedHeaderFrame;
        public event EventHandler<EventArgs<IChannelFrame>> ReceivedUndeterminedFrame;
        public event EventHandler<EventArgs<FundamentalFrameType, int>> ReceivedFrameImage;
        public event EventHandler<EventArgs<FundamentalFrameType, byte[], int, int>> ReceivedFrameBufferImage;
        public event EventHandler ConfigurationChanged;
        public event EventHandler<EventArgs<Exception>> ParsingException;
        public event EventHandler BufferParsed;

        /// <summary>
        /// Occurs when a data image is deserialized successfully to one of the output types that the data
        /// image represents.
        /// </summary>
        /// <remarks>
        /// <see cref="EventArgs{T}.Argument"/> is the object that was deserialized from the binary image.
        /// </remarks>
        public event EventHandler<EventArgs<JsisCsvDataRow>>? DataParsed;
        #endregion


        #region [ Constructors ]

        /// <summary>
        /// Creates a new instance of the <see cref="MultiSourceFrameImageParserBase{TSourceIdentifier,TTypeIdentifier,TOutputType}"/> class.
        /// </summary>
        public JsisCsvRowParser()
        {
            m_bufferQueue = new AsyncDoubleBufferedQueue<List<string>>
            {
                ProcessItemsFunction = ParseQueuedBuffers
            };

            m_bufferQueue.ProcessException += ProcessExceptionHandler;

            m_outputQueue = new AsyncQueue<EventArgs<JsisCsvDataRow>>
            {
                ProcessItemFunction = PublishParsedOutput
            };

            m_outputQueue.ProcessException += m_parsedOutputQueue_ProcessException;

            //if (ProtocolUsesSyncBytes)
            //    m_sourceInitialized = new ConcurrentDictionary<TSourceIdentifier, bool>();

            //m_unparsedBuffers = new ConcurrentDictionary<TSourceIdentifier, byte[]?>();
            //m_parsedSourceData = new List<JsisCsvDataRow>();
        }

        #endregion

        public int QueuedBuffers => m_bufferQueue.Count;

        public int QueuedOutputs => throw new NotImplementedException();

        public int RedundantFramesPerPacket => throw new NotImplementedException();

        public IConfigurationFrame ConfigurationFrame { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IConnectionParameters ConnectionParameters { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public CheckSumValidationFrameTypes CheckSumValidationFrameTypes { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool TrustHeaderLength { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        /// <summary>
        /// Gets or sets a boolean value that indicates whether the data parser is currently enabled.
        /// </summary>
        /// <remarks>
        /// Setting <see cref="Enabled"/> to true will start the <see cref="BinaryImageParserBase"/> if it is not started,
        /// setting to false will stop the <see cref="BinaryImageParserBase"/> if it is started.
        /// </remarks>
        public bool Enabled
        {
            get => m_enabled;
            set
            {
                if (value && !m_enabled)
                    Start();
                else if (!value && m_enabled)
                    Stop();
                m_outputQueue.Enabled = value;
            }
        }
        public string Name => throw new NotImplementedException();

        public string Status => throw new NotImplementedException();
        public DateTime BaseDateTime { get; set; }
        public string Device { get; internal set; }


        #region [ Methods ]
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Parse(SourceChannel source, byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            m_enabled = true;
            Console.WriteLine("overriding base start method in iframparser.cs");
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
        // This method is used to process all queued data buffers.
        private void ParseQueuedBuffers(IList<List<string>> buffers)
        {
            if (!Enabled)
                return;

            try
            {
                // Process all queued data buffers...
                foreach (List<string> buffer in buffers)
                {
                    //// Track current buffer source
                    //m_source = buffer.Source;

                    //// Check to see if this data source has been initialized
                    //if (m_sourceInitialized != null)
                    //    StreamInitialized = m_sourceInitialized.GetOrAdd(m_source, true);

                    //// Restore any unparsed buffers for this data source, if any
                    //UnparsedBuffer = m_unparsedBuffers.GetOrAdd(m_source, (byte[]?)null);

                    // Start parsing sequence for this buffer - this will begin publication of new parsed outputs
                    //if (buffer.Buffer != null && buffer.Count > 0)
                    JsisCsvDataRow newRow = new JsisCsvDataRow();
                    ParseRow(buffer, newRow);
                    OnDataParsed(newRow);
                    //// Track last unparsed buffer for this data source
                    //m_unparsedBuffers[m_source] = UnparsedBuffer;
                }
            }
            catch
            {

            }
            //finally
            //{
            //    // If user has attached to SourceDataParsed event, expose list of parsed data per source
            //    if (SourceDataParsed != null)
            //    {
            //        foreach (JsisCsvDataRow parsedData in m_parsedSourceData)
            //            OnSourceDataParsed(m_parsedSourceData);

            //        // Clear parsed data dictionary for next pass - note that this does not contend with
            //        // OnDataParsed override since the event called synchronously via base.Write above.
            //        m_parsedSourceData.Clear();
            //    }
            //}
        }

        private void OnDataParsed(JsisCsvDataRow row)
        {
            EventArgs<JsisCsvDataRow> outputArgs = new EventArgs<JsisCsvDataRow>(row);
            // Queue-up parsed output for publication
            m_outputQueue.Enqueue(outputArgs);

            if (SourceDataParsed == null)
                return;

            // If user has attached to SourceDataParsed event, track parsed data per source
            //List<JsisCsvDataRow> sourceData = m_parsedSourceData.GetOrAdd(row.Source, id => new List<JsisCsvDataRow>());
            //m_parsedSourceData.Add(row);
        }

        private void ParseRow(List<string> buffer, JsisCsvDataRow newRow)
        {
            for (int i = 0; i < buffer.Count; i++)
            {
                var success = double.TryParse(buffer[i], out double value);
                if (!success)
                {
                    value = double.NaN; //if conversion fail, set value to NAN, is this the behaviour we want?
                }
                if (i == 0)
                {
                    var ts = BaseDateTime.AddSeconds(value);
                    newRow.Timestamp = ts;
                    //timeStamps.Add(datetimed); //might need to change if the first time column changes
                    //timeStampeNumberInDays.Add(datetimed.ToOADate());
                }
                else
                {
                    //signalList[i - 1].Data.Add(value);
                }
            }
        }

        // We just bubble any exceptions captured in process queue out to parsing exception event...
        private void ProcessExceptionHandler(object sender, EventArgs<Exception> e) => OnParsingException(e.Argument);
        /// <summary>
        /// Raises the <see cref="ParsingException"/> event.
        /// </summary>
        /// <param name="ex">The <see cref="Exception"/> that was encountered during parsing.</param>
        protected virtual void OnParsingException(Exception ex) => ParsingException?.SafeInvoke(this, new EventArgs<Exception>(ex));
        /// <summary>
        /// Raises the <see cref="SourceDataParsed"/> event.
        /// </summary>
        /// <param name="source">Identifier for the data source.</param>
        /// <param name="output">The objects that were deserialized from binary images from the <paramref name="source"/> data stream.</param>
        protected virtual void OnSourceDataParsed(IList<JsisCsvDataRow> output) => SourceDataParsed?.SafeInvoke(this, new EventArgs<IList<JsisCsvDataRow>>(output));

        /// <summary>
        /// <see cref="AsyncQueue{T}"/> handler used to publish queued outputs.
        /// </summary>
        /// <param name="outputArgs">Event args containing new output to publish.</param>
        protected virtual void PublishParsedOutput(EventArgs<JsisCsvDataRow> outputArgs) => DataParsed?.SafeInvoke(this, outputArgs);
        // Expose exceptions encountered via async queue processing to parsing exception event
        private void m_parsedOutputQueue_ProcessException(object sender, EventArgs<Exception> e) => OnParsingException(e.Argument);

        internal void ParseHeaderRow(string[] row)
        {
            for (int i = 0; i < row.Length; i++)
            {

            }
            DataParsed?.SafeInvoke(this, EventArgs.Empty);
        }

        internal void ParseDataRow(string[] row)
        {
            m_bufferQueue.Enqueue(new[] { row.ToList() });
        }
        #endregion
    }
}
