using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MyDefense.Clans
{
    /// <summary>Preserves server nulls; Unity JsonUtility synthesizes nested empty objects instead.</summary>
    public static class ClanJson
    {
        public static T Read<T>(string json) where T:class
        {
            if(string.IsNullOrWhiteSpace(json)||json.Length>262144)throw new FormatException("Invalid clan response size");
            using(var input=new StringReader(json))
            using(var reader=new JsonTextReader(input){MaxDepth=32,DateParseHandling=DateParseHandling.None})
            {
                var value=JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
                if(reader.Read())throw new FormatException("Trailing clan response data");
                if(typeof(T)==typeof(ClanStatus))
                {
                    var clan=value["clan"];
                    if(clan==null||(clan.Type!=JTokenType.Null&&clan.Type!=JTokenType.Object))throw new FormatException("Invalid clan membership shape");
                    var pending=value["pendingClanId"];
                    if(pending==null||pending.Type==JTokenType.Null)value["pendingClanId"]=0;
                    else if(pending.Type!=JTokenType.Integer)throw new FormatException("Invalid pending clan ID");
                }
                return value.ToObject<T>(JsonSerializer.Create(new JsonSerializerSettings{TypeNameHandling=TypeNameHandling.None,MetadataPropertyHandling=MetadataPropertyHandling.Ignore,MaxDepth=32}));
            }
        }
#if UNITY_EDITOR
        public static string SerializeFixture(object value)=>JsonConvert.SerializeObject(value,new JsonSerializerSettings{TypeNameHandling=TypeNameHandling.None,NullValueHandling=NullValueHandling.Include});
#endif
    }
}
