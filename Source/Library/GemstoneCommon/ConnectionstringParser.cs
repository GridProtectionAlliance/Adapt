//******************************************************************************************************
//  ConnectionStringParser.cs - Gbtc
//
//  Copyright © 2013, Grid Protection Alliance.  All Rights Reserved.
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
//  10/14/2013 - Stephen C. Wills
//       Generated original version of source code.
//  03/10/2017 - J. Ritchie Carroll
//       Added checks for validation attributes.
//  04/01/2021 - C. Lackner
//       Moved to .NET Core.
//
//******************************************************************************************************
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Gemstone.Reflection.MemberInfoExtensions;
using Gemstone.StringExtensions;
using Microsoft.Extensions.Configuration;

namespace GemstoneCommon
{
    public class ConnectionStringParser
    {

        #region [ Internal classes ]

        /// <summary>
        /// Stores reflected information from a <see cref="PropertyInfo"/>
        /// object used to parse connection strings.
        /// </summary>
        protected class ConnectionStringProperty
        {
            /// <summary>
            /// The <see cref="PropertyInfo"/> object.
            /// </summary>
            public PropertyInfo PropertyInfo;

            /// <summary>
            /// The type converter used to convert the value
            /// of this property to and from a string.
            /// </summary>
            public TypeConverter Converter;

            /// <summary>
            /// The name of the property as it appears in the connection string.
            /// </summary>
            public string[] Names;

            /// <summary>
            /// The default value of the property if its value
            /// is not explicitly specified in the connection string.
            /// </summary>
            public object DefaultValue;

            /// <summary>
            /// Indicates whether or not the property is required
            /// to be explicitly defined in the connection string.
            /// </summary>
            public bool Required;

            /// <summary>
            /// Gets all validation attributes that may be applied to the property.
            /// </summary>
            public ValidationAttribute[] ValidationAttributes;

            /// <summary>
            /// Creates a new instance of the <see cref="ConnectionStringProperty"/> class.
            /// </summary>
            /// <param name="propertyInfo">The <see cref="PropertyInfo"/> object.</param>
            public ConnectionStringProperty(PropertyInfo propertyInfo)
            {
                SettingNameAttribute settingNameAttribute;
                DefaultValueAttribute defaultValueAttribute;
                TypeConverterAttribute typeConverterAttribute;
                Type converterType;

                PropertyInfo = propertyInfo;
                Names = propertyInfo.TryGetAttribute(out settingNameAttribute) ? settingNameAttribute.Names : new[] { propertyInfo.Name };

                bool hasDefaultValue = propertyInfo.TryGetAttribute(out defaultValueAttribute);

                Required = !hasDefaultValue;

                if (Required)
                    DefaultValue = null;
               else
                DefaultValue = defaultValueAttribute.Value;
                    
                

                if (propertyInfo.TryGetAttribute(out typeConverterAttribute))
                {
                    converterType = Type.GetType(typeConverterAttribute.ConverterTypeName);

                    if ((object)converterType != null)
                        Converter = (TypeConverter)Activator.CreateInstance(converterType);
                }

                propertyInfo.TryGetAttributes(out ValidationAttributes);
            }

        }

         #endregion

        #region [ Members ]
        // Constants

        /// <summary>
        /// Default value for the <see cref="ParameterDelimiter"/> property.
        /// </summary>
        public const char DefaultParameterDelimiter = ';';

        /// <summary>
        /// Default value for the <see cref="KeyValueDelimiter"/> property.
        /// </summary>
        public const char DefaultKeyValueDelimiter = '=';

        /// <summary>
        /// Default value for the <see cref="StartValueDelimiter"/> property.
        /// </summary>
        public const char DefaultStartValueDelimiter = '{';

        /// <summary>
        /// Default value for the <see cref="EndValueDelimiter"/> property.
        /// </summary>
        public const char DefaultEndValueDelimiter = '}';

        /// <summary>
        /// Default value for the <see cref="ExplicitlySpecifyDefaults"/> property.
        /// </summary>
        public const bool DefaultExplicitlySpecifyDefaults = false;

        // Fields
        protected char m_parameterDelimiter;
        protected char m_keyValueDelimiter;
        protected char m_startValueDelimiter;
        protected char m_endValueDelimiter;
        protected bool m_explicitlySpecifyDefaults;

        #endregion

        #region [ Constructor ]

