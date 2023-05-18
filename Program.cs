using CoreOSC;
using CoreOSC.Types;
using System.Net.Sockets;

if (args.Length != 2) {
    args = new string[] {"set", "test.txt"};
}

var setMode = args[0] == "set";
var fileName = args[1];

BytesConverter BytesConverter = new BytesConverter();
OscMessageConverter MessageConverter = new OscMessageConverter();

using (var udpClient = new UdpClient("192.168.2.41", 2223)) {
    byte[] ConvertMessage(string line) {
        var data = line.Split(".");
        var value = string.Join('.', data.Skip(2));
        var address = new Address($"{data[0]}/{data[1]}");
        if (data[1] == "") {
            address = new Address($"{data[0]}");
        }
        var args = new object[] { };
        if (value != "") {
            args = new object[] { value };
        }
        var message = new OscMessage(address, args);
        var dWords = MessageConverter.Serialize(message);
        _ = BytesConverter.Deserialize(dWords, out var bytes);
        return bytes.ToArray();
    }
    OscMessage ParseMessage(byte[] data) {
            var dWords = BytesConverter.Serialize(data);
            MessageConverter.Deserialize(dWords, out var value);
            return value;
    }
    byte[] Query(byte[] message) {
        udpClient.Send(message, message.Length);
        var sender = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0);
        var result = udpClient.ReceiveAsync();
        result.Wait();
        return result.Result.Buffer;
    }

    if (setMode) {
        var messages = File.ReadAllLines(fileName);
        foreach (var message in messages) {
            Query(System.Text.Encoding.UTF8.GetBytes(message));
        }
    } else {
        var RootTypes = new [] {
            "ch",
            "aux",
            "bus",
            "main",
            "mtx",
            "dca",
            "fx",
        };
        var rootTypeMessages = RootTypes
            .Select(type => Task.Run(() => ConvertMessage($"/{type}..")))
            .ToList();
        var counts = new List<Task<OscMessage>>();
        foreach (var root in rootTypeMessages) {
            var data = Query(await root);
            counts.Add(Task.Run(() => ParseMessage(data)));
        }
        var queryMessages = new List<Task<byte[]>>();
        foreach (var count in counts) {
            var countMessage = await count;
            queryMessages.AddRange(countMessage.Arguments.Select(i => Task.Run(
                () => ConvertMessage($"{countMessage.Address.Value}.{i}.*"))));
        }
        Task.WaitAll(counts.ToArray());

        var responses = new List<byte>();
        foreach (var message in queryMessages) {
            var result = Query(await message);
            responses.AddRange(result);
            responses.Add((byte)'\n');
        }
        File.WriteAllBytes(fileName, responses.ToArray());
    }
}
