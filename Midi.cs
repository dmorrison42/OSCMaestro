using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;

internal class MidiServer : IDisposable {
    public enum Mode {
        Listen,
        Save,
    }

    private Mode m_Mode = Mode.Listen;
    private VirtualDevice m_Device;

    public bool Verbose { get; set; }
    public string DeviceName => "WINGSnapshots";

    public delegate void MidiServerEventHandler(string midi);
    public delegate void MidiServerStartEventHandler();

    public event MidiServerEventHandler? Save;
    public event MidiServerStartEventHandler? StartSave;
    public event MidiServerEventHandler? Restore;

    public MidiServer(bool verbose) {
        Verbose = verbose;

        // TODO: Make this variable
        m_Device = VirtualDevice.Create(DeviceName);

        m_Device.InputDevice.EventReceived += (_, e) => {
            if (e.Event is NoteOnEvent noe) {
                var midiString = $"ch{noe.Channel} #{noe.NoteNumber} v{noe.Velocity}";
                if (Verbose) {
                    Console.WriteLine(midiString);
                }

                // Channel 16 (0 indexed) is used for admin/meta things
                if ($"{noe.Channel}" == "15") {
                    // TODO: map these midi notes better
                    if ($"{noe.NoteNumber}" == "0") {
                        m_Mode = Mode.Save;
                        StartSave?.Invoke();

                        if (Verbose) {
                            Console.WriteLine("Entering save mode");
                        }
                        // Stop processing and wait for next signal
                        return;
                    }
                }

                switch (m_Mode) {
                    case Mode.Save:
                        Save?.Invoke(midiString);
                        break;
                    case Mode.Listen:
                        Restore?.Invoke(midiString);
                        break;
                }
                m_Mode = Mode.Listen;
                if (Verbose) {
                    Console.WriteLine("Entering listening mode");
                }
            }
        };

        m_Device.InputDevice.StartEventsListening();
        if (Verbose) {
            Console.WriteLine($"Started virtual MIDI device: {DeviceName}");
        }
    }

    public void Dispose() {
        m_Device.Dispose();
    }
}