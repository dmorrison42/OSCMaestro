using System.Net.Sockets;
using CoreOSC;
using CoreOSC.Types;

internal class Wing : IDisposable {
    private UdpClient m_UdpClient;
    private BytesConverter m_BytesConverter = new BytesConverter();
    private OscMessageConverter m_MessageConverter = new OscMessageConverter();
    private byte[][] m_QueryMessages = new byte[][]{};
    public bool Verbose { get; set; }

    private byte[] ConvertToBytes(OscMessage message) {
        var dWords = m_MessageConverter.Serialize(message);
        _ = m_BytesConverter.Deserialize(dWords, out var bytes);
        return bytes.ToArray();
    }

    public Wing(string hostname, int port, bool verbose) {
        Verbose = verbose;

        m_UdpClient = new UdpClient(hostname, port);
        var rootTypes = new [] { "ch", "aux", "bus", "main", "mtx", "dca", "fx" };

        var rootMessageTasks = rootTypes.Select(type => Task.Run(() => {
            return ConvertToBytes(new OscMessage(new Address($"/{type}")));
        }));

        var countTasks = new List<Task<OscMessage>>();
        foreach (var root in rootMessageTasks) {
            root.Wait();
            var data = Query(root.Result);
            countTasks.Add(Task.Run(() => {
                var dWords = m_BytesConverter.Serialize(data);
                m_MessageConverter.Deserialize(dWords, out var value);
                return value;
            }));
        }
        var queryMessages = new List<Task<byte[]>>();
        foreach (var count in countTasks) {
            count.Wait();
            var countMessage = count.Result;
            queryMessages.AddRange(countMessage.Arguments.Select(i => Task.Run(() => {
                var addr = new Address($"{countMessage.Address.Value}/{i}");
                return ConvertToBytes(new OscMessage(addr, new object [] {"*"}));
            })));
        }

        Task.WaitAll(queryMessages.ToArray());
        m_QueryMessages = queryMessages.Select(q => q.Result).ToArray();

        if (verbose) {
            foreach (var qm in m_QueryMessages) {
                Console.WriteLine($"Query msg: {System.Text.Encoding.UTF8.GetString(qm)}");
            }
        }
    }

    private byte[] Query(byte[] message) {
        if (Verbose) Console.WriteLine($"wing-> {System.Text.Encoding.UTF8.GetString(message)}");
        m_UdpClient.Send(message, message.Length);
        var sender = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0);
        var result = m_UdpClient.ReceiveAsync();
        result.Wait();

        var resp = result.Result.Buffer;
        if (Verbose) Console.WriteLine($"wing<- {System.Text.Encoding.UTF8.GetString(resp)}");

        return resp;
    }

    public Snapshot GetSnapshot() {
        return new Snapshot(m_QueryMessages.Select(m => Query(m)));
    }

    public void SetSnapshot(Snapshot snapshot) {
        foreach (var line in snapshot.Messages) {
            Query(line);
        }
    }

    public void Dispose() {
        m_UdpClient.Dispose();
    }
}
