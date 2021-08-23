// ******************************************************************************************************
//  AnalyticSectionAttribute.tsx - Gbtc
//
//  Copyright © 2021, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may not use this
//  file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  08/02/2021 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************


using Gemstone.Data.Model;
using System;

namespace Adapt.Models
{
    /// <summary>
    /// Defines an attribute that determines the <see cref="AnalyticSection"/> associated with an Attributes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class AnalyticSectionAttribute : Attribute
    {
     
        /// <summary>
        /// Gets a list of <see cref="AnalyticSection"/> this Analytic can be used in.
        /// </summary>
        public AnalyticSection[] Sections
        {
            get;
        }

        /// <summary>
        /// Creates a new <see cref="AllowSearchAttribute"/>.
        /// </summary>
        /// <param name="Section"> The <see cref="AnalyticSection"/> this analytic is available in.</param>
        public AnalyticSectionAttribute(AnalyticSection Section)
        {
            this.Sections = new AnalyticSection[] { Section };
        }

        /// <summary>
        /// Creates a new <see cref="AllowSearchAttribute"/>.
        /// </summary>
        /// <param name="Sections"> A List of <see cref="AnalyticSection"/>s this analytic is available in.</param>
        public AnalyticSectionAttribute(params AnalyticSection[] Sections)
        {
            this.Sections = Sections;
        }
    }

}
