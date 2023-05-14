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
    OscMessage ParseMessage(IEnumerable<DWord> dWords) {
            MessageConverter.Deserialize(dWords, out var value);
            return value;
    }
    async Task<IEnumerable<DWord>> Query(byte[] message) {
        await udpClient.SendAsync(message, message.Length);
        var result = await udpClient.ReceiveAsync();
        return BytesConverter.Serialize(result.Buffer);
    }

    if (setMode) {
        var messages = File.ReadAllLines(fileName)
            .Select(line => Task.Run(() => ConvertMessage(line)))
            .ToList();
        foreach (var message in messages) {
            await Query(await message);
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
            var data = await Query(await root);
            counts.Add(Task.Run(() => ParseMessage(data)));
        }
        var queryMessages = new List<Task<byte[]>>();
        foreach (var count in counts) {
            var countMessage = await count;
            queryMessages.AddRange(countMessage.Arguments.Select(i => Task.Run(
                () => ConvertMessage($"{countMessage.Address.Value}.{i}.*"))));
        }
        Task.WaitAll(counts.ToArray());

        var responses = new List<Task<string>>();
        foreach (var message in queryMessages) {
            var result = await Query(await message);
            responses.Add(Task.Run(() => {
                var message = ParseMessage(result);
                var address = message.Address.Value.Split("/");
                return $"/{address[1]}.{address[2]}.{message.Arguments.First()}";
            }));
        }
        File.WriteAllLines(fileName, await Task.WhenAll(responses.ToArray()));
    }
}
