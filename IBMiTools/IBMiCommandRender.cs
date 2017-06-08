﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

namespace IBMiCmd.IBMiTools
{
    class IBMiCommandRender
    {

        internal static string[] RenderFFDCollectionScript(List<SourceLine> src, string[] tmp)
        {
            IBMiUtilities.DebugLog("RenderFFDCollectionScript");
            string[] cmd = new string[(src.Count * 3) + 2];
            int i = 0, t = 0;
            // Run commands on remote
            cmd[i++] = "ASCII";
            cmd[i++] = $"QUOTE RCMD CHGLIBL LIBL({ IBMi.GetConfig("datalibl").Replace(',', ' ')})  CURLIB({ IBMi.GetConfig("curlib") })";
            foreach (SourceLine sl in src)
            {
                cmd[i++] = $"QUOTE RCMD {IBMi.GetConfig("installlib")}/NPPDSPFFD {sl.searchResult}";
                cmd[i++] = $"RECV /home/{ IBMi.GetConfig("username") }/{ sl.searchResult }.tmp \"{ tmp[t++] }\"";
                cmd[i++] = $"QUOTE RCMD RMVLNK OBJLNK('/home/{ IBMi.GetConfig("username") }/{ sl.searchResult }.tmp')";
            }
            
            IBMiUtilities.DebugLog("RenderFFDCollectionScript - DONE!");
            return cmd;
        }

        internal static string[] RenderRemoteInstallScript(List<string> sourceFiles, string library)
        {
            // Make room for <upload, copy, delete, compile> for each file
            string[] cmd = new string[sourceFiles.Count * 4 + 3];
            int i = 0;
            cmd[i++] = "ASCII";
            cmd[i++] = "QUOTE RCMD CRTPF FILE(QTEMP/NPPCLSRC)  RCDLEN(112) FILETYPE(*SRC) MAXMBRS(*NOMAX) TEXT('Deploy NPP plugin commands')";
            cmd[i++] = "QUOTE RCMD CRTPF FILE(QTEMP/NPPCMDSRC) RCDLEN(112) FILETYPE(*SRC) MAXMBRS(*NOMAX) TEXT('Deploy NPP plugin commands')";
            foreach (string file in sourceFiles)
            {
                string fileName = file.Substring(file.LastIndexOf("\\") + 1);
                string member = fileName.Substring(fileName.LastIndexOf("-") + 1, fileName.LastIndexOf(".") - (fileName.LastIndexOf("-") + 1));
                string sourceFile = null, crtCmd = null;

                switch (fileName.Substring(fileName.Length - 4))
                {
                    case ".clp":
                        sourceFile = "NPPCLSRC";
                        crtCmd = $"CRTCLPGM PGM({library}/{member}) SRCFILE(QTEMP/NPPCLSRC) SRCMBR({member}) REPLACE(*YES) TEXT('{Main.PluginDescription}')";
                        break;
                    case ".cmd":
                        sourceFile = "NPPCMDSRC";
                        crtCmd = $"CRTCMD CMD({library}/{member}) PGM({library}/{member}) SRCFILE(QTEMP/NPPCMDSRC) SRCMBR({member}) REPLACE(*YES) TEXT('{Main.PluginDescription}')";
                        break;
                    default:
                        continue;
                }

                cmd[i++] = $"SEND \"{ file }\" /home/{ IBMi.GetConfig("username") }/{ fileName }";
                cmd[i++] = $"QUOTE RCMD CPYFRMSTMF FROMSTMF('/home/{ IBMi.GetConfig("username") }/{ fileName }') TOMBR('/QSYS.LIB/QTEMP.LIB/{ sourceFile }.FILE/{ member }.MBR')";
                cmd[i++] = $"QUOTE RCMD RMVLNK OBJLNK('/home/{ IBMi.GetConfig("username") }/{ fileName }')";
                cmd[i++] = $"QUOTE RCMD { crtCmd }";
            }
            return cmd;
        }
    }
}
