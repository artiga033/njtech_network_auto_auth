using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Shared
{
    public enum Channel
    {
        Default,
        Cmcc,
        Telecom
    }
    public static class ChannelEnumOps
    {
        public static string ToChannelParam(this Channel channel)
            => channel switch
            {
                Channel.Default => "default",
                Channel.Cmcc => "@cmcc",
                Channel.Telecom => "@telecom",
                _ => throw new ArgumentOutOfRangeException(nameof(channel)),
            };
        public static string ToChannelShowParam(this Channel channel)
            => channel switch
            {
                Channel.Default => "校园内网",
                Channel.Cmcc => "中国移动",
                Channel.Telecom => "中国电信",
                _ => throw new ArgumentOutOfRangeException(nameof(channel)),
            };
    }
}
