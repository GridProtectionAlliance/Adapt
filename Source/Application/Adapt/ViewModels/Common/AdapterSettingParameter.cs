//******************************************************************************************************
//  AdapterSettingParameter.cs - Gbtc
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
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;


namespace Adapt.ViewModels.Common
{
    /// <summary>
    /// View-model for a key-value pair in an adapter connection string.
    /// This can also represent key-value pairs which aren't necessarily
    /// in the connection string, but rather are defined by properties
    /// in an adapter class. This view-model is used by the
    /// <see cref="AdapterSettingParameter"/>.
    /// </summary>
    public class AdapterSettingParameterVM : ViewModelBase
    {
        #region [ Members ]

        // Fields
        private PropertyInfo m_info;
        private string m_name;
        private string m_description;
        private object m_value;
        private object m_defaultValue;
        private bool m_isRequired;
        private bool m_customPopUpOpen;
        private RelayCommand m_customButtonCmd;
        private UIElement m_customPopup;

        private string[] m_ConnectionSTringNames;

        #endregion

        #region [ Constructor ]

        public AdapterSettingParameterVM()
        {
            m_customButtonCmd = new RelayCommand(OpenCustom, () => true);
        }
        #endregion
        #region [ Properties ]

        /// <summary>
        /// Triggered anytime the Setting is changed.
        /// </summary>
        public event EventHandler<SettingChangedArg> SettingChanged;

