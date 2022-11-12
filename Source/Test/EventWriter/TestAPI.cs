using Adapt.Models;
using AdaptLogic;
using GemstoneAnalytic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable HeuristicUnreachableCode
namespace EventWriterTests
{
    [TestClass]
    public class TestAPI
    {
    #if DEBUG
        private const bool IsDebug = true;
    #else
        private const bool IsDebug = false;
    #endif
        public TestContext TestContext { get; set; }

        public AdaptSignal TestSignal = new AdaptSignal("Key", "Test Signal", "Device", 30) { Type = MeasurementType.EventFlag };

        private static readonly string DataPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}{Path.DirectorySeparatorChar}ADAPT{Path.DirectorySeparatorChar}dataTree{Path.DirectorySeparatorChar}";

        [TestMethod]
        public void GeneralWriteTest()
        {
            SignalWritter.CleanAppData();
            List<AdaptEvent> events = new List<AdaptEvent>()
            {
                new AdaptEvent("GeneralWriteTest",new DateTime(2020,1,1,0,0,0,0),1),
                new AdaptEvent("GeneralWriteTest",new DateTime(2020,1,1,0,0,1,0),1),
                new AdaptEvent("GeneralWriteTest",new DateTime(2020,1,1,0,0,2,0),1),
            };

            WriteEvents(events, new AdaptSignal("GeneralWriteTest",TestSignal));
            ReadSummary();
            ReadSummaries(SignalReader.GetAvailableReader().First().SignalGuid,2020,1,1,0,0);
        }

        [TestMethod]
        public void GapWriteTest()
        {
            SignalWritter.CleanAppData();
            List<AdaptEvent> events = new List<AdaptEvent>()
            {
                new AdaptEvent("GapWriteTest",new DateTime(2020,1,1,0,0,0,0),1),
                new AdaptEvent("GapWriteTest",new DateTime(2020,1,1,1,0,1,0),1),
                new AdaptEvent("GapWriteTest",new DateTime(2020,1,1,5,0,2,0),1),
            };

            WriteEvents(events, new AdaptSignal("GapWriteTest", TestSignal));
            ReadSummary();
            ReadSummaries(SignalReader.GetAvailableReader().First().SignalGuid, 2020, 1, 1, 0, 0);
        }

        [TestMethod]
        public void OverflowWriteTest()
        {
            SignalWritter.CleanAppData();
            List<AdaptEvent> events = new List<AdaptEvent>()
            {
                new AdaptEvent("OverflowWriteTest",new DateTime(2020,1,1,0,0,0,0),70*10000000L),
                new AdaptEvent("OverflowWriteTest",new DateTime(2020,1,1,1,0,1,0),3601*10000000L),
            };

            WriteEvents(events, new AdaptSignal("GapWriteTest", TestSignal));
            ReadSummary();
            ReadSummaries(SignalReader.GetAvailableReader().First().SignalGuid, 2020, 1, 1, 0, 0);
        }

        [TestMethod]
        public void EmptyWriteTest()
        {
            SignalWritter.CleanAppData();
            List<AdaptEvent> events = new List<AdaptEvent>()
            {};

            WriteEvents(events, new AdaptSignal("EmptyWriteTest", TestSignal));
            ReadSummary();
            ReadSummaries(SignalReader.GetAvailableReader().First().SignalGuid, 2020, 1, 1, 0, 0);
        }

        private void WriteEvents(List<AdaptEvent> events, AdaptSignal signal)
        {
            CancellationTokenSource cancelationSource = new CancellationTokenSource();

            SignalWritter writter = new SignalWritter(signal);
            Task task = writter.StartWritter(cancelationSource.Token);
            foreach (AdaptEvent ev in events)
                writter.AddPoint(ev);
            writter.Complete();
            task.Wait();
        }

        private void ReadSummary()
        {
            List<SignalReader> readers = SignalReader.GetAvailableReader();
            Console.WriteLine($"Found {readers.Count()} Readers");

            int i = 0;
            foreach (SignalReader reader in readers)
            {
                EventSummary summary = reader.GetEventSummary(DateTime.MinValue, DateTime.MaxValue);
                List<AdaptEvent> data = reader.GetEvents(DateTime.MinValue, DateTime.MaxValue).ToList();
                Console.WriteLine($"{reader.Signal.Name} Phase: {reader.Signal.Phase.ToString()}; Device: {reader.Signal.Device} ");
                Console.WriteLine($"\t  {summary.Count} Events in Summary");
                Console.WriteLine($"\t  {summary.Sum} Total Time in Summary (Ticks)");

                Console.WriteLine($"\t  {data.Count()} Events in Data");
                Console.WriteLine($"\t  {data.Sum(e => e.Value)} Total Time in Data (Ticks)");
            }



            
        }

        private void ReadSummaries(string guid, int year, int month, int day, int hours, int minute)
        {
            string path = $"{DataPath}{guid}";
            Console.WriteLine("##### Year ####");
            path += Path.DirectorySeparatorChar + year.ToString("00");
            PrintEventSummary(ReadSummary(path));
            Console.WriteLine("##### Month ####");
            path += Path.DirectorySeparatorChar + month.ToString("00");
            PrintEventSummary(ReadSummary(path));
            Console.WriteLine("##### Day ####");
            path += Path.DirectorySeparatorChar + day.ToString("00");
            PrintEventSummary(ReadSummary(path));
            Console.WriteLine("##### Hour ####");
            path += Path.DirectorySeparatorChar + hours.ToString("00");
            PrintEventSummary(ReadSummary(path));
            Console.WriteLine("##### Minute ####");
            path += Path.DirectorySeparatorChar + minute.ToString("00");
            PrintEventSummary(ReadSummary(path));
        }
    

        private EventSummary ReadSummary(string folder)
        {
            if (!File.Exists(folder + Path.DirectorySeparatorChar + "summary.node"))
                return new EventSummary() { Tmin = DateTime.MinValue, Tmax = DateTime.MaxValue, Sum = 0, Count = 0, Continuation = false, Max = 0, Min = 0 };
            byte[] data = File.ReadAllBytes(folder + Path.DirectorySeparatorChar + "summary.node");
            return new EventSummary(data);
        }

        private void PrintEventSummary(EventSummary summary)
        {
            Console.WriteLine($"\t {summary.Count} Events for {summary.Sum} Ticks");
            Console.WriteLine($"\t {summary.Count} Events for {summary.Sum/Gemstone.Ticks.PerSecond} Seconds");
            Console.WriteLine($"\t Starting {summary.Tmin.ToString()}");
            Console.WriteLine($"\t Ending {summary.Tmax.ToString()}");
        }
    }

}
