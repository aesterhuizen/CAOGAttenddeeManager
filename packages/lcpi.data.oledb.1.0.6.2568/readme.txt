README - LCPI ADO.NET Data Provider For OLE DB.

=================================================================
Description:

This package includes the ADO.NET data provider for work with Firebird DBMS
and InterBase DBMS through IBProvider [OLE DB Provider for FB/IB].

This ADO.NET data provider fully implements the System.Data.OleDb features and
adds many own, unique features.

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
 - Support of .NET FW 3.5/4.0/4.5/4.5.1/4.6
 - Unified support of 32-bit and 64-bit programs
 - Support of multithreaded applications
 - Reliable management of resources
 - Aggressive management of the .NET component lifetime
 - Localization of error messages
 - Advanced installer (MSI and EXE)
 - Installation to GAC
 - Registration in machine.config
 - DDEX
 - Total code testing

Home Page:
 http://www.ibprovider.com

ADO.NET Provider Installer:
 http://www.ibprovider.com/tools/download.php?filename=lcpi.oledb.net.exe&lang=1

IBProvider Installers [TRIAL]:
 http://www.ibprovider.com/tools/download.php?filename=ibprovider_trial_64bit.msi&lang=1
 http://www.ibprovider.com/tools/download.php?filename=ibprovider_trial_32bit.msi&lang=1

Manuals:
 http://www.ibprovider.com/eng/documentation/lcpi_oledb_net__using_adonet_with_firebird.html
 http://www.ibprovider.com/eng/documentation/lcpi_oledb_net__installer_msi.html

Samples:
 http://www.ibprovider.com/eng/documentation/examples.html#lcpi_oledb_net_c0001

News Archive:
 http://www.ibprovider.com/eng/news/n_150408_adonet_provider_release.html
 http://www.ibprovider.com/eng/news/n_141014.html
 http://www.ibprovider.com/eng/news/n_131120.html
 http://www.ibprovider.com/eng/news/n_131017.html
 http://www.ibprovider.com/eng/news/n_130614.html
 http://www.ibprovider.com/eng/news/n_120608.html

Contact E-Mail:
 ibprovider@ibprovider.com

=================================================================
ChangeLog:

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
