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
        private DateTime m_baseDateTime;
        private string m_device;
        private JsisCsvHeader m_header;
        private string m_jsisCsvFilename;
        #endregion

        public JsisCsvHeader GetHeader()
        {
            if (m_header is null)
            {
                m_header = new JsisCsvHeader(m_device);
                using (Microsoft.VisualBasic.FileIO.TextFieldParser reader = new Microsoft.VisualBasic.FileIO.TextFieldParser(m_jsisCsvFilename))
                {
                    reader.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
                    reader.SetDelimiters(",");
                    reader.HasFieldsEnclosedInQuotes = true;

                    m_header.SignalNames = reader.ReadFields();
                    m_header.SignalTypes = reader.ReadFields();
                    m_header.SignalUnits = reader.ReadFields();
                    m_header.SignalDescription = reader.ReadFields();
                    if (double.TryParse(reader.ReadFields().First(), out double value1) && double.TryParse(reader.ReadFields().First(), out double value2))
                    {
                        m_header.SamplingRate = (int)Math.Round(1 / ((value2 - value1) * 10)) * 10;
                    }
                    m_header.ParseChannels();
                }
            }
            return m_header;
        }

        private string m_getPMUName(string filename)
        {
            return Path.GetFileNameWithoutExtension(filename).Split('_')[0];
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

        public async IAsyncEnumerable<JsisCsvDataRow> GetData()
        {
            if (m_header is null)
            {
                GetHeader();
            }

            using (Microsoft.VisualBasic.FileIO.TextFieldParser reader = new Microsoft.VisualBasic.FileIO.TextFieldParser(m_jsisCsvFilename))
            {
                reader.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
                reader.SetDelimiters(",");
                reader.HasFieldsEnclosedInQuotes = true;

                reader.ReadFields();
                reader.ReadFields();
                reader.ReadFields();
                reader.ReadFields();

                while (!reader.EndOfData)
                {
                    JsisCsvDataRow newRow = new JsisCsvDataRow();
                    string[] data = reader.ReadFields();
                    //await Task.Run(() =>
                    //{
                        for (int i = 0; i < data.Count(); i++)
                        {
                            var success = double.TryParse(data[i], out double value);
                            if (!success)
                            {
                                value = double.NaN; //if conversion fail, set value to NAN, is this the behaviour we want?
                            }
                            if (i == 0)
                            {
                                var ts = m_baseDateTime.AddSeconds(value);
                                newRow.Timestamp = ts;
                            }
                            else
                            {
                                var signal = m_header.ColumnSignalDict[i];
                                var newSignal = new JsisCsvChannel(signal);
                                newSignal.Measurement = value;
                                switch (newSignal.Type)
                                {
                                    case Adapt.Models.MeasurementType.VoltageMagnitude:
                                        newRow.PhasorDefinitions.Add(newSignal);
                                        break;
                                    case Adapt.Models.MeasurementType.VoltagePhase:
                                        newRow.PhasorDefinitions.Add(newSignal);
                                        break;
                                    case Adapt.Models.MeasurementType.CurrentMagnitude:
                                        newRow.PhasorDefinitions.Add(newSignal);
                                        break;
                                    case Adapt.Models.MeasurementType.CurrentPhase:
                                        newRow.PhasorDefinitions.Add(newSignal);
                                        break;
                                    case Adapt.Models.MeasurementType.VoltageRMS:
                                        break;
                                    case Adapt.Models.MeasurementType.CurrentRMS:
                                        break;
                                    case Adapt.Models.MeasurementType.Frequency:
                                        newRow.FrequencyDefinition.Add(newSignal);
                                        break;
                                    case Adapt.Models.MeasurementType.Digital:
                                        newRow.DigitalDefinitions.Add(newSignal);
                                        break;
                                    case Adapt.Models.MeasurementType.Analog:
                                        newRow.AnalogDefinitions.Add(newSignal);
                                        break;
                                    case Adapt.Models.MeasurementType.DeltaFrequency:
                                        break;
                                    case Adapt.Models.MeasurementType.ROCOF:
                                        break;
                                    case Adapt.Models.MeasurementType.Other:
                                        newRow.CustomDefinitions.Add(newSignal);
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    //});

                    yield return newRow;
                }

            }
        }
        public JsisCsvParser(string filename)
        {
            m_jsisCsvFilename = filename;
            m_baseDateTime = m_getFileDateTime(filename);
            m_device = m_getPMUName(filename);
        }
    }
}
