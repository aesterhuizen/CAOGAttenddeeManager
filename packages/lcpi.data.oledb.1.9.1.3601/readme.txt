README - LCPI ADO.NET Data Provider For OLE DB.

=================================================================
Description:

* This package contains ADO.NET data provider, which fully implements
  the System.Data.OleDb features and adds many own, unique features.

* Primary targets of this ADO.NET provider: Firebird/InterBase DBMS and
  LCPI.IBProvider.3 [Native OLE DB Provider For FB/IB].

Functionality:
 - Pool of connections
 - Nested transactions
 - Multiple transactions within one connection
 - TransactionScope support
 - Automatic formation of parameter descriptions
 - IN, OUT and IN-OUT parameters
 - Named and unnamed parameters in queries
 - Execution of scripts with parameters
 - MARS
 - Support of OLEDB schemas and ADO.NET metadata collections
 - Support of all types of the IB/FB data
 - Support of stream reading of blobs
 - Support of databases with the first and the third dialects
 
Technologies:
 - Integration with ADODB and OLE DB.
 - Support of .NET FW 3.5/4.0/4.5/4.5.1/4.6/4.6.2/4.7.2
 - Support of .NET Standard 2.0
 - Unified support of 32-bit and 64-bit programs
 - Support of multithreaded applications
 - Reliable management of resources
 - Aggressive management of the .NET component lifetime
 - Localization of error messages
 - Advanced installer (MSI and EXE)
 - Installation to GAC
 - Registration in machine.config
 - DDEX for VS2008-VS2017
 - Total code testing

Home Page:
 https://www.ibprovider.com

ADO.NET Provider Installer:
 https://www.ibprovider.com/tools/download.php?filename=lcpi.oledb.net.exe&lang=1

IBProvider Installers [TRIAL]:
 https://www.ibprovider.com/tools/download.php?filename=ibprovider_trial_64bit.msi&lang=1
 https://www.ibprovider.com/tools/download.php?filename=ibprovider_trial_32bit.msi&lang=1

Manuals:
 https://www.ibprovider.com/eng/documentation/lcpi_oledb_net__using_adonet_with_firebird.html
 https://www.ibprovider.com/eng/documentation/lcpi_oledb_net__installer_msi.html

Samples:
 https://www.ibprovider.com/eng/documentation/examples.html#examples__lcpi_ado_net

News Archive:
 https://www.ibprovider.com/eng/news/n_181030.html
 https://www.ibprovider.com/eng/news/n_180608.html
 https://www.ibprovider.com/eng/news/n_171114.html
 https://www.ibprovider.com/eng/news/n_171016.html
 https://www.ibprovider.com/eng/news/n_170622.html
 https://www.ibprovider.com/eng/news/n_170514__lcpi_data_oledb__v1_0_8.html
 https://www.ibprovider.com/eng/news/n_170511.html
 https://www.ibprovider.com/eng/news/n_150408_adonet_provider_release.html
 https://www.ibprovider.com/eng/news/n_141014.html
 https://www.ibprovider.com/eng/news/n_131120.html
 https://www.ibprovider.com/eng/news/n_131017.html
 https://www.ibprovider.com/eng/news/n_130614.html
 https://www.ibprovider.com/eng/news/n_120608.html

Contact E-Mail:
 ibprovider@ibprovider.com

=================================================================
ChangeLog:

v1.9.1.3601
* Improved a support of IBProvider properties:
  - first_week_day
  - auto_commit
  - nested_trans
  - inner_trans

All changes affected only OleDbConnectionStringBuilder and targeted to improving DDEX UI.

Other components of provider not changed.

v1.9.0.3592
* No changes in Data providers. But they were recompiled.

v1.8.0.3545
* Added support for FW4.7.2
* Added support for new IBProvider property "schema_ldr_cfg__descriptions"
* Internal improvements

v1.7.3.3440
* Fixed a little problem, which detected by PVS-Studio
  - mistake in internal code: ThrowBugCheck::unexpected_type_of_providerProgID

v1.7.2.3437
* Improved OleDbDataReader::GetDecimal
  - Allowed reading of columns with datatypes: SByte, Int16-Int64, Byte, UInt16-UInt64, Single, Double
* Usage of error code DB_E_UNSUPPORTEDCONVERSION instead DB_E_CANTCONVERTVALUE

v1.7.1.3418
* Correction of error messages
* Code normalization

v1.7.0.3395
* Synchronization with IBProvider behaviour
  - Usage DB_E_DATAOVERFLOW instead DISP_E_OVERFLOW
* Minimal IBProvider version: 3.50

v1.6.0.3379
* Internal changes
   - redesign of the instrumental library
* OleDbConnectionStringBuilder
   - support of new IBProvider property: "guid_rules"

Data provider not changed.

v1.5.0.3103
* [new] OleDbDataReaderSchemaColumnNames
  - names of table columns from OleDbDataReader.GetSchemaTable
* Internal changes

