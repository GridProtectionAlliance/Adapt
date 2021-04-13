// ******************************************************************************************************
//  DataSourceTypeDescription.tsx - Gbtc
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
//  03/29/2020 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************
using Adapt.Models;
using Gemstone.Data;
using Gemstone.Data.Model;
using Gemstone.IO;
using Gemstone.Reflection.MemberInfoExtensions;
using Gemstone.StringExtensions;
using Gemstone.TypeExtensions;
using GemstoneWPF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Adapt.ViewModels
{
    /// <summary>
    /// ViewModel for DataSource Type
    /// </summary>
    public class DataSourceTypeDescription
    {
        #region [ Properties ]
        public string Header
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        public Visibility HeaderVisibility
        {
            get
            {
                return ((object)Header != null) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Type Type { get; set; }

        #endregion

        #region[ Methods ]
        public override string ToString()
        {
            if ((object)Header != null)
                return string.Format("{0}: {1}", Header, Description);

            return Description;
        }
        #endregion

        #region [ Static ]

        /// <summary>
        /// Searches the <paramref name="searchDirectory"/> for assemblies containing
        /// implementations of the given adapter type. It then returns a collection
        /// containing all the implementations it found as well as their descriptions.
        /// </summary>
        /// <param name="searchDirectory">The directory in which to search for assemblies.</param>
        /// <param name="adapterType">The type to be searched for in the assemblies.</param>
        /// <returns>The collection of types found as well as their descriptions.</returns>
        public static List<DataSourceTypeDescription> LoadDataSourceTypes(string searchDirectory)
        {

            DescriptionAttribute descriptionAttribute;

            return typeof(IDataSource).LoadImplementations(searchDirectory, true)
                .Distinct()
                .Where(type => GetEditorBrowsableState(type) == EditorBrowsableState.Always)
                .Select(type => GetDescription(type))
                .OrderByDescending(pair => pair.Type.TryGetAttribute(out descriptionAttribute))
                .ThenBy(pair => pair.ToString())
                .ToList();
        }

        /// <summary>
        /// Gets the editor browsable state of the given type. This method will
        /// search for a <see cref="EditorBrowsableAttribute"/> using reflection.
        /// If none is found, it will default to <see cref="EditorBrowsableState.Always"/>.
        /// </summary>
        /// <param name="type">The type for which an editor browsable state is found.</param>
        /// <returns>
        /// Either the editor browsable state as defined by an <see cref="EditorBrowsableAttribute"/>
        /// or else <see cref="EditorBrowsableState.Always"/>.
        /// </returns>
        private static EditorBrowsableState GetEditorBrowsableState(Type type)
        {
            EditorBrowsableAttribute editorBrowsableAttribute;

            if (type.TryGetAttribute(out editorBrowsableAttribute))
                return editorBrowsableAttribute.State;

            return EditorBrowsableState.Always;
        }

        /// <summary>
        /// Gets a description of the given type. This method will search for a
        /// <see cref="DescriptionAttribute"/> using reflection. If none is found,
        /// it will default to the <see cref="Type.FullName"/> of the type.
        /// </summary>
        /// <param name="type">The type for which a description is found.</param>
        /// <returns>
        /// Either the description as defined by a <see cref="DescriptionAttribute"/>
        /// or else the <see cref="Type.FullName"/> of the parameter.
        /// </returns>
        private static DataSourceTypeDescription GetDescription(Type type)
        {
            DataSourceTypeDescription adapterTypeDescription = new DataSourceTypeDescription();
            DescriptionAttribute descriptionAttribute;
            string[] splitDescription;

            if (type.TryGetAttribute(out descriptionAttribute))
                splitDescription = descriptionAttribute.Description.ToNonNullNorEmptyString(type.FullName).Split(':');
            else
                splitDescription = new string[] { type.FullName };

            if (splitDescription.Length > 1)
            {
                adapterTypeDescription.Header = splitDescription[0].Trim();
                adapterTypeDescription.Description = splitDescription[1].Trim();
            }
            else
            {
                adapterTypeDescription.Description = splitDescription[0].Trim();
            }

            adapterTypeDescription.Type = type;
            return adapterTypeDescription;
        }
        #endregion
    }
}