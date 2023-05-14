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
        var message = new OscMessage(new Address($"{data[0]}/{data[1]}"), new object[] { value });
        var dWords = MessageConverter.Serialize(message);
        _ = BytesConverter.Deserialize(dWords, out var bytes);
        return bytes.ToArray();
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
    }
}
