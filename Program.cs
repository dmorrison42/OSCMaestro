using CommandLine;

Parser.Default
    .ParseArguments<Options>(args)
    .WithParsed(o => {
        if (o.Verbose) {
            Console.WriteLine("OSCTree (name should be changed) v0.1");
        }
        var wing = o.SkipWing ? null : new Wing("192.168.2.41", 2223, o.VerboseWing);

        var server = new MidiServer(o.MidiDevice, o.Verbose);

        using (wing)
        using (server) {
            IReadOnlyList<string> preSaveMessages = new string[] {};

            server.StartSave += () => {
                preSaveMessages = wing?.GetSnapshot().MessagesUTF8 ?? new string[] {};
            };

            server.Save += (midi) => {
                if (o.Verbose) {
                    Console.WriteLine($"Saving snapshot to {midi}.snapshot");
                }
                var postSaveMessages = wing?.GetSnapshot().MessagesUTF8 ?? new string[] {};

                var differentMessages = postSaveMessages.Where(m => preSaveMessages?.Contains(m) == false).ToArray();

                if (differentMessages.Any()) {
                    new Snapshot(differentMessages)
                        .ToFile($"{midi}.snapshot");
                } else {
                    new Snapshot(postSaveMessages)
                        .ToFile($"{midi}.snapshot");
                }
            };

            server.Restore += (midi) => {
                if (o.Verbose) {
                    Console.WriteLine($"Restoring from snapshot {midi}.snapshot");
                }
                var snapshot = new Snapshot($"{midi}.snapshot");
                wing?.SetSnapshot(snapshot);
            };

            for (;;) {
                Thread.Sleep(1000);
            }
        }
    });
