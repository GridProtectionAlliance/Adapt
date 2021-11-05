using Adapt.Models;
using Gemstone;
using GemstoneCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsisCsvReader
{
    public class JsisCsvHeaderParser
    {
        // Fields
        private JsisCsvHeader m_header;
        // Events
        public event EventHandler<EventArgs<JsisCsvHeader>> HeaderParsingDone;
        public string Device { get; internal set; }

        internal void ParseHeader(EventArgs<List<string[]>> e)
        {
            string[] signalNames = e.Argument[0];
            string[] signalTypes = e.Argument[1];
            string[] signalUnits = e.Argument[2];
            string[] signalDescription = e.Argument[3];
            string[] dataRow1 = e.Argument[4];
            string[] dataRow2 = e.Argument[5];
            double samplingRate = -1d;
            if (double.TryParse(dataRow1[0], out double value1) && double.TryParse(dataRow2[0], out double value2))
            {
                samplingRate = (int)Math.Round(1 / ((value2 - value1) * 10)) * 10;
            }

            m_header = new JsisCsvHeader(Device);
            for (int i = 1; i < signalNames.Length; i++)
            {
                string type = signalTypes[i];
                string name = signalNames[i];
                string unit = signalUnits[i];
                string description = signalDescription[i];
                JsisCsvChannel newChannel = new JsisCsvChannel(Device);
                newChannel.Name = name;
                newChannel.Description = description;
                newChannel.Unit = unit;
                newChannel.FramesPerSecond = samplingRate;
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
                                                m_header.PhasorDefinitions.Add(newChannel);
                                                break;
                                            case 'A':
                                                newChannel.Phase = Phase.A;
                                                m_header.PhasorDefinitions.Add(newChannel);
                                                break;
                                            case 'B':
                                                newChannel.Phase = Phase.B;
                                                m_header.PhasorDefinitions.Add(newChannel);
                                                break;
                                            case 'C':
                                                newChannel.Phase = Phase.C;
                                                m_header.PhasorDefinitions.Add(newChannel);
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
                                                m_header.PhasorDefinitions.Add(newChannel);
                                                break;
                                            case 'A':
                                                newChannel.Phase = Phase.A;
                                                m_header.PhasorDefinitions.Add(newChannel);
                                                break;
                                            case 'B':
                                                newChannel.Phase = Phase.B;
                                                m_header.PhasorDefinitions.Add(newChannel);
                                                break;
                                            case 'C':
                                                newChannel.Phase = Phase.C;
                                                m_header.PhasorDefinitions.Add(newChannel);
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
                                                m_header.PhasorDefinitions.Add(newChannel);
                                                break;
                                            case 'A':
                                                newChannel.Phase = Phase.A;
                                                m_header.PhasorDefinitions.Add(newChannel);
                                                break;
                                            case 'B':
                                                newChannel.Phase = Phase.B;
                                                m_header.PhasorDefinitions.Add(newChannel);
                                                break;
                                            case 'C':
                                                newChannel.Phase = Phase.C;
                                                m_header.PhasorDefinitions.Add(newChannel);
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
                                                m_header.PhasorDefinitions.Add(newChannel);
                                                break;
                                            case 'A':
                                                newChannel.Phase = Phase.A;
                                                m_header.PhasorDefinitions.Add(newChannel);
                                                break;
                                            case 'B':
                                                newChannel.Phase = Phase.B;
                                                m_header.PhasorDefinitions.Add(newChannel);
                                                break;
                                            case 'C':
                                                newChannel.Phase = Phase.C;
                                                m_header.PhasorDefinitions.Add(newChannel);
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
                                m_header.AnalogDefinitions.Add(newChannel);
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
                                m_header.FrequencyDefinition.Add(newChannel);
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
                                m_header.DigitalDefinitions.Add(newChannel);
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
                    m_header.CustomDefinitions.Add(newChannel);
                }
                m_header.ColumnSignalDict[i] = newChannel;
            }
            EventArgs<JsisCsvHeader> outputArgs = new EventArgs<JsisCsvHeader>(m_header);
            HeaderParsingDone?.Invoke(this, outputArgs);
        }

        public DateTime BaseDateTime { get; internal set; }
    }
}
