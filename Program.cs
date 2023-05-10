using CoreOSC;
using CoreOSC.IO;
using System.Net.Sockets;


using (var udpClient = new UdpClient("192.168.2.41", 2223)) {
    async Task<object[]> Query(string address) {
        var message = new OscMessage(new Address(address));

        await udpClient.SendMessageAsync(message);
        var response = await udpClient.ReceiveMessageAsync();
        return response.Arguments.ToArray();
    }

    var channels = await Query("/ch");
    foreach (var channel in channels) {
        var level = (await Query($"/ch/{channel}/fdr")).First();
        Console.WriteLine($"{channel}: {level}");
    }
}
