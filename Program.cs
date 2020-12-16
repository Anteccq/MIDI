using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;

namespace midi
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("待機：なんか入力");
            Console.ReadKey();

            if (MidiIn.NumberOfDevices == 0) return;

            for(var i=0;i<MidiIn.NumberOfDevices;i++){
                var d = MidiIn.DeviceInfo(i).ProductName;
                Console.WriteLine($"i : {d}");
            }
            
            var deviceName = MidiIn.DeviceInfo(1).ProductName;
            Console.WriteLine($"接続 : {deviceName}");
            
            var midiIn = new MidiIn(0);
            var stopWatch = new Stopwatch();


            var midiStream = Observable.FromEventPattern<MidiInMessageEventArgs>(
                h => midiIn.MessageReceived += h,
                h => midiIn.MessageReceived -= h);


            var onStream = midiStream.Where(x => x.EventArgs.MidiEvent.CommandCode == MidiCommandCode.NoteOn)
                .Select(x => (x, stopWatch.ElapsedMilliseconds));
            var offStream = midiStream.Where(x => x.EventArgs.MidiEvent.CommandCode == MidiCommandCode.NoteOff);
            var noteList = new List<NoteData>();
            var list = new List<List<NoteData>>();

            var list1 = noteList;
            onStream.Zip(offStream)
                .Subscribe(x =>
                {
                    var endTime = stopWatch.ElapsedMilliseconds;
                    var note1 = x.First.x.EventArgs.MidiEvent;
                    var length = endTime - x.First.ElapsedMilliseconds;
                    if (!(note1 is NoteOnEvent noe)) return;
                    Console.WriteLine($"*********************************");
                    Console.WriteLine($"コマンド : {noe.CommandCode}");
                    Console.WriteLine($"ノーツ名 : {noe.NoteName}");
                    Console.WriteLine($"長さ : {length}");
                    Console.WriteLine($"Time : {x.First.ElapsedMilliseconds}");
                    Console.WriteLine($"強さ ： {noe.Velocity}");
                    var note = new NoteData()
                    {
                        AbsoluteTime = x.First.ElapsedMilliseconds,
                        NoteNumber = noe.NoteNumber,
                        Length = length,
                        NoteName = noe.NoteName,
                        Velocity = noe.Velocity
                    };

                    list1.Add(note);
                });
            midiIn.Start();

            stopWatch.Start();
            for (var i = 0; i < 10; i++)
            {
                Console.WriteLine($"キーを押すとデータ収集を開始します。 {i + 1}/10");
                Console.WriteLine($"再びキーを押すと次のデータ収集に移行します。");
                Console.ReadKey();
                noteList = new List<NoteData>();
                stopWatch.Restart();

                Console.ReadKey();
                list.Add(noteList);
            }

            list.ForEach(notes =>
            {
                if (notes.Count == 0) return;
                var baseTime = notes[0].AbsoluteTime;
                Console.WriteLine("====================================================");
                notes.ForEach(x =>
                {
                    x.RelativeTime = x.AbsoluteTime - baseTime;
                    Console.WriteLine($"コード：{x.NoteName} 強さ：{x.Velocity}");
                    Console.WriteLine($"長さ：{x.Length} 相対的時間：{x.RelativeTime}");
                });
            });

            Console.ReadKey();
            stopWatch.Stop();
            midiIn.Stop();
            midiIn.Dispose();

            using var stream = new StreamWriter("./midi.csv", false, Encoding.UTF8);

            foreach (var note in noteList)
            {
                stream.Write($"{note.NoteName} Velocity, Length, RelativeTime, ");
            }
            stream.WriteLine();

            foreach (var nodeList in list)
            {
                foreach (var note in nodeList)
                {
                    stream.Write($"{note.Velocity}, {note.Length}, {note.RelativeTime}, ");
                }
                stream.WriteLine();
            }
        }
    }
}