        /// <summary>
        /// Creates a new instance of the <see cref="ConnectionStringParser"/> class.
        /// </summary>
        public ConnectionStringParser()
        {
            m_parameterDelimiter = DefaultParameterDelimiter;
            m_keyValueDelimiter = DefaultKeyValueDelimiter;
            m_startValueDelimiter = DefaultStartValueDelimiter;
            m_endValueDelimiter = DefaultEndValueDelimiter;
        }

        #endregion

        #region [ Properties ]
        /// <summary>
        /// Gets or sets the parameter delimiter used to
        /// separate key-value pairs in the connection string.
        /// </summary>
        public char ParameterDelimiter
        {
            get
            {
                return m_parameterDelimiter;
            }
            set
            {
                m_parameterDelimiter = value;
            }
        }

        /// <summary>
        /// Gets or sets the key-value delimiter used to
        /// separate keys from values in the connection string.
        /// </summary>
        public char KeyValueDelimiter
        {
            get
            {
                return m_keyValueDelimiter;
            }
            set
            {
                m_keyValueDelimiter = value;
            }
        }

        /// <summary>
        /// Gets or sets the start value delimiter used to denote the
        /// start of a value in the cases where the value contains one
        /// of the delimiters defined for the connection string.
        /// </summary>
        public char StartValueDelimiter
        {
            get
            {
                return m_startValueDelimiter;
            }
            set
            {
                m_startValueDelimiter = value;
            }
        }

        /// <summary>
        /// Gets or sets the end value delimiter used to denote the
        /// end of a value in the cases where the value contains one
        /// of the delimiters defined for the connection string.
        /// </summary>
        public char EndValueDelimiter
        {
            get
            {
                return m_endValueDelimiter;
            }
            set
            {
                m_endValueDelimiter = value;
            }
        }

        /// <summary>
        /// Gets or sets the flag that determines whether to explicitly
        /// specify parameter values that match their defaults when
        /// serializing settings to a connection string.
        /// </summary>
        public bool ExplicitlySpecifyDefaults
        {
            get
            {
                return m_explicitlySpecifyDefaults;
            }
            set
            {
                m_explicitlySpecifyDefaults = value;
            }
        }

        #endregion

        #region [ Methods ]

        public virtual List<KeyValuePair<string, string>> ParseConnectionString(string ConnectionString)
        {
            Dictionary<string, string>  settings = ConnectionString.ParseKeyValuePairs(m_parameterDelimiter, m_keyValueDelimiter, m_startValueDelimiter, m_endValueDelimiter);

            return settings.ToList();

        }

        public virtual string ComposeConnectionstring(List<KeyValuePair<string, string>> settings)
        {
            return settings.ToDictionary(item => item.Key, item => item.Value).JoinKeyValuePairs(m_parameterDelimiter, m_keyValueDelimiter, m_startValueDelimiter, m_endValueDelimiter);
        }

        public virtual List<KeyValuePair<string, string>> ParseConnectionString(string ConnectionString, Type settingsType)
        {
            ConnectionStringProperty[] connectionStringProperties;
            Dictionary<string, string> settings;
            string key;
            string value;
            Dictionary<string, string> settingsWithDefault = new Dictionary<string, string>();

            connectionStringProperties = GetConnectionStringProperties(settingsType);

            settings = ConnectionString.ParseKeyValuePairs(m_parameterDelimiter, m_keyValueDelimiter, m_startValueDelimiter, m_endValueDelimiter);

            foreach (ConnectionStringProperty property in connectionStringProperties)
            {
                value = string.Empty;
                key = property.Names.FirstOrDefault(name => settings.TryGetValue(name, out value));

                if ((object)key != null)
                    settingsWithDefault.Add(key, value);
                else if (!property.Required)
                    settingsWithDefault.Add(property.Names.FirstOrDefault(), property.DefaultValue.ToString());

                else
                    throw new ArgumentException("Unable to parse required connection string parameter because it does not exist in the connection string.", property.Names.First());

            }

            return settingsWithDefault.ToList();

        }

        /// <summary>
        /// Gets the set of properties which are part of the connection string.
        /// </summary>
        /// <param name="settingsObjectType">The type of the settings object used to look up properties via reflection.</param>
        /// <returns>The set of properties which are part of the connection string.</returns>
        protected ConnectionStringProperty[] GetConnectionStringProperties(Type settingsObjectType)
        {
            return s_connectionStringPropertiesLookup.GetOrAdd(settingsObjectType, s_valueFactory);
        }

