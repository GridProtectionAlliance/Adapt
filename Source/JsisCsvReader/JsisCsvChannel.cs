using Adapt.Models;
using GemstoneCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsisCsvReader
{
    public class JsisCsvChannel
    {
        #region [ Members ]
        private Phase m_phase;
        private MeasurementType m_type;
        private string m_Name;
        private string m_description;
        private string m_unit;
        private string m_device;
        private string m_pmuName;
        private double m_fps;
        #endregion

        #region [ Constructor ]
        public JsisCsvChannel(string device)
        {
            m_pmuName = device;
        }
        #endregion

        #region [ Properties ]
        //public string ID => m_Key;

        public double FramesPerSecond
        {
            get => m_fps;
            set => m_fps = value;
        }

        /// <summary>
        /// The Name of this Signal in user readable form.
        /// </summary>
        public string Name
        {
            get => m_Name;
            set { m_Name = value; }
        }

        public string Device => m_pmuName;

        public Phase Phase
        {
            get => m_phase;
            set => m_phase = value;
        }
        public MeasurementType Type
        {
            get => m_type;
            set => m_type = value;
        }
        public string Description
        {
            get => m_description;
            set => m_description = value;

        }
        public string Unit
        {
            get => m_unit;
            set => m_unit = value;

        }

        #endregion

    }
}
