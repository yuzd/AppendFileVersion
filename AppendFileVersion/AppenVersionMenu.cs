using EnvDTE80;
using Ivony.Html;
using Ivony.Html.Parser;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace AppendFileVersion
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class AppenVersionMenu
    {


        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly DTE2 _dte;
        private readonly OleMenuCommandService _mcs;
        private readonly Func<string[], bool> _itemToHandleFunc;
        private Package _package;


        public AppenVersionMenu(DTE2 dte, OleMenuCommandService mcs, Func<string[], bool> itemToHandleFunc, Package package)
        {

            _dte = dte;
            _mcs = mcs;
            _itemToHandleFunc = itemToHandleFunc;
            _package = package;
        }


        public void SetupCommands()
        {
            CommandID command = new CommandID(CommandGuids.guidDiffCmdSet, (int)CommandId.AppenVersionMenuId);
            OleMenuCommand jsCommand = new OleMenuCommand(CommandInvoke, command);
            jsCommand.BeforeQueryStatus += html_BeforeQueryStatus;
            _mcs.AddCommand(jsCommand);
        }


        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CommandInvoke(object sender, EventArgs e)
        {
            var items = ProjectHelpers.GetSelectedItemPaths(_dte).ToList();
            if (items.Count != 1)
            {
                ProjectHelpers.AddError(_package, "no target was selected");
                return;
            }

            try
            {
                string file = items.ElementAt(0);
                if (File.Exists(file))
                {
                    DoAppendInCurrentFile(file);
                }
                else if (Directory.Exists(file))
                {
                    DoAppendInFolder(file);
                }

                MessageBox.Show("append success",
                    "Success", MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
            }
            catch (Exception ex)
            {
                ProjectHelpers.AddError(_package, ex.ToString());
                MessageBox.Show("Error happens: " + ex,
                    "Fail", MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
            }
        }


        private void DoAppendInCurrentFile(string filePath)
        {
            try
            {
                var body = File.ReadAllText(filePath, Encoding.UTF8);
                var htmlSource = new JumonyParser();
                var bodyHtml = htmlSource.Parse(body);
                var scriptlist = bodyHtml.Find("script").ToList();
                var csslist = bodyHtml.Find("link").ToList();
                var dataNowStr = DateTime.Now.ToString("yyyyMMddHHmm");
                void Replace(List<IHtmlElement> list, string tag, string key)
                {
                    foreach (var item in list)
                    {
                        var str = item.ToString();
                        if (!body.Contains(str))
                        {
                            //标签写的不规范
                            var attrs = item.Attributes();
                            str = attrs.Where(r => r.Name.ToLower().Equals("href") || r.Name.ToLower().Equals("src"))
                                .Select(r => r.AttributeValue).FirstOrDefault();
                            if(string.IsNullOrEmpty(str))continue;
                        }
                        else if (tag.Contains("\""))
                        {
                            if (!str.Contains(tag) && !str.Contains(tag.Replace("\"", "'")))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (!str.Contains(tag))
                            {
                                continue;
                            }
                        }


                        var lowerCase = str.ToLower();
                        var newStr = "";
                        if (lowerCase.Contains("." + key + "\""))
                        {
                            newStr = Regex.Replace(str, "." + key + "\"", "." + key + $"?{dataNowStr}\"", RegexOptions.IgnoreCase);
                        }
                        else if (lowerCase.Contains("." + key + "'"))
                        {
                            newStr = Regex.Replace(str, "." + key + "'", "." + key + $"?{dataNowStr}'", RegexOptions.IgnoreCase);
                        }
                        else if (lowerCase.Contains("." + key + "?"))
                        {
                            var findKey = "." + key + "?";
                            var totalLength = lowerCase.Length;
                            //找到下一个 " 或者 ‘ 之前的字符串
                            var indexOfta = str.IndexOf(findKey, StringComparison.OrdinalIgnoreCase);
                            if (indexOfta != -1)
                            {
                                indexOfta += findKey.Length;
                                for (int i = indexOfta; i < totalLength; i++)
                                {
                                    if (!str[i].Equals('\'') && !str[i].Equals('"'))
                                    {
                                        findKey += str[i];
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                                var newfindKey = findKey.Replace("?", "\\?");
                                newStr = Regex.Replace(str, newfindKey, findKey + "_" + $"{dataNowStr}", RegexOptions.IgnoreCase);
                            }
                            else
                            {
                                continue;
                            }

                        }
                        else if (lowerCase.EndsWith("." + key))
                        {
                            newStr = str.Replace( "." + key, "." + key + $"?{dataNowStr}");

                        }

                        if (!string.IsNullOrEmpty(newStr))
                        {
                           
                            body = ReplaceFirstOccurrence(body,str, newStr);
                        }
                    }
                }

                Replace(scriptlist, "src=", "js");
                Replace(csslist, "rel=\"stylesheet\"", "css");

                File.WriteAllText(filePath, body);
            }
            catch (Exception ex)
            {
                ProjectHelpers.AddError(_package, "file : " + filePath + "====>" + ex.ToString());
            }
        }
        public  string ReplaceFirstOccurrence(string Source, string Find, string Replace)
        {
            int Place = Source.IndexOf(Find);
            string result = Source.Remove(Place, Find.Length).Insert(Place, Replace);
            return result;
        }

        private void DoAppendInFolder(string folderPath)
        {
            string[] allfiles = Directory.GetFiles(folderPath, "*.cshtml", SearchOption.AllDirectories);
            foreach (var file in allfiles)
            {
                DoAppendInCurrentFile(file);
            }
        }

        void html_BeforeQueryStatus(object sender, System.EventArgs e)
        {
            OleMenuCommand oCommand = (OleMenuCommand)sender;

            oCommand.Visible = _itemToHandleFunc(new[] { ".html", ".cshtml", "view", "views" });
        }




    }
}
