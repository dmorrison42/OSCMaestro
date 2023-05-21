using System.Text;

internal class Snapshot {
    // WARNING: This currently requires each line of a snapshot not contain a newline
    private IReadOnlyList<byte[]> m_Messages = new byte[][] {};

    public IReadOnlyList<byte[]> Messages => m_Messages;
    public IReadOnlyList<string> MessagesUTF8 => m_Messages.Select(m => Encoding.UTF8.GetString(m)).ToList();

    public Snapshot(string path) {
        var messages = File.ReadAllLines(path);
        m_Messages = messages.Select(m => Encoding.UTF8.GetBytes(m)).ToList();
    }

    public Snapshot(IEnumerable<byte[]> messages) {
        m_Messages = messages.ToList();
    }

    public Snapshot(IEnumerable<string> messages) {
        m_Messages = messages.Select(m => Encoding.UTF8.GetBytes(m)).ToList();
    }

    public void ToFile(string fileName) {
        var lines = m_Messages.Select(m => Encoding.UTF8.GetString(m));
        File.WriteAllLines(fileName, lines);
    }
}