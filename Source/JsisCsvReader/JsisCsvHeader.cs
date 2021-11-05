using Adapt.Models;
using Gemstone;
using GemstoneCommon;
using GemstonePhasorProtocolls;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace JsisCsvReader
{
    public class JsisCsvHeader
    {
        private string m_device;

        public JsisCsvHeader()
        {
            PhasorDefinitions = new List<JsisCsvChannel>();
            AnalogDefinitions = new List<JsisCsvChannel>();
            DigitalDefinitions = new List<JsisCsvChannel>();
            FrequencyDefinition = new List<JsisCsvChannel>();
            CustomDefinitions = new List<JsisCsvChannel>();
            ColumnSignalDict = new Dictionary<int, JsisCsvChannel>();
        }

        public JsisCsvHeader(string device) : this()
        {
            m_device = device;
        }

        public List<JsisCsvChannel> PhasorDefinitions { get; set; }
        public List<JsisCsvChannel> AnalogDefinitions { get; set; }
        public List<JsisCsvChannel> DigitalDefinitions { get; set; }
        public List<JsisCsvChannel> FrequencyDefinition { get; set; }
        public List<JsisCsvChannel> CustomDefinitions { get; set; }
        public Dictionary<int, JsisCsvChannel> ColumnSignalDict { get; set; }
        public string PMUName => m_device;
        ///// <summary>
        //                                           /// Gets a reference to the <see cref="PhasorDefinitionCollection"/> of this <see cref="IConfigurationCell"/>.
        //                                           /// </summary>
        //PhasorDefinitionCollection PhasorDefinitions
        //{
        //    get;
        //}

        ///// <summary>
        ///// Gets or sets the <see cref="DataFormat"/> for the <see cref="IPhasorDefinition"/> objects in the <see cref="PhasorDefinitions"/> of this <see cref="IConfigurationCell"/>.
        ///// </summary>
        //DataFormat PhasorDataFormat
        //{
        //    get;
        //    set;
        //}

        ///// <summary>
        ///// Gets or sets the <see cref="CoordinateFormat"/> for the <see cref="IPhasorDefinition"/> objects in the <see cref="PhasorDefinitions"/> of this <see cref="IConfigurationCell"/>.
        ///// </summary>
        //CoordinateFormat PhasorCoordinateFormat
        //{
        //    get;
        //    set;
        //}

        ///// <summary>
        ///// Gets or sets the <see cref="AngleFormat"/> for the <see cref="IPhasorDefinition"/> objects in the <see cref="PhasorDefinitions"/> of this <see cref="IConfigurationCell"/>.
        ///// </summary>
        //AngleFormat PhasorAngleFormat
        //{
        //    get;
        //    set;
        //}

        ///// <summary>
        ///// Gets or sets the <see cref="IFrequencyDefinition"/> of this <see cref="IConfigurationCell"/>.
        ///// </summary>
        //IFrequencyDefinition FrequencyDefinition
        //{
        //    get;
        //    set;
        //}

        ///// <summary>
        ///// Gets or sets the <see cref="DataFormat"/> of the <see cref="FrequencyDefinition"/> of this <see cref="IConfigurationCell"/>.
        ///// </summary>
        //DataFormat FrequencyDataFormat
        //{
        //    get;
        //    set;
        //}

        ///// <summary>
        ///// Gets or sets the nominal of the <see cref="FrequencyDefinition"/> of this <see cref="IConfigurationCell"/>.
        ///// </summary>
        //double NominalFrequency
        //{
        //    get;
        //    set;
        //}

        ///// <summary>
        ///// Gets a reference to the <see cref="AnalogDefinitionCollection"/> of this <see cref="IConfigurationCell"/>.
        ///// </summary>
        //AnalogDefinitionCollection AnalogDefinitions
        //{
        //    get;
        //}

        ///// <summary>
        ///// Gets or sets the <see cref="DataFormat"/> for the <see cref="IAnalogDefinition"/> objects in the <see cref="AnalogDefinitions"/> of this <see cref="IConfigurationCell"/>.
        ///// </summary>
        //DataFormat AnalogDataFormat
        //{
        //    get;
        //    set;
        //}

        ///// <summary>
        ///// Gets a reference to the <see cref="DigitalDefinitionCollection"/> of this <see cref="IConfigurationCell"/>.
        ///// </summary>
        //DigitalDefinitionCollection DigitalDefinitions
        //{
        //    get;
        //}

    }
    //public class JsisCsvHeader : IHeaderFrame
    //{
    //    public HeaderCellCollection Cells => throw new NotImplementedException();

    //    public IHeaderFrameParsingState State { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    //    public string HeaderData { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    //    public FundamentalFrameType FrameType => throw new NotImplementedException();

    //    public ushort IDCode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    //    public UnixTimeTag TimeTag => throw new NotImplementedException();

    //    public Dictionary<string, string> Attributes => throw new NotImplementedException();

    //    public object Tag { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    //    public int BinaryLength => throw new NotImplementedException();

    //    public ConcurrentDictionary<string, ITimeSeriesValue> Measurements => throw new NotImplementedException();

    //    public bool Published { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    //    public int SortedMeasurements => throw new NotImplementedException();

    //    public Ticks Timestamp { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    //    public string PMUName { get; set; }

    //    object IChannelFrame.Cells => throw new NotImplementedException();

    //    IChannelParsingState IChannel.State { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    //    public int GenerateBinaryImage(byte[] buffer, int startIndex)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void GetObjectData(SerializationInfo info, StreamingContext context)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public int ParseBinaryImage(byte[] buffer, int startIndex, int length)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
