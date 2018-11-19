using System;

namespace AppendFileVersion
{
    /// <summary>
    /// 
    /// </summary>
    public class CommandGuids
    {
        public const string guidDiffCmdSetString = "8e18f0f2-4053-4b37-a6e9-9c2f5f94d73a";


        public static readonly Guid guidDiffCmdSet = new Guid(guidDiffCmdSetString);


    }


    enum CommandId
    {
        AppenVersionMenuId = 0x1047
    }
}
