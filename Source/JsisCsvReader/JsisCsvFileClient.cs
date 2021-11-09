using Gemstone;
using Gemstone.Communication;
using Gemstone.EventHandlerExtensions;
using Gemstone.IO;
using Gemstone.Units;
using Gemstone.StringExtensions;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gemstone.Threading;

namespace JsisCsvReader
{
    public class JsisCsvFileClient : IClient
    {
        // Fields
        private bool m_autoRepeat;
        private bool m_receiveOnDemand;
        private int m_receiveInterval;
        private readonly SharedTimer m_receiveDataTimer;
        private Ticks m_connectTime;
        private Ticks m_disconnectTime;
        private ClientState m_currentState;
        private ManualResetEvent? m_connectionHandle;
        private bool m_initialized;
        private readonly TransportProvider<TextFieldParser> m_fileClient;
        private FileAccess m_fileAccessMode;
        private Thread? m_connectionThread;
        private Dictionary<string, string> m_connectData = DefaultConnectionString.ParseKeyValuePairs();
        private FileStream m_csvFilestream;
        private int m_receiveBufferSize;
        private int m_maxConnectionAttempts;
        private string? m_connectionString;
        private bool m_disposed;


        /// <summary>
        /// Gets or sets the data required by the client to connect to the server.
        /// </summary>
        public string ConnectionString
        {
            get => m_connectionString ?? "";
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException(nameof(value));

                ValidateConnectionString(value);

                m_connectionString = value;
                ReConnect();
            }
        }
        public int SendBufferSize { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <summary>
        /// Gets or sets the size of the buffer used by the client for receiving data from the server.
        /// </summary>
        /// <exception cref="ArgumentException">The value being assigned is either zero or negative.</exception>
        public int ReceiveBufferSize
        {
            get => m_receiveBufferSize;
            set
            {
                if (value != 1)
                    throw new ArgumentException("Value cannot be zero or negative");

                m_receiveBufferSize = value;
            }
        }
        public Encoding TextEncoding { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string ServerUri => throw new NotImplementedException();

        /// <summary>
        /// Gets the current <see cref="ClientState"/>.
        /// </summary>
        public virtual ClientState CurrentState => m_currentState;

        public TransportProtocol TransportProtocol => throw new NotImplementedException();

        public Time ConnectionTime => throw new NotImplementedException();

        /// <summary>
        /// Gets the <see cref="TransportStatistics"/> for the client connection.
        /// </summary>
        public TransportStatistics Statistics { get; }

        public bool Enabled { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool IsDisposed => throw new NotImplementedException();

        public string Name => throw new NotImplementedException();

        public string Status => throw new NotImplementedException();
        /// <summary>
        /// Gets or sets the <see cref="FileMode"/> value to be used when opening the file.
        /// </summary>
        public FileMode FileOpenMode { get; set; }

        /// <summary>
        /// Gets or set the <see cref="FileShare"/> value to be used when opening the file.
        /// </summary>
        public FileShare FileShareMode { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="FileAccess"/> value to be used when opening the file.
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="FileAccessMode"/> is set to <see cref="FileAccess.ReadWrite"/> when <see cref="AutoRepeat"/> is enabled.</exception>
        public FileAccess FileAccessMode
        {
            get => m_fileAccessMode;
            set
            {
                if (value == FileAccess.ReadWrite && m_autoRepeat)
                    throw new InvalidOperationException("FileAccessMode cannot be set to FileAccess.ReadWrite when AutoRepeat is enabled");

                m_fileAccessMode = value;
            }
        }
        /// <summary>
        /// Gets or sets a boolean value that indicates whether receiving (reading) of data will be initiated manually by calling <see cref="ReadNextBuffer"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="ReceiveInterval"/> will be set to -1 when <see cref="ReceiveOnDemand"/> is enabled.
        /// </remarks>
        public bool ReceiveOnDemand
        {
            get => m_receiveOnDemand;
            set
            {
                m_receiveOnDemand = value;

                // We'll disable receiving data at a set interval if user wants to receive data on demand.
                if (m_receiveOnDemand)
                    m_receiveInterval = -1;
            }
        }
        /// <summary>
        /// Gets or sets the maximum number of times the client will attempt to connect to the server.
        /// </summary>
        /// <remarks>Set <see cref="MaxConnectionAttempts"/> to -1 for infinite connection attempts.</remarks>
        public int MaxConnectionAttempts
        {
            get => m_maxConnectionAttempts;
            set => m_maxConnectionAttempts = value < 1 ? -1 : value;
        }

        // Constants

        /// <summary>
        /// Specifies the default value for the <see cref="MaxConnectionAttempts"/> property.
        /// </summary>
        public const int DefaultMaxConnectionAttempts = -1;
        /// <summary>
        /// Specifies the default value for the <see cref="FileOpenMode"/> property.
        /// </summary>
        public const FileMode DefaultFileOpenMode = FileMode.OpenOrCreate;

        /// <summary>
        /// Specifies the default value for the <see cref="FileShareMode"/> property.
        /// </summary>
        public const FileShare DefaultFileShareMode = FileShare.ReadWrite;

        /// <summary>
        /// Specifies the default value for the <see cref="FileAccessMode"/> property.
        /// </summary>
        public const FileAccess DefaultFileAccessMode = FileAccess.ReadWrite;
        /// <summary>
        /// Specifies the default value for the <see cref="ClientBase.ConnectionString"/> property.
        /// </summary>
        public const string DefaultConnectionString = "File=DataFile.txt";

        // Events
        public event EventHandler ConnectionAttempt;
        public event EventHandler ConnectionEstablished;
        public event EventHandler ConnectionTerminated;
        public event EventHandler<EventArgs<Exception>> ConnectionException;
        public event EventHandler SendDataStart;
        public event EventHandler SendDataComplete;
        public event EventHandler<EventArgs<Exception>> SendDataException;
        public event EventHandler<EventArgs<int>> ReceiveData;
        public event EventHandler<EventArgs<string[], int>> ReceiveCsvData;
        public event EventHandler<EventArgs<byte[], int>> ReceiveDataComplete;
        public event EventHandler<EventArgs<Exception>> ReceiveDataException;
        public event EventHandler Disposed;
        public event EventHandler<EventArgs<List<string[]>>> ReceiveCsvHeader;
        public string Filename { get; set; }

        public JsisCsvFileClient()
        {
            m_currentState = ClientState.Disconnected;
            m_fileClient = new TransportProvider<TextFieldParser>();
            m_maxConnectionAttempts = DefaultMaxConnectionAttempts;
            Statistics = new TransportStatistics();
            m_receiveDataTimer = s_timerScheduler.CreateTimer();
        }

        /// <summary>
        /// Validates the specified <paramref name="connectionString"/>.
        /// </summary>
        /// <param name="connectionString">Connection string to be validated.</param>
        /// <exception cref="ArgumentException">File property is missing.</exception>
        protected void ValidateConnectionString(string connectionString)
        {
            m_connectData = connectionString.ParseKeyValuePairs();

            if (!m_connectData.ContainsKey("file"))
                throw new ArgumentException($"File property is missing (Example: {DefaultConnectionString})");
        }
        public void Connect()
        {
            throw new NotImplementedException();
        }

        public WaitHandle ConnectAsync()
        {
            if (CurrentState != ClientState.Disconnected)
                throw new InvalidOperationException("Client is currently not disconnected");

            // Initialize if uninitialized.
            if (!m_initialized)
                Initialize();

            // Set up connection event wait handle
            m_connectionHandle = new ManualResetEvent(false);
            //return m_connectHandle;

            //m_connectionHandle = (ManualResetEvent?)m_connectHandle;

            m_fileClient.SetReceiveBuffer(ReceiveBufferSize);

            m_connectionThread = new Thread(OpenFile) { IsBackground = true };
            m_connectionThread.Start();

            return m_connectionHandle;
        }

        internal void ReadHeader()
        {
            List<string[]> headerRows = new List<string[]>();
            for (int i = 0; i < 6; i++)
            {
                headerRows.Add(m_fileClient.Provider.ReadFields());
            }
            ReceiveCsvHeader?.SafeInvoke(this, new EventArgs<List<string[]>>(headerRows));
            //string[] signalNames = m_fileClient.Provider.ReadFields();
            //string[] signalTypes = m_fileClient.Provider.ReadFields();
            //string[] signalUnits = m_fileClient.Provider.ReadFields();
            //string[] signalDescription = m_fileClient.Provider.ReadFields();
            //string dateLines = m_fileClient.Provider.ReadToEnd();
            ////m_fileClient.Provider.ReadFields();
            //// Notify users of header ready
            //ReceiveCsvHeader?.SafeInvoke(this, new EventArgs<string[], string[], string[], string[]>(signalNames, signalTypes, signalUnits, signalDescription));
        }

        /// <summary>
        /// Re-connects the client if currently connected.
        /// </summary>
        private void ReConnect()
        {
            if (m_currentState != ClientState.Connected)
                return;

            Disconnect();

            while (m_currentState != ClientState.Disconnected)
                Thread.Sleep(100);

            Connect();
        }

        public void Disconnect()
        {
            if (CurrentState == ClientState.Disconnected)
                return;

            m_fileClient.Reset();
            m_receiveDataTimer.Stop();

            //m_connectionThread?.Abort();

            OnConnectionTerminated();
        }

        /// <summary>
        /// Raises the <see cref="ConnectionTerminated"/> event.
        /// </summary>
        protected virtual void OnConnectionTerminated()
        {
            m_currentState = ClientState.Disconnected;

            // Save the time when client was disconnected from the server.
            m_disconnectTime = DateTime.UtcNow.Ticks;

            ConnectionTerminated?.SafeInvoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Releases all the resources used by the <see cref="ClientBase"/> object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (m_disposed)
                return;

            try
            {
                // This will be done regardless of whether the object is finalized or disposed.
                if (disposing)
                {
                    // This will be done only when the object is disposed by calling Dispose().
                    Disconnect();
                    m_connectionHandle?.Dispose();
                }
            }
            finally
            {
                m_disposed = true;  // Prevent duplicate dispose.
                Disposed?.SafeInvoke(this, EventArgs.Empty);
            }
        }

        public void Initialize()
        {
            if (m_initialized)
                return;

            m_initialized = true;   // Initialize only once.
        }

        public int Read(byte[] buffer, int startIndex, int length)
        {
            throw new NotImplementedException();
        }

        public void Send(byte[] data, int offset, int length)
        {
            throw new NotImplementedException();
        }

        public WaitHandle SendAsync(byte[] data, int offset, int length)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Raises the <see cref="ConnectionAttempt"/> event.
        /// </summary>
        protected void OnConnectionAttempt()
        {
            m_currentState = ClientState.Connecting;
            ConnectionAttempt?.SafeInvoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the <see cref="ConnectionEstablished"/> event.
        /// </summary>
        protected virtual void OnConnectionEstablished()
        {
            m_currentState = ClientState.Connected;
            m_disconnectTime = 0;

            // Save the time when the client connected to the server.
            m_connectTime = DateTime.UtcNow.Ticks;

            // Signal any waiting threads about successful connection.
            m_connectionHandle?.Set();

            ConnectionEstablished?.SafeInvoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Connects to the <see cref="FileStream"/>.
        /// </summary>
        private void OpenFile()
        {
            int connectionAttempts = 0;

            while (MaxConnectionAttempts == -1 || connectionAttempts < MaxConnectionAttempts)
            {
                try
                {
                    OnConnectionAttempt();

                    // Open the file.
                    m_csvFilestream = new FileStream(FilePath.GetAbsolutePath(m_connectData["file"]), FileOpenMode, m_fileAccessMode, FileShareMode);
                    m_fileClient.Provider = new TextFieldParser(m_csvFilestream);
                    m_fileClient.Provider.TextFieldType = FieldType.Delimited;
                    m_fileClient.Provider.SetDelimiters(",");
                    m_fileClient.Provider.HasFieldsEnclosedInQuotes = true;
                    Filename = m_connectData["file"];

                    // Move to the specified offset.
                    //m_fileClient.Provider.Seek(m_startingOffset, SeekOrigin.Begin);

                    m_connectionHandle?.Set();
                    OnConnectionEstablished();

                    if (!m_receiveOnDemand)
                    {
                        if (m_receiveInterval > 0)
                        {
                            // Start receiving data at interval.
                            m_receiveDataTimer.Interval = m_receiveInterval;
                            m_receiveDataTimer.Start();
                        }
                        else
                        {
                            // Start receiving data continuously.
                            while (true)
                            {
                                ReadData();         // Read all available data.
                                Thread.Sleep(1000); // Wait for more data to be available.
                            }
                        }
                    }

                    break;  // We're done here.
                }
                catch (Exception ex)
                {
                    // Keep retrying connecting to the file.
                    Thread.Sleep(1000);
                    connectionAttempts++;
                    OnConnectionException(ex);
                }
            }
        }

        /// <summary>
        /// Receive (reads) data from the <see cref="FileStream"/>.
        /// </summary>
        private void ReadData()
        {
            try
            {
                // Process the entire file content
                if (!m_fileClient.Provider.EndOfData)
                {
                    //// Retrieve data from the file.
                    //m_fileClient.BytesReceived = m_fileClient.Provider.ReadFields();
                    //m_fileClient.Statistics.UpdateBytesReceived(m_fileClient.BytesReceived);

                    // Notify of the retrieved data.
                    //ReceiveCsvData?.SafeInvoke(this, new EventArgs<string[], int>(m_fileClient.Provider.ReadFields(), (int)Statistics.TotalBytesReceived));
                    int lineNumber = (int)m_fileClient.Provider.LineNumber;
                    string[] data = m_fileClient.Provider.ReadFields();
                    //JsisCsvRowDataWithIndex source = new JsisCsvRowDataWithIndex
                    //{

                    //}
                    OnReceiveDataComplete(data, lineNumber);

                    // Re-read the file if the user wants to repeat when done reading the file.
                    if (m_autoRepeat && m_fileClient.Provider.LineNumber == -1) 
                    {
                        m_fileClient.Provider = new TextFieldParser(m_csvFilestream);
                        m_fileClient.Provider.TextFieldType = FieldType.Delimited;
                        m_fileClient.Provider.SetDelimiters(",");
                        m_fileClient.Provider.HasFieldsEnclosedInQuotes = true;
                    }

                    //TODO: Check Disconnect to break loop as well
                    // Stop processing the file if user has either opted to receive data on demand or receive data at a predefined interval.
                    //if (m_receiveOnDemand || m_receiveInterval > 0)
                    //    break;
                }
                else
                {
                    //disconnect
                }
            }
            catch (Exception ex)
            {
                // Notify of the exception.
                if (!(ex is NullReferenceException))
                    OnReceiveDataException(ex);
            }
        }

        private void OnReceiveDataComplete(string[] data, int line)
        {
            if (data == null)
                return;

            // Update transport statistics
            Statistics.LastReceive = DateTime.UtcNow;
            Statistics.LastBytesReceived = line;
            Statistics.TotalBytesReceived = line;

            //// Reset buffer index used by read method
            //ReadIndex = 0;

            // Notify users of data ready
            ReceiveCsvData?.SafeInvoke(this, new EventArgs<string[], int>(data, line));

            //// Most inheritors of this class "reuse" an existing buffer, as such you cannot assume what the user is going to do
            //// with the buffer provided, so we pass in a "copy" of the buffer for the user since they may assume control of and
            //// possibly even cache the provided buffer (e.g., passing the buffer to a process queue)
            //ReceiveDataComplete?.SafeInvoke(this, new EventArgs<byte[], int>(data.BlockCopy(0, line), line));
        }

        private void m_receiveDataTimer_Elapsed(object sender, EventArgs<DateTime> e) => ReadData();

        /// <summary>
        /// Raises the <see cref="ClientBase.ConnectionException"/> event.
        /// </summary>
        /// <param name="ex">Exception to send to <see cref="ClientBase.ConnectionException"/> event.</param>
        protected void OnConnectionException(Exception ex)
        {
            m_currentState = ClientState.Disconnected;
            if (!(ex is ObjectDisposedException))
                ConnectionException?.SafeInvoke(this, new EventArgs<Exception>(ex));
        }
        /// <summary>
        /// Raises the <see cref="ReceiveDataException"/> event.
        /// </summary>
        /// <param name="ex">Exception to send to <see cref="ReceiveDataException"/> event.</param>
        protected virtual void OnReceiveDataException(Exception ex)
        {
            if (!(ex is ObjectDisposedException))
                ReceiveDataException?.SafeInvoke(this, new EventArgs<Exception>(ex));
        }
        /// <summary>
        /// Reads next data buffer from the <see cref="FileStream"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="ReadNextBuffer"/> is called when <see cref="FileClient"/> is not connected.</exception>
        /// <exception cref="InvalidOperationException"><see cref="ReadNextBuffer"/> is called when <see cref="ReceiveOnDemand"/> is disabled.</exception>
        public void ReadNextBuffer()
        {
            if (m_receiveOnDemand)
            {
                if (CurrentState == ClientState.Connected)
                    ReadData();
                else
                    throw new InvalidOperationException("Client is currently not connected");
            }
            else
            {
                throw new InvalidOperationException("ReadNextBuffer() cannot be used when ReceiveOnDemand is disabled");
            }
        }
        #region [ Static ]

        // Static Fields

        // Common use static timer for FileClient instances
        private static readonly SharedTimerScheduler s_timerScheduler = new SharedTimerScheduler();

        #endregion
    }
}