        /// <summary>
        /// Gets or sets the <see cref="PropertyInfo"/> of the
        /// Parameter
        /// associated with this <see cref="AdapterSettingParameter"/>.
        /// </summary>
        public PropertyInfo Info
        {
            get
            {
                return m_info;
            }
            set
            {
                m_info = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the name of the <see cref="AdapterSettingParameter"/>
        /// which is either a key in the connection string or the name of a property in
        /// the adapter class.
        /// </summary>
        public string Name
        {
            get
            {
                return m_name;
            }
            set
            {
                m_name = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the description of the <see cref="AdapterSettingParameter"/>
        /// obtained through the <see cref="Info"/> using reflection. 
        /// The property must define a <see cref="System.ComponentModel.DescriptionAttribute"/> for this
        /// to become populated.
        /// </summary>
        public string Description
        {
            get
            {
                return m_description;
            }
            set
            {
                m_description = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the value of the <see cref="AdapterSettingParameter"/> as defined
        /// by either the connection string or the <see cref="DefaultValue"/> of the parameter.
        /// </summary>
        public object Value
        {
            get
            {
                return m_value ?? m_defaultValue;
            }
            set
            {
                m_value = value;
                SettingChanged?.Invoke(this, new SettingChangedArg() { Name = m_name, Value = value });
                OnPropertyChanged();
            
            }
        }

        /// <summary>
        /// Gets or sets the default value of the <see cref="AdapterSettingParameter"/>
        /// obtained through the <see cref="Info"/> using reflection. A Property must
        /// define a <see cref="System.ComponentModel.DefaultValueAttribute"/> for this to
        /// be populated.
        /// </summary>
        public object DefaultValue
        {
            get
            {
                return m_defaultValue;
            }
            set
            {
                m_defaultValue = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether this parameter is defined by a property
        /// that is annotated with the <see cref="System.ComponentModel.DefaultValueAttribute"/>.
        /// </summary>
        public bool IsRequired
        {
            get
            {
                return m_isRequired;
            }
            set
            {
                m_isRequired = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets a list of enum types if this <see cref="AdapterSettingParameter"/>'s type is an enum.
        /// If it is not an enum, this returns null.
        /// </summary>
        public List<string> EnumValues
        {
            get
            {
                if (!IsEnum)
                    return null;

                return Enum.GetValues(m_info.PropertyType)
                    .Cast<object>()
                    .Select(obj => obj.ToString())
                    .ToList();
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="System.Windows.Controls.RadioButton"/>
        /// labeled "False" is checked. Since the actual value of this <see cref="AdapterSettingParameter"/>
        /// is represented by <see cref="Value"/>, and that value is what goes into the connection string
        /// this simply returns true if the value in the Value property is the word "false".
        /// </summary>
        public bool IsFalseChecked
        {
            get
            {
                return Value.ToNonNullString().Equals(false.ToString(), StringComparison.CurrentCultureIgnoreCase);
            }
        }

        
        /// <summary>
        /// Gets a value that indicates whether the type of this <see cref="AdapterSettingParameter"/>
        /// is defined to be a <see cref="bool"/> in the adapter type. This determines whether the
        /// <see cref="System.Windows.Controls.RadioButton"/>s labeled "True" and "False" are visible.
        /// </summary>
        public bool IsBoolean
        {
            get
            {
                return (m_info != null) && (m_info.PropertyType == typeof(bool)) && !IsCustom;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the type of this <see cref="AdapterSettingParameter"/>
        /// is defined to be an enum in the adapter type. This determines whether the
        /// <see cref="System.Windows.Controls.ComboBox"/> bound to the enum values is visible.
        /// </summary>
        public bool IsEnum
        {
            get
            {
                return (m_info != null) && m_info.PropertyType.IsEnum && !IsCustom;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the type of this <see cref="AdapterSettingParameter"/>
        /// is defined to be an string in the adapter type. This determines whether the
        /// <see cref="System.Windows.Controls.TextBox"/> bound to the string values is visible.
        /// </summary>
        public bool IsText
        {
            get
            {
                return (m_info != null) && m_info.PropertyType == typeof(string) && !IsCustom;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the type of this <see cref="AdapterSettingParameter"/>
        /// is defined to be an number type in the adapter type. This determines whether the
        /// <see cref="System.Windows.Controls.TextBox"/> bound to the number values is visible.
        /// </summary>
        public bool IsNumeric
        {
            get
            {
                return (m_info != null) && !IsCustom && (m_info.PropertyType == typeof(int) || m_info.PropertyType == typeof(double) || m_info.PropertyType == typeof(float));
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the value of this parameter can be configured via a
        /// custom control. This determines whether the hyper-link that links to the custom configuration
        /// pop-up is visible.
        /// </summary>
        public bool IsCustom
        {
            get
            {
                try
                {
                    return !(m_info?.GetCustomAttribute<CustomConfigurationEditorAttribute>() is null);
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets a value that indicates whether custom setting popup control is visible.
        /// </summary>
        public bool CustomPopupOpen
        {
            get
            {
                return IsCustom && m_customPopUpOpen;
            }
            set
            {
                m_customPopUpOpen = value;
                OnPropertyChanged();
            }
        }

        public UIElement CustomPopup
        { 
            get { return m_customPopup; }
            set
            {
                m_customPopup = value;
                OnPropertyChanged();
            }
        }
        public ICommand OpenCustomPopup => m_customButtonCmd;
      

        /// <summary>
        /// Gets a value that indicates whether the type of this <see cref="AdapterSettingParameter"/>
        /// is something other than a <see cref="bool"/> or an enum. This determines whether the
        /// <see cref="System.Windows.Controls.TextBox"/> bound to the <see cref="Value"/> of the parameters is
        /// visible. The value is true for most parameters, including those which are not defined by an adapter type.
        /// </summary>
        public bool IsOther
        {
            get
            {
                return!IsBoolean && !IsEnum && !IsText && !IsNumeric && !IsCustom;
            }
        }


        /// <summary>
        /// Gets or Sets the Names used in the Connectionstring. This can be different then <see cref="Name"/>
        /// </summary>
        public string[] ConnectionStringNames
        {
            get => m_ConnectionSTringNames;
            set => m_ConnectionSTringNames = value;
        }

        #endregion

        #region[ Methods ]
        public void OpenCustom()
        {
            try
            { 
                CustomConfigurationEditorAttribute customConfigurationEditorAttribute = Info.GetCustomAttribute<CustomConfigurationEditorAttribute>();

                if (customConfigurationEditorAttribute is null)
                    return;

                Action<object> complete = new Action<object>((object val) => {
                    m_customPopUpOpen = false;
                    OnPropertyChanged(nameof(CustomPopupOpen));
                    Value = val;
                });

                if (customConfigurationEditorAttribute.ConnectionString is null)
                    CustomPopup = Activator.CreateInstance(customConfigurationEditorAttribute.EditorType, Name, Value, complete) as UIElement;
                else
                    CustomPopup = Activator.CreateInstance(customConfigurationEditorAttribute.EditorType, Name, Value, complete, customConfigurationEditorAttribute.ConnectionString) as UIElement;

                if (m_customPopup is null)
                    return;

                CustomPopupOpen = true;
            }
            catch (Exception ex)
            {
                    string message = $"Unable to open custom configuration control due to exception: {ex.Message}";
                    Popup(message, "Custom Configuration Error", MessageBoxImage.Error);
            }
        }
        #endregion

        #region [ Static ]

        /// <summary>
        /// Gets the <see cref="AdapterSettingParameter"/> for a given <see cref="IAdapter"/>.
        /// </summary>
        /// <param name="dataSource">The <see cref="IAdapter"/> for which the Settings are.</param>
        /// <param name="connectionString">The current Connection string to be parsed.</param>
        /// <returns></returns>
        public static List<AdapterSettingParameterVM> GetSettingParameters(IAdapter dataSource, string connectionString) 
        {

            Type settingType = dataSource.SettingType;

            

            if (settingType != null)
            {

                ConnectionStringParser connectionStringParser = new ConnectionStringParser();
                Dictionary<string,string> settings = connectionStringParser.ParseConnectionString(connectionString).ToDictionary(item => item.Key,item => item.Value);
                // Get the list of properties
                IEnumerable<PropertyInfo> infoList = settingType.GetProperties()
                    .Where(info => info.CanWrite && info.CanRead);

                // Turn the List into AdapterSettingParameter
                return infoList.Select(info => GetParameter(info, settings))
                    .OrderByDescending(parameter => parameter.IsRequired)
                    .ThenBy(parameter => parameter.Name)
                    .ToList();
            }

            return new List<AdapterSettingParameterVM>();
        }

        public static string GetConnectionString(List<AdapterSettingParameterVM> parameters, IAdapter dataSource)
        {
            Type settingType = dataSource.SettingType;
            if (settingType == null)
                return "";

            ConnectionStringParser connectionStringParser = new ConnectionStringParser();
            return connectionStringParser.ComposeConnectionstring(parameters.Select(item => new KeyValuePair<string, string>(item.Name, item.Value?.ToString() ?? "")).ToList());
            
            
        }
        /// <summary>
        /// Retrieves an existing parameter from the <see cref="ParameterList"/>. If no
        /// parameter exists with a name that matches the given <see cref="MemberInfo.Name"/>,
        /// then a new one is created. There is also a special case in which the parameter is
        /// already defined, but no <see cref="PropertyInfo"/> exists for it. In that case,
        /// the property info is added, as well as any other new information, and the parameter
        /// is returned.
        /// </summary>
        /// <param name="info">The property info that defines the connection string parameter.</param>
        /// <returns>The parameter defined by the given property info.</returns>
        private static AdapterSettingParameterVM GetParameter(PropertyInfo info, Dictionary<string,string> settingsObject)
        {
            DefaultValueAttribute defaultValueAttribute;
            DescriptionAttribute descriptionAttribute;
            AdapterSettingParameterVM parameter = null;
            SettingNameAttribute settingNameAttribute;

            bool isRequired = false;
            object defaultValue = null;
            string value = null;
            string description = null;

            string[] Names = info.TryGetAttribute(out settingNameAttribute) ? settingNameAttribute.Names.Concat(new[] { info.Name }).ToArray() : new[] { info.Name };
            Names.FirstOrDefault(name => settingsObject.TryGetValue(name, out value));

            // These are different cases, but we need to extract the description
            // and default value in both. In cases where we already know this
            // information, we can skip this step.
            if (parameter == null || parameter.Info == null)
            {
                isRequired = !info.TryGetAttribute(out defaultValueAttribute);
                defaultValue = isRequired ? null : defaultValueAttribute.Value;
                description = info.TryGetAttribute(out descriptionAttribute) ? descriptionAttribute.Description : string.Empty;
            }

            if (parameter == null)
            {
                // Create a brand new parameter to be returned.
                parameter = new AdapterSettingParameterVM
                {
                    Info = info,
                    Name = info.Name,
                    Description = description,
                    Value = (object)value != null? ConvertToPropertyType(value,info) : null,
                    DefaultValue = defaultValue,
                    IsRequired = isRequired,
                    ConnectionStringNames = Names
                };
            }
            else if (parameter.Info == null)
            {
                // Update the existing parameter with newly obtained information.
                parameter.Info = info;
                parameter.Description = description;
                parameter.DefaultValue = defaultValue;
            }

           
            return parameter;
        }

        /// <summary>
        /// Converts the given string value to the type of the given property.
        /// </summary>
        /// <param name="value">The string value to be converted.</param>
        /// <param name="property">The property used to determine what type to convert to.</param>
        /// <returns>The given string converted to the type of the given property.</returns>
        private static object ConvertToPropertyType(string value, PropertyInfo property)
        {
            return value.ConvertToType(property.PropertyType);
        }


        #endregion
    }

    public class SettingChangedArg : EventArgs
    {
        public string Name;
        public object Value;
    }
}
