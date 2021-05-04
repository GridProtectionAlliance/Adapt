//******************************************************************************************************
//  Serialization.cs - Gbtc
//
//  Copyright © 2012, Grid Protection Alliance.  All Rights Reserved.
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
//  06/08/2006 - Pinal C. Patel
//       Original version of source code generated.
//  09/09/2008 - J. Ritchie Carroll
//       Converted to C#.
//  09/09/2008 - J. Ritchie Carroll
//       Added TryGetObject overloads.
//  02/16/2009 - Josh L. Patterson
//       Edited Code Comments.
//  08/4/2009 - Josh L. Patterson
//       Edited Code Comments.
//  09/14/2009 - Stephen C. Wills
//       Added new header and license agreement.
//  04/06/2011 - Pinal C. Patel
//       Modified GetString() method to not check for the presence of Serializable attribute on the 
//       object being serialized since this is not required by the XmlSerializer.
//  04/08/2011 - Pinal C. Patel
//       Moved Serialize() and Deserialize() methods from GSF.Services.ServiceModel.Serialization class
//       in GSF.Services.dll to consolidate serialization methods.
//  12/14/2012 - Starlynn Danyelle Gilliam
//       Modified Header.
//  04/05/2021 - C. Lackner
//       Moved to .NET Core for ADAPT
//
//******************************************************************************************************

using Gemstone;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace GemstoneCommon
{
    #region [ Enumerations ]

    /// <summary>
    /// Indicates the format of <see cref="object"/> serialization or deserialization.
    /// </summary>
    public enum SerializationFormat
    {
        /// <summary>
        /// <see cref="object"/> is serialized or deserialized using <see cref="DataContractSerializer"/> to XML (eXtensible Markup Language) format.
        /// </summary>
        /// <remarks>
        /// <see cref="object"/> can be serialized or deserialized using <see cref="XmlSerializer"/> by adding the <see cref="XmlSerializerFormatAttribute"/> to the <see cref="object"/>.
        /// </remarks>
        Xml,
        /// <summary>
        /// <see cref="object"/> is serialized or deserialized using <see cref="DataContractJsonSerializer"/> to JSON (JavaScript Object Notation) format.
        /// </summary>
        Json,
        /// <summary>
        /// <see cref="object"/> is serialized or deserialized using <see cref="BinaryFormatter"/> to binary format.
        /// </summary>
        Binary
    }

    #endregion

    /// <summary>
    /// Common serialization related functions.
    /// </summary>
    public static class Serialization
    {
        /// <summary>
        /// Gets <see cref="SerializationInfo"/> value for specified <paramref name="name"/>; otherwise <paramref name="defaultValue"/>.
        /// </summary>
        /// <typeparam name="T">Type of parameter to get from <see cref="SerializationInfo"/>.</typeparam>
        /// <param name="info"><see cref="SerializationInfo"/> object that contains deserialized values.</param>
        /// <param name="name">Name of deserialized parameter to retrieve.</param>
        /// <param name="defaultValue">Default value to return if <paramref name="name"/> does not exist or cannot be deserialized.</param>
        /// <returns>Value for specified <paramref name="name"/>; otherwise <paramref name="defaultValue"/></returns>
        /// <remarks>
        /// <see cref="SerializationInfo"/> do not have a direct way of determining if an item with a specified name exists, so when calling
        /// one of the Get(n) functions you will simply get a <see cref="SerializationException"/> if the parameter does not exist; similarly
        /// you will receive this exception if the parameter fails to properly deserialize. This extension method protects against both of
        /// these failures and returns a default value if the named parameter does not exist or cannot be deserialized.
        /// </remarks>
        public static T GetOrDefault<T>(this SerializationInfo info, string name, T defaultValue)
        {
            try
            {
                return (T)info.GetValue(name, typeof(T));
            }
            catch (SerializationException)
            {
                return defaultValue;
            }
        }

    }
}