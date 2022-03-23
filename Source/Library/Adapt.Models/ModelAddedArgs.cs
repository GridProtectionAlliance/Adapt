// ******************************************************************************************************
//  ModelAddedArgs.tsx - Gbtc
//
//  Copyright © 2022, Grid Protection Alliance.  All Rights Reserved.
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
//  03/23/2022 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************


using System;

namespace Adapt.Models
{
    /// <summary>
    /// Arguments for a Model Added Event
    /// </summary>
    public abstract class ModelAddedArgs<T> : EventArgs
    {
        /// <summary>
        /// Gets the ID of the <see cref="T"/>.
        /// </summary>
        public int ID { get; }

        /// <summary>
        /// Gets the <see cref="T"/> that was added
        /// </summary>
        public T Model {get;}

        /// <summary>
        /// Creates a new <see cref="ModelAddedArgs"/>
        /// </summary>
        /// <param name="ID">The ID of the Model.</param>
        public ModelAddedArgs(int Id)
        {
            this.ID = Id;
            Model = default(T);
        }

        /// <summary>
        /// Creates a new <see cref="ModelAddedArgs"/>
        /// </summary>
        /// <param name="Id">The ID of the Model.</param>
        /// <param name="model">The Model added.</param>
        public ModelAddedArgs(int Id, T model)
        {
            this.ID = Id;
            this.Model = model;
        }

    }
}