        #endregion

        #region [ Static ]

        // Static Fields
        protected static readonly ConcurrentDictionary<Type, ConnectionStringProperty[]> s_connectionStringPropertiesLookup = new ConcurrentDictionary<Type, ConnectionStringProperty[]>();

        protected static readonly Func<Type, ConnectionStringProperty[]> s_valueFactory = t =>
        {

            return t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.CanRead && property.CanWrite)
                .Select(property => new ConnectionStringProperty(property))
                .ToArray();
        };

        #endregion
    }

    public class ConnectionStringParser<T>: ConnectionStringParser where T : class, new()
    {

        #region [ Methods ]

        public override List<KeyValuePair<string, string>> ParseConnectionString(string ConnectionString) => ParseConnectionString(ConnectionString, typeof(T));
    
        public T ParseConnectionStringToObject(string ConnectionString)
        {
            ConnectionStringProperty[] connectionStringProperties;
            Dictionary<string, string> settings;
            string key;
            string value;
            T result = new T();

            connectionStringProperties = GetConnectionStringProperties(typeof(T));



            // If there are no properties, then our work is done
            if (connectionStringProperties.Length <= 0)
                return result;

            settings = ConnectionString.ParseKeyValuePairs(m_parameterDelimiter, m_keyValueDelimiter, m_startValueDelimiter, m_endValueDelimiter);

            foreach (ConnectionStringProperty property in connectionStringProperties)
            {
                value = string.Empty;
                key = property.Names.FirstOrDefault(name => settings.TryGetValue(name, out value));

                if ((object)key != null)
                    property.PropertyInfo.SetValue(result, ConvertToPropertyType(value, property));
                else if (!property.Required)
                    property.PropertyInfo.SetValue(result, property.DefaultValue);
                else
                    throw new ArgumentException("Unable to parse required connection string parameter because it does not exist in the connection string.", property.Names.First());

                if ((object)property.ValidationAttributes == null)
                    continue;

                object propertyValue = property.PropertyInfo.GetValue(result);
                string propertyName = key ?? property.Names.First();

                foreach (ValidationAttribute attr in property.ValidationAttributes)
                    attr.Validate(propertyValue, propertyName);
            }

            return result;
        }

        public string ComposeConnectionstring(T SettingsObject)
        {
            ConnectionStringProperty[] connectionStringProperties;
            Dictionary<string, string> settings;
            connectionStringProperties = GetConnectionStringProperties(typeof(T));



            // If there are no properties, then our work is done
            if (connectionStringProperties.Length <= 0)
                return "";

            // Create a dictionary of key-value pairs which
            // can easily be converted to a connection string
            settings = connectionStringProperties
                .Select(property => Tuple.Create(property, property.PropertyInfo.GetValue(SettingsObject)))
                .Where(tuple => tuple.Item2 != null && (m_explicitlySpecifyDefaults || !tuple.Item2.Equals(tuple.Item1.DefaultValue)))
                .ToDictionary(tuple => tuple.Item1.Names.First(), tuple => ConvertToString(tuple.Item2, tuple.Item1), StringComparer.CurrentCultureIgnoreCase);

            // Convert the dictionary to a connection string and return the result
            return settings.JoinKeyValuePairs(m_parameterDelimiter, m_keyValueDelimiter, m_startValueDelimiter, m_endValueDelimiter);
        }

        

        /// <summary>
        /// Converts the given string value to the type of the given property.
        /// </summary>
        /// <param name="value">The string value to be converted.</param>
        /// <param name="property">The property used to determine what type to convert to.</param>
        /// <returns>The given string converted to the type of the given property.</returns>
        protected virtual object ConvertToPropertyType(string value, ConnectionStringProperty property)
        {
            return ((object)property.Converter != null)
                ? property.Converter.ConvertFromString(value)
                : value.ConvertToType(property.PropertyInfo.PropertyType);
        }

        /// <summary>
        /// Converts the given object to a string.
        /// </summary>
        /// <param name="obj">The object to be converted.</param>
        /// <param name="property">The property which defines the type of the object.</param>
        /// <returns>The object converted to a string.</returns>
        protected virtual string ConvertToString(object obj, ConnectionStringProperty property)
        {
            return ((object)property.Converter != null)
                ? property.Converter.ConvertToString(obj)
                : Gemstone.Common.TypeConvertToString(obj);
        }

        #endregion


        
    }
}
