//******************************************************************************************************
//  EnumDescriptionHelper.cs - Gbtc
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
//  11/11/2021 - A. Hagemeyer
//       Generated original version of source code.
//
//******************************************************************************************************
using System;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace GemstoneCommon
{
    /// <summary>
    /// Returns descriptor or name with spaces of a given enum
    /// </summary>
    public static class EnumDescriptionHelper
    {
        public static string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            string st = null;
            if (attributes.Length > 0)
            {
                st = attributes[0].Description;
                if (st.Contains(':'))
                    return st.Substring(st.LastIndexOf(':') + 1);
                return st;
            }
            else
                return value.ToString();
        }

        public static string GetDisplayName(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            string st = null;
            if (attributes.Length > 0)
            {
                st = attributes[0].Description;
                if (st.Contains(':'))
                    return st.Substring(0, st.IndexOf(":"));
                return st;
            }
            else
                return value.ToString();
        }
    }
}