v1.4.0.3036
* Publication of instrumental library (lcpi.lib) in separated nuget package

  Please inform us about any problems. Thanks.

* OleDbConnection::GetSchema
  - added support of schema "PrimaryKeys"
  - added support of schema "ForeignKeys"

  In previous releases these schemas were available only through GetOleDbSchema method.

v1.3.0.2965
* New names of DDEX providers modules (DLLs) for VS2008-VS2015
  - Unification with names of DDEX provider for VS2017

Data provider not changed.

v1.2.0.2951
* Support .NET Standard 2.0
* Minimal IBProvider version: 3.47.0.27224 [please update]

v1.1.2.2915
* OleDbCommandBuilder
  - resolved a problem with quoting of object identifiers for FB/IB databases with 1 dialect
* OleDbConnectionStringBuilder
  - added a support for new IBProvider property "schema_restrictions"

v1.1.1.2906
* Added a support for new IBProvider property:
  - wchars_in_utf8_symbol

All changes affected only OleDbConnectionStringBuilder.

Other components of provider not changed.

v1.1.0.2901
* ADO.NET provider
 - No changes
* New DDEX provider (v2)
 - For VS2017 and FW 4.6.2 only

v1.0.10.2887
* ADO.NET provider
 - No changes
* DDEX provider
 - Resolved a problem with drag&drop operation
 - Improved "Connection Configuration" dialogue

v1.0.9.2872 [deleted]

v1.0.8.2633
* [added] OleDbConnectionStringBuilder::IsNetProviderProperty
  - required in updated DDEX provider for correct processing own properties of .Net provider

v1.0.7.2597
* [added] OleDbServices
  - works with COM objects with IDataInitialize interface
  - this an auxiliary component for OleDbConnection objects
  - might be used for replacing "MSDASC.MSDAINITIALIZE.1"
    on the "LCPI.OleDbServices.DataInitManager.Global.1"
* [added] OleDbConnection::Services
  - R/W property to associate an OleDbConnection object with an OleDbServices object
* Fixed errors
* Improved debug code

v1.0.6.2568
* Added a support for new IBProvider properties:
  - remote:auth
  - remote:wire_crypt [corrected]

* Improved a support of IBProvider properties:
  - remote:protocol

All changes affected only OleDbConnectionStringBuilder.

Other components of provider not changed.

v1.0.5.2564 [deleted]

v1.0.4.2558
* Added a support for new IBProvider properties:
  - check_cn_status
  - remote:protocol
* Improved a support of IBProvider properties:
  - array_rw_mode
  - remote:protocol_type

All changes affected only OleDbConnectionStringBuilder.

Other components of provider not changed.

v1.0.3.2526
* The support of ADO.NET infrastructure improved:
 - [added] protected OleDbConnection::DbProviderFactory

v1.0.2.2502
* Added a nominal support for new IBProvider properties:
  - dbdate_rules

v1.0.2.2500
* Added a nominal support for new IBProvider properties:
  - array_rw_mode
  - remote:protocol_arch
  - remote:protocol_type
* Removed a support of deprecated IBProvider properties:
  - force_param_describe

v1.0.2.2498
* Added a support for "dbclient_type" [IBProvider]
* FW4.6: recompiled by VS2015 Upd1

v1.0.1.2491
* No changes in primary assemblies
* Small updates in tests

v1.0.1.2490
* Added a support for .NET FW 4.6

v1.0.0.2476
* Release of product

v1.0.0.2475
* OleDbConnection.AttachToNativeSession
  - uses lcpi.lib.com.t_com_ptr_wrapper

v1.0.0.2470
* Instrumental Library (lcpi.lib)
  - [new] lcpi.lib.adodb.AdoDbConstructor
* ADO.NET provider
  - Little internal reorganization

v1.0.0.2465
* This is a pre final build.
* [added] Bridge into the world of ADODB/OLEDB/COM:
  - OleDbConnection::GetNativeSession
  - OleDbConnection::AttachToNativeSession

v1.0.0.2457
* [added] OleDbConnection.ModifyDatabase
  - support for OLEDB interface IDBDataSourceAdmin::ModifyDataSource

v1.0.0.2436
* Revision and improvement of error messages

v1.0.0.2412
* OleDbConnection.Open
* OleDbConnection.CreateDatabase
  - [added] support of notification through OleDbConnection.InfoMessage

v1.0.0.2408
* [new] OleDbDataReader.GetArray

v1.0.0.2353
* [new] OleDbConnectionStringBuilder.IBProvider
* [new] OleDbConnectionStringBuilder.UserID
* [new] OleDbConnectionStringBuilder.Password
* [new] OleDbConnectionStringBuilder.IntegratedSecurity
* other changes in OleDbConnectionStringBuilder

v1.0.0.2308
* [new] OleDbConnection.CreateDatabase
* [new] OleDbConnection.DropDatabase
* IBProvider minimal version: 3.23.0.17468
