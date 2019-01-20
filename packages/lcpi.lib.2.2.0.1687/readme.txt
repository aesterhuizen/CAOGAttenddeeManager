README - LCPI Instrumental Library.

=================================================================
Description:

This package contains common structures for LCPI software.

Home Page:
 http://www.ibprovider.com

Contact E-Mail:
 ibprovider@ibprovider.com

=================================================================
ChangeLog:

v2.2.0.1687
* Added support for FW4.7.2
* Added support for new IBProvider property "schema_ldr_cfg__descriptions"
* Improvements in structure.TraceMsgBuilderUtils

v2.1.2.1669
* Fixed small problems, which detected by PVS-Studio:
  - Mistakes in code with verifications:
     - tagDBTIMESTAMPOFFSET::Create
     - tagDBTIMESTAMP::Create
     - tagDBDATE::Create

v2.1.1.1665
* Updating of version after code revision
  - No changes in code, only formatting of sources

v2.1.0.1660
* tagDBDATE::ToDateTime_NoThrow
* tagDBTIMESTAMP::ToDateTime_NoThrow
* tagDBTIMESTAMPOFFSET::ToDateTimeOffset_NoThrow
  - Usage DB_E_DATAOVERFLOW instead DISP_E_OVERFLOW

v2.0.0.1654
* Redesign of public interface

v1.1.0.1348
* Reorganization
  - structure.t_str_formatter renamed to structure.StringFormatter
    + more exact implementation
  - structure.t_str_version renamed to structure.Version
  - structure.intptr_utils renamed to structure.IntPtrUtils
  - structure.uintptr_utils renamed to structure.UIntPtrUtils

v1.0.0.1282
* First public release
