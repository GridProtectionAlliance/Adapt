using System;
using System.Globalization;

namespace JsisCsvReader
{
    public class JSISCSVReader
    {
        #region [ Members ]
        private int m_samplingRate = -1;
        private int m_numberOfDataPointInFile = 0;
        #endregion

        #region [ Constructor ]
        #endregion

        #region [ Methods ]
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
        #endregion
    }
}
