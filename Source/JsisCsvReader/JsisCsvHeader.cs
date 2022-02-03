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
        public int SamplingRate { get; set; }

        public void ParseChannels()
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
                newChannel.FramesPerSecond = SamplingRate;
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
                        case 'R':
                            if (tp.Length == 1 || type == "ROCOF")
                            {
                                newChannel.Type = MeasurementType.ROCOF;
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

    }
}
