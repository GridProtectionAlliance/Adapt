

//******************************************************************************************************
//  DateSelectViewModel.cs - Gbtc
//
//  Copyright © 2021, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://www.opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  06/09/2011 - Stephen C. Wills
//       Generated original version of source code.
//  12/20/2012 - Starlynn Danyelle Gilliam
//       Modified Header.
//  03/30/2021 - C. Lackner
//       Moved to .NET Core.
//
//******************************************************************************************************

using Adapt.Models;
using Gemstone.Reflection.MemberInfoExtensions;
using Gemstone.StringExtensions;
using GemstoneCommon;
using GemstoneWPF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;


namespace Adapt.ViewModels.Common
{
    /// <summary>
    /// View-model <see cref="Adapt.View.Common.DateSelect"/>
    /// </summary>
    public class DateSelectVM : ViewModelBase
    {
        #region [ Members ]
        private DateTime m_start;
        private DateTime m_end;
        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets or sets the first <see cref="DateTime"/>
        /// </summary>
        public DateTime Start
        {
            get
            {
                return m_start;
            }
            set
            {
                m_start = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Duration));
                OnPropertyChanged(nameof(StartDate));
                OnPropertyChanged(nameof(StartHour));
                OnPropertyChanged(nameof(StartMinute));
                OnPropertyChanged(nameof(StartSecond));
                }
        }


        /// <summary>
        /// Gets or sets the first Date.
        /// </summary>
        public DateTime StartDate
        {
            get
            {
                return m_start.Date;
            }
            set
            {
                m_start = value.AddSeconds(StartSecond).AddHours(StartHour).AddMinutes(StartMinute);
                OnPropertyChanged();
                OnPropertyChanged(nameof(Start));
                OnPropertyChanged(nameof(Duration));
            }
        }

        /// <summary>
        /// Gets or sets the first Hour.
        /// </summary>
        public int StartHour
        {
            get
            {
                return m_start.Hour;
            }
            set
            {
                m_start = StartDate.AddSeconds(StartSecond).AddHours(value).AddMinutes(StartMinute);
                OnPropertyChanged();
                OnPropertyChanged(nameof(Start));
                OnPropertyChanged(nameof(Duration));
            }
        }

        /// <summary>
        /// Gets or sets the first Minute.
        /// </summary>
        public int StartMinute
        {
            get
            {
                return m_start.Minute;
            }
            set
            {
                m_start = StartDate.AddSeconds(StartSecond).AddHours(StartHour).AddMinutes(value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(Start));
                OnPropertyChanged(nameof(Duration));
            }
        }

        /// <summary>
        /// Gets or sets the first Second.
        /// </summary>
        public int StartSecond
        {
            get
            {
                return m_start.Second;
            }
            set
            {
                m_start = StartDate.AddSeconds(value).AddHours(StartHour).AddMinutes(StartMinute);
                OnPropertyChanged();
                OnPropertyChanged(nameof(Start));
                OnPropertyChanged(nameof(Duration));
            }
        }

        /// <summary>
        /// Gets or sets the second <see cref="DateTime"/>
        /// </summary>
        public DateTime End
        {
            get
            {
                return m_end;
            }
            set
            {
                m_end = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Duration));
                OnPropertyChanged(nameof(EndDate));
                OnPropertyChanged(nameof(EndHour));
                OnPropertyChanged(nameof(EndMinute));
                OnPropertyChanged(nameof(EndSecond));
            }
        }

        /// <summary>
        /// Gets or sets the second Date.
        /// </summary>
        public DateTime EndDate
        {
            get
            {
                return m_end.Date;
            }
            set
            {
                m_end = value.AddSeconds(EndSecond).AddHours(EndHour).AddMinutes(EndMinute);
                OnPropertyChanged();
                OnPropertyChanged(nameof(End));
                OnPropertyChanged(nameof(Duration));
            }
        }

        /// <summary>
        /// Gets or sets the second Hour.
        /// </summary>
        public int EndHour
        {
            get
            {
                return m_end.Hour;
            }
            set
            {
                m_end = EndDate.AddSeconds(EndSecond).AddHours(value).AddMinutes(EndMinute);
                OnPropertyChanged();
                OnPropertyChanged(nameof(End));
                OnPropertyChanged(nameof(Duration));
            }
        }

        /// <summary>
        /// Gets or sets the second Minute.
        /// </summary>
        public int EndMinute
        {
            get
            {
                return m_end.Minute;
            }
            set
            {
                m_end = EndDate.AddSeconds(EndSecond).AddHours(EndHour).AddMinutes(value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(End));
                OnPropertyChanged(nameof(Duration));
            }
        }

        /// <summary>
        /// Gets or sets the second Second.
        /// </summary>
        public int EndSecond
        {
            get
            {
                return m_end.Second;
            }
            set
            {
                m_end = EndDate.AddSeconds(value).AddHours(EndHour).AddMinutes(EndMinute);
                OnPropertyChanged();
                OnPropertyChanged(nameof(End));
                OnPropertyChanged(nameof(Duration));
            }
        }

        /// <summary>
        /// Gets a string showing the selected Duration
        /// </summary>
        public string Duration
        {
            get
            {
                string result = "(";
                TimeSpan span = End - Start;

                if (span.TotalDays > 0)
                    result = result + $" {Math.Floor(span.TotalDays)} days";
                if (span.Hours > 0)
                    result = result + $" {span.Hours} hours";
                if (span.Minutes > 0)
                    result = result + $" {span.Minutes} minutes";
                if (span.Seconds > 0)
                    result = result + $" {span.Seconds} seconds";

                result = result + " )";
                return result;
            }
        }
        #endregion

        #region [ Constructor ]

        public DateSelectVM()
        {
            try
            {
                if (!File.Exists(TempDateTimeFile))
                {
                    m_end = DateTime.UtcNow;
                    m_start = m_end.Subtract(new TimeSpan(0, 10, 0));
                    WriteTempFile();
                }
                else
                    ReadTempFile();
                
            }
            catch (Exception ex)
            {
                m_end = DateTime.UtcNow;
                m_start = m_end.Subtract(new TimeSpan(0, 10, 0));
            }

            this.PropertyChanged += UpdateFile;
          
        }

        private void UpdateFile(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Start" || e.PropertyName == "End")
                try
                {
                    WriteTempFile();
                }
                catch { }
        }

        private void WriteTempFile()
        {
            byte[] data = new byte[16];
            BitConverter.GetBytes(m_start.ToBinary()).CopyTo(data, 0);
            BitConverter.GetBytes(m_end.ToBinary()).CopyTo(data, 8);

            using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(TempDateTimeFile)))
            {                
                writer.Write(data);
                writer.Flush();
                writer.Close();
            }
        }

        private void ReadTempFile()
        {
            byte[] data = File.ReadAllBytes(TempDateTimeFile);
            m_start = DateTime.FromBinary(BitConverter.ToInt64(data, 0));
            m_end = DateTime.FromBinary(BitConverter.ToInt64(data, 8));
        }
        #endregion

        #region [ Static ]

        private static readonly string TempDateTimeFile = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}{Path.DirectorySeparatorChar}Adapt{Path.DirectorySeparatorChar}DateTimes.tmp";

        #endregion
    }
}
