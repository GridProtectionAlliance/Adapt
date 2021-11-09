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
        public string[] SignalNames { get; set; }
        public string[] SignalTypes { get; set; }
        public string[] SignalUnits { get; set; }
        public string[] SignalDescription { get; set; }

        internal void ParseChannels()
        {

            for (int i = 1; i < SignalNames.Length; i++)
            {
                string type = SignalTypes[i];
                string name = SignalNames[i];
                string unit = SignalUnits[i];
                string description = SignalDescription[i];
                JsisCsvChannel newChannel = new JsisCsvChannel(PMUName);
                newChannel.Name = name;
                newChannel.Description = description;
                newChannel.Unit = unit;
                //newChannel.FramesPerSecond = samplingRate;
                bool isCustom = false;
                if (!string.IsNullOrEmpty(type))
                {
                    char[] tp = type.ToArray();
                    switch (tp[0])
                    {
                        case 'V':
                            if (tp.Length == 3)
                            {
                                switch (tp[2])
                                {
                                    case 'M':
                                        newChannel.Type = MeasurementType.VoltageMagnitude;
                                        switch (tp[1])
                                        {
                                            case 'P':
                                                newChannel.Phase = Phase.Pos;
                                                PhasorDefinitions.Add(newChannel);
                                                break;
                                            case 'A':
                                                newChannel.Phase = Phase.A;
                                                PhasorDefinitions.Add(newChannel);
                                                break;
                                            case 'B':
                                                newChannel.Phase = Phase.B;
                                                PhasorDefinitions.Add(newChannel);
                                                break;
                                            case 'C':
                                                newChannel.Phase = Phase.C;
                                                PhasorDefinitions.Add(newChannel);
                                                break;
                                            default:
                                                isCustom = true;
                                                break;
                                        }
                                        break;
                                    case 'A':
                                        newChannel.Type = MeasurementType.VoltagePhase;
                                        switch (tp[1])
                                        {
                                            case 'P':
                                                newChannel.Phase = Phase.Pos;
                                                PhasorDefinitions.Add(newChannel);
                                                break;
                                            case 'A':
                                                newChannel.Phase = Phase.A;
                                                PhasorDefinitions.Add(newChannel);
                                                break;
                                            case 'B':
                                                newChannel.Phase = Phase.B;
                                                PhasorDefinitions.Add(newChannel);
                                                break;
                                            case 'C':
                                                newChannel.Phase = Phase.C;
                                                PhasorDefinitions.Add(newChannel);
                                                break;
                                            default:
                                                isCustom = true;
                                                break;
                                        }
                                        break;
                                    default:
                                        isCustom = true;
                                        break;
                                }
                            }
                            else
                            {
                                isCustom = true;
                            }
                            break;
                        case 'I':
                            if (tp.Length == 3)
                            {
                                switch (tp[2])
                                {
                                    case 'M':
                                        newChannel.Type = MeasurementType.CurrentMagnitude;
                                        switch (tp[1])
                                        {
                                            case 'P':
                                                newChannel.Phase = Phase.Pos;
                                                PhasorDefinitions.Add(newChannel);
                                                break;
                                            case 'A':
                                                newChannel.Phase = Phase.A;
                                                PhasorDefinitions.Add(newChannel);
                                                break;
                                            case 'B':
                                                newChannel.Phase = Phase.B;
                                                PhasorDefinitions.Add(newChannel);
                                                break;
                                            case 'C':
                                                newChannel.Phase = Phase.C;
                                                PhasorDefinitions.Add(newChannel);
                                                break;
                                            default:
                                                isCustom = true;
                                                break;
                                        }
                                        break;
                                    case 'A':
                                        newChannel.Type = MeasurementType.CurrentPhase;
                                        switch (tp[1])
                                        {
                                            case 'P':
                                                newChannel.Phase = Phase.Pos;
                                                PhasorDefinitions.Add(newChannel);
                                                break;
                                            case 'A':
                                                newChannel.Phase = Phase.A;
                                                PhasorDefinitions.Add(newChannel);
                                                break;
                                            case 'B':
                                                newChannel.Phase = Phase.B;
                                                PhasorDefinitions.Add(newChannel);
                                                break;
                                            case 'C':
                                                newChannel.Phase = Phase.C;
                                                PhasorDefinitions.Add(newChannel);
                                                break;
                                            default:
                                                isCustom = true;
                                                break;
                                        }
                                        break;
                                    default:
                                        isCustom = true;
                                        break;
                                }
                            }
                            else
                            {
                                isCustom = true;
                            }
                            break;
                        case 'P':
                        case 'Q':
                            if (tp.Length == 1)
                            {
                                newChannel.Type = MeasurementType.Analog;
                                newChannel.Phase = Phase.NONE;
                                AnalogDefinitions.Add(newChannel);
                            }
                            else
                            {
                                isCustom = true;
                            }
                            break;
                        case 'F':
                            if (tp.Length == 1)
                            {
                                newChannel.Type = MeasurementType.Frequency;
                                newChannel.Phase = Phase.NONE;
                                FrequencyDefinition.Add(newChannel);
                            }
                            else
                            {
                                isCustom = true;
                            }
                            break;
                        case 'D':
                            if (tp.Length == 1)
                            {
                                newChannel.Type = MeasurementType.Digital;
                                newChannel.Phase = Phase.NONE;
                                DigitalDefinitions.Add(newChannel);
                            }
                            else
                            {
                                isCustom = true;
                            }
                            break;
                        default:
                            isCustom = true;
                            break;
                    }
                }
                else
                {
                    isCustom = true;
                }
                if (isCustom)
                {
                    newChannel.Type = MeasurementType.Other;
                    newChannel.Phase = Phase.NONE;
                    CustomDefinitions.Add(newChannel);
                }
                ColumnSignalDict[i] = newChannel;
            }
        }

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
