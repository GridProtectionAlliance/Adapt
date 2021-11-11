
--  ----------------------------------------------------------------------------------------------------
--  ADAPT Data Structures for SQLite - Gbtc
--
--  Copyright © 2021, Grid Protection Alliance.  All Rights Reserved.
--
--  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
--  the NOTICE file distributed with this work for additional information regarding copyright ownership.
--  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may
--  not use this file except in compliance with the License. You may obtain a copy of the License at:
--
--      http://www.opensource.org/licenses/MIT
--
--  Unless agreed to in writing, the subject software distributed under the License is distributed on an
--  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
--  License for the specific language governing permissions and limitations.
--
--  Schema Modification History:
--  ----------------------------------------------------------------------------------------------------
--  03/25/2021 - C. Lackner
--       Generated original version of schema.
--  ----------------------------------------------------------------------------------------------------


-- *******************************************************************************************
-- IMPORTANT NOTE: When making updates to this schema, please increment the version number!
-- *******************************************************************************************
CREATE VIEW SchemaVersion AS SELECT 3 AS VersionNumber;

CREATE Table DataSource (
	ID INTEGER PRIMARY KEY NOT NULL,
	Name VARCHAR(200) NOT NULL,
	TypeName VARCHAR(200) NOT NULL,
	ConnectionString TEXT NOT NULL,
	AssemblyName VARCHAR(200) NOT NULL
);

CREATE Table DeviceMetaData (
	ID INTEGER PRIMARY KEY NOT NULL,
	DataSourceID INTEGER NOT NULL,
	DeviceID VARCHAR(200) NOT NULL,
	Field VARCHAR(200) NOT NULL,
	Value TEXT NOT NULL,
	FOREIGN KEY(DataSourceID) REFERENCES DataSource(ID)
);

CREATE Table SignalMetaData (
	ID INTEGER PRIMARY KEY NOT NULL,
	DataSourceID INTEGER NOT NULL,
	SignalID VARCHAR(200) NOT NULL,
	DeviceID VARCHAR(200) NOT NULL,
	Field VARCHAR(200) NOT NULL,
	Value TEXT NOT NULL,
	FOREIGN KEY(DataSourceID) REFERENCES DataSource(ID)
);

CREATE UNIQUE INDEX IDX_SignalMetaData 
ON SignalMetaData(DataSourceID,SignalID,Field);

CREATE UNIQUE INDEX IDX_DeviceMetaData 
ON DeviceMetaData(DataSourceID,DeviceID,Field);

CREATE Table Template (
	ID INTEGER PRIMARY KEY NOT NULL,
	Name VARCHAR(200) NOT NULL
);

CREATE Table TemplateInputDevice (
	ID INTEGER PRIMARY KEY NOT NULL,
	TemplateID  INTEGER NOT NULL,
	Name VARCHAR(200) NOT NULL,
	IsInput BIT NOT NULL,
	OutputName VARCHAR(200) NOT NULL,
	FOREIGN KEY(TemplateID) REFERENCES Template(ID)
);


CREATE Table TemplateInputSignal (
	ID INTEGER PRIMARY KEY NOT NULL,
	DeviceID  INTEGER NOT NULL,
	Name VARCHAR(200) NOT NULL,
	Phase INTEGER NULL,
	MeasurmentType INTEGER NULL,
	FOREIGN KEY(DeviceID) REFERENCES TemplateInputDevice(ID)
);

CREATE Table TemplateSection (
	ID INTEGER PRIMARY KEY NOT NULL,
	TemplateID  INTEGER NOT NULL,
	Name VARCHAR(200) NOT NULL,
	AnalyticTypeID INTEGER NOT NULL,
	[Order] INTEGER NOT NULL,
	FOREIGN KEY(TemplateID) REFERENCES Template(ID)
);

CREATE Table Analytic (
	ID INTEGER PRIMARY KEY NOT NULL,
	TemplateID  INTEGER NOT NULL,
	SectionID  INTEGER NOT NULL,
	Name VARCHAR(200) NOT NULL,
	TypeName VARCHAR(200) NOT NULL,
	ConnectionString TEXT NOT NULL,
	AssemblyName VARCHAR(200) NOT NULL,
	FOREIGN KEY(TemplateID) REFERENCES Template(ID),
	FOREIGN KEY(SectionID) REFERENCES TemplateSection(ID)
);

CREATE TABLE AnalyticInput (
	ID INTEGER PRIMARY KEY NOT NULL,
	AnalyticID  INTEGER NOT NULL,
	[InputIndex] INTEGER NOT NULL,
	IsInputSignal BIT NOT NULL,
	SignalID Integer NOT NULL
)

CREATE TABLE AnalyticOutputSignal (
	ID INTEGER PRIMARY KEY NOT NULL,
	Name VARCHAR(200) NOT NULL,
	AnalyticID  INTEGER NOT NULL,
	DeviceID  INTEGER NOT NULL,
	OutputIndex  INTEGER NOT NULL,
	FOREIGN KEY(AnalyticID) REFERENCES Analytic(ID),
	FOREIGN KEY(DeviceID) REFERENCES TemplateInputDevice(ID)
)

CREATE TABLE TemplateOutputSignal (
	ID INTEGER PRIMARY KEY NOT NULL,
	Name VARCHAR(200) NOT NULL,
	TemplateID  INTEGER NOT NULL,
	Phase  INTEGER NOT NULL,
	Type  INTEGER NOT NULL,
	IsInputSignal BIT NOT NULL,
	SignalID INTEGER NOT NULL,
	FOREIGN KEY(TemplateID) REFERENCES Template(ID)
)
