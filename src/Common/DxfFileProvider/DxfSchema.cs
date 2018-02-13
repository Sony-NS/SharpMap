/*
 * Created  : Sony NS 
 * Descript : dxf file schema
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DxfFileProvider
{
    class DxfSchema
    {
        // DFX file strings
        public const string GIS_DXF_NSEC = "  0";
        public const int GIS_DXF_C0 = 0;
        public const int GIS_DXF_C1 = 1;
        public const int GIS_DXF_C10 = 10;
        public const string GIS_DXF_E10 = " 10";
        public const int GIS_DXF_C20 = 20;
        public const string GIS_DXF_E20 = " 20";
        public const int GIS_DXF_C30 = 30;
        public const string GIS_DXF_E30 = " 30";
        public const int GIS_DXF_C11 = 11;
        public const string GIS_DXF_E11 = " 11";
        public const int GIS_DXF_C12 = 12;
        public const string GIS_DXF_E12 = " 12";
        public const int GIS_DXF_C13 = 13;
        public const string GIS_DXF_E13 = " 13";
        public const int GIS_DXF_C21 = 21;
        public const string GIS_DXF_E21 = " 21";
        public const int GIS_DXF_C22 = 22;
        public const string GIS_DXF_E23 = " 23";
        public const int GIS_DXF_C23 = 23;
        public const string GIS_DXF_E31 = " 31";
        public const int GIS_DXF_C40 = 40;
        public const string GIS_DXF_E40 = " 40";
        public const int GIS_DXF_C50 = 50;
        public const string GIS_DXF_E50 = " 50";
        public const int GIS_DXF_C51 = 51;
        public const string GIS_DXF_E51 = " 51";
        public const int GIS_DXF_C1011 = 1011;
        public const string GIS_DXF_E1011 = "1011";
        public const int GIS_DXF_C1021 = 1021;
        public const string GIS_DXF_E1021 = "1021";
        public const int GIS_DXF_C1031 = 1021;
        public const string GIS_DXF_E1031 = "1031";
        public const int GIS_DXF_CADE = 1001;
        public const string GIS_DXF_EADE = "1001";
        public const string GIS_DXF_EADE_MAIN = "  2";
        public const string GIS_DXF_NADE = "ADE";
        public const int GIS_DXF_CADE_DATA = 1000;
        public const string GIS_DXF_EADE_DATA = "1000";
        public const int GIS_DXF_CADE_MARKER = 1002;
        public const string GIS_DXF_EADE_MARKER = "1002";
        public const string GIS_DXF_NBEGIN = "{";
        public const string GIS_DXF_NEND = "}";
        public const int GIS_DXF_CSECTION = 0;
        public const string GIS_DXF_ESECTION = "  0";
        public const string GIS_DXF_NSECTION = "SECTION";
        public const string GIS_DXF_ETABLES = "   2";
        public const string GIS_DXF_NTABLES = "TABLES";
        public const string GIS_DXF_ETABLE = "  0";
        public const string GIS_DXF_NTABLE = "TABLE";
        public const string GIS_DXF_NLIMIT = " 70";
        public const string GIS_DXF_EAPPID = "   0";
        public const string GIS_DXF_EAPPID_MAIN = "   2";
        public const string GIS_DXF_NAPPID = "APPID";
        public const string GIS_DXF_NAPPIDLIMIT = "     1";
        public const string GIS_DXF_NADELIMIT = "    64";
        public const string GIS_DXF_EENDTAB = "  0";
        public const string GIS_DXF_NENDTAB = "ENDTAB";
        public const int GIS_DXF_CENDSEC = 0;
        public const string GIS_DXF_EENDSEC = "  0";
        public const string GIS_DXF_NENDSEC = "ENDSEC";
        public const int GIS_DXF_CHEADER = 2;
        public const string GIS_DXF_EHEADER = "  2";
        public const string GIS_DXF_NHEADER = "HEADER";
        public const int GIS_DXF_CEXTMIN = 9;
        public const string GIS_DXF_EEXTMIN = "  9";
        public const string GIS_DXF_NEXTMIN = "$EXTMIN";
        public const int GIS_DXF_CXEXTMAX = 9;
        public const string GIS_DXF_EEXTMAX = "  9";
        public const string GIS_DXF_NEXTMAX = "$EXTMAX";
        public const int GIS_DXF_CENTITIES = 2;
        public const string GIS_DXF_EENTITIES = "  2";
        public const string GIS_DXF_NENTITIES = "ENTITIES";
        public const int GIS_DXF_CHANDLE = 5;
        public const string GIS_DXF_EHANDLE = "  5";
        public const int GIS_DXF_CLAYER = 8;
        public const string GIS_DXF_ELAYER = "  8";
        public const int GIS_DXF_CCOLOR = 62;
        public const string GIS_DXF_ECOLOR = " 62";
        public const int GIS_DXF_CPOINT = 0;
        public const string GIS_DXF_EPOINT = "  0";
        public const string GIS_DXF_NPOINT = "POINT";
        public const int GIS_DXF_CTEXT = 0;
        public const string GIS_DXF_NTEXT = "TEXT";
        public const int GIS_DXF_CINSERT = 0;
        public const string GIS_DXF_NINSERT = "INSERT";
        public const int GIS_DXF_CSOLID = 0;
        public const string GIS_DXF_NSOLID = "SOLID";
        public const int GIS_DXF_CLINE = 0;
        public const string GIS_DXF_NLINE = "LINE";
        public const int GIS_DXF_CARC = 0;
        public const string GIS_DXF_NARC = "ARC";
        public const int GIS_DXF_CCIRCLE = 0;
        public const string GIS_DXF_NCIRCLE = "CIRCLE";
        public const int GIS_DXF_CPOLYLINE = 0;
        public const string GIS_DXF_EPOLYLINE = "  0";
        public const string GIS_DXF_NPOLYLINE = "POLYLINE";
        public const int GIS_DXF_CLWPOLYLINE = 0;
        public const string GIS_DXF_NLWPOLYLINE = "LWPOLYLINE";
        public const int GIS_DXF_CVERTEXFOLLOW = 66;
        public const string GIS_DXF_EVERTEXFOLLOW = " 66";
        public const string GIS_DXF_NVERTEXFOLLOW = "     1";
        public const int GIS_DXF_CVERTEXATTR = 70;
        public const string GIS_DXF_EVERTEXATTR = " 70";
        public const string GIS_DXF_EVERTEXATTR0 = "     0";
        public const string GIS_DXF_EVERTEXATTR1 = "     1";
        public const int GIS_DXF_CVERTEX = 0;
        public const string GIS_DXF_EVERTEX = "  0";
        public const string GIS_DXF_NVERTEX = "VERTEX";
        public const int GIS_DXF_CSEQEND = 0;
        public const string GIS_DXF_ESEQEND = "  0";
        public const string GIS_DXF_NSEQEND = "SEQEND";
        public const int GIS_DXF_CEOF = 0;
        public const string GIS_DXF_EEOF = "  0";
        public const string GIS_DXF_NEOF = "EOF";
        // Built-in fields
        public const string GIS_DXF_FLD_ID = "DXF_ID";
        public const string GIS_DXF_FLD_HANDLE = "DXF_HANDLE";
        public const string GIS_DXF_FLD_LAYER_NAME = "DXF_LAYER";
        public const string GIS_DXF_FLD_ELEVATION = "DXF_ELEVATION";
        public const string GIS_DXF_FLD_SHAPE_TYPE = "DXF_TYPE";
        public const string GIS_DXF_FLD_LABEL = "DXF_LABEL";
        public const string GIS_DXF_FLD_COLOR = "DXF_COLOR";
    }
}
