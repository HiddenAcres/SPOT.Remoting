using System;
using Microsoft.SPOT;
using System.Collections;
using System.IO;
using Json.NETMF;

namespace HA.SPOT.Remoting.Interop
{
    public sealed class RemotingCommand
    {
        public string MethodName { get; set; }
        public object[] Parameters { get; set; }
        public Type CommandType { get; set; }
        public static RemotingCommand Deserialize(string JSON)
        { return (RemotingCommand)JsonSerializer.DeserializeString(JSON); }
    }

    public sealed class RemotingResponse
    {
        public RemotingResponse(object response)
            : this(response, response.GetType())
        { }
        public RemotingResponse(object response, Type responseType)
        {
            this.Response = response;
            this.ResponseType = responseType;
        }

        public object Response { get; protected set; }
        public Type ResponseType { get; protected set; }

        public string Serialize()
        { return JsonSerializer.SerializeObject(this); }
    }
}
