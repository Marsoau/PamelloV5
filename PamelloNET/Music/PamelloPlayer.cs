using Discord.Audio;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PamelloNET.Music
{
    public enum PlayerLoopMode
    {
        UnLoop, Loop, SingleLoop
    }

    public class PamelloPlayer
    {
        public readonly IAudioClient AClient;

        public short QMax { get; set; }
        public List<PamelloSong> Queue { get; private set; }
        public short QN { get; private set; }
        private short PrevQN { get; set; }

        public PlayerLoopMode LoopMode { get; set; }
        public bool IsJumped { get; private set; }
        public bool ReturnTo { get; private set; }

        public bool IsActive { get; private set; }
        public bool IsPlaying { get; private set; }

        private Random RND { get; set; }

        private Stream? AStream { get; set; }
        private AudioOutStream? DAStream { get; set; }

        private CancellationTokenSource ctokenSource { get; set; }

        public PamelloPlayer(IAudioClient aclient) {
            AClient = aclient;
            Queue = new List<PamelloSong>();

            LoopMode = PlayerLoopMode.UnLoop;
            QN = 0;
            ReturnTo = false;

            QMax = short.Parse(Program.config["queue_max_songs"]);

            IsJumped = false;
            IsActive = false;
            IsPlaying = false;

            RND = new Random();
        }

        //nice
        public async Task MainLoop() {
            if (!IsActive) {
                IsActive = true;
                while (IsActive) {
                    Program.QueueEHandler.Trigger("all&refresh");
                    await PlayNext();

                    AfterSong();
                }
            }
        }

        //nice
        private async Task PlayNext() {
            if (Queue.Count > 0) {
                using (Process? ffmpeg = CreateStream(Queue[QN].Path))
                using (AStream = ffmpeg.StandardOutput.BaseStream)
                using (DAStream = AClient.CreatePCMStream(AudioApplication.Mixed)) {
                    try {
                        IsPlaying = true;

                        Console.WriteLine($"Playing song {Queue[QN]}");
                        using (StreamWriter writer = new StreamWriter(File.Open(Program.config["music_path"] + "history.txt", FileMode.Append))) {
                            writer.WriteLine(Queue[QN]);
                        }

                        ctokenSource = new CancellationTokenSource();
                        await AStream.CopyToAsync(DAStream, ctokenSource.Token);
                    }
                    catch (Exception ex) {
                        Console.WriteLine($"Exception Message: {ex.Message}");
                    }
                    finally {
                        await DAStream.FlushAsync();
                    }
                    IsPlaying = false;
                }
            }
            else IsActive = false;
        }

        //nice
        private void AfterSong() {
            if (Queue.Count == 0) {
                IsActive = false;
                return;
            }

            if (!IsActive) {
                LoopMode = PlayerLoopMode.UnLoop;
                Queue.Clear();
                QN = 0;

                return;
            }

            if (QN < 0 || QN >= Queue.Count) QN = 0;

            if (!IsJumped) {
                if (!ReturnTo) {
                    PrevQN = QN;

                    if (LoopMode == PlayerLoopMode.UnLoop) {
                        Queue.RemoveAt(QN);
                        QN = 0;
                    }
                    else if (LoopMode == PlayerLoopMode.Loop) {
                        QN++;
                        if (QN >= Queue.Count) { QN = 0; }
                    }
                }
                else {
                    if (PrevQN < 0) throw new Exception("There is no way back");

                    short buff = PrevQN;
                    PrevQN = QN;
                    QN = buff;

                    ReturnTo = false;
                } 
            }
            else IsJumped = false;

            if (Queue.Count > 0) IsActive = true;
            else IsActive = false;
        }

        //nice
        public void QueueAdd(PamelloSong song, int pos = -1) {
            if (Queue.Count >= QMax) throw new Exception($"Queue is full (max {QMax} songs)");

            if (pos < 0) Queue.Add(song);
            else if (pos <= Queue.Count) Queue.Insert(pos, song);
            else throw new Exception("Index out of queue");
        }

        //nice
        public void Shuffle() {
            int k, n = Queue.Count;
            PamelloSong value;
            while (n > 1) {
                n--;
                if (n == QN) continue;

                do { k = RND.Next(n + 1); } while (k == QN);

                value = Queue[k];
                Queue[k] = Queue[n];
                Queue[n] = value;
            }
        }

        //nice
        public void Clear() {
            Queue.Clear();
            if (IsPlaying) ctokenSource.Cancel();
        }

        //~nice
        public PamelloSong Jump(short topos) {
            if (topos == QN) throw new Exception("Already playing that song");

            if (topos >= 0 && topos < Queue.Count) {
                IsJumped = true;
                PrevQN = QN;
                QN = topos;
                Skip();

                return Queue[QN];
            }
            else throw new Exception("Index out of queue");
        }

        public PamelloSong Back() {
            if (PrevQN < 0) throw new Exception("There is no way back");

            ReturnTo = true;
            Skip();

            return Queue[QN];
        }

        //~nice
        public PamelloSong Leap(short topos) {
            if (topos == QN) throw new Exception("Already playing that song");

            if (topos >= 0 && topos < Queue.Count) {
                IsJumped = true;
                ReturnTo = true;
                PrevQN = QN;
                QN = topos;
                Skip();

                return Queue[QN];
            }
            else throw new Exception("Index out of queue");
        }

        //nice
        public void Swap(short from, short to) {
            if (from == to) return;

            if ((from >= 0 && from < Queue.Count) && (to >= 0 && to < Queue.Count)) {
                PamelloSong buff = Queue[to];
                Queue[to] = Queue[from];
                Queue[from] = buff;

                if (QN == from) QN = to;
                else if (QN == to) QN = from;
            }
            else throw new Exception("Index out of queue");
        }

        //nice
        public void Move(short from, short to) {
            if (from == to) return;

            if (to == QN) QN++;
            else if (from == QN) QN = to;
            else if (from > QN && to < QN) QN++;

            if (from < 0 && from >= Queue.Count) throw new Exception("intex \"from\" out of queue");
            else if (to < 0 && to >= Queue.Count) throw new Exception("intex \"to\" out of queue");
            else {
                PamelloSong buff = Queue[from];
                Queue.RemoveAt(from);
                Queue.Insert(to, buff);
            }
        }

        //rework
        public void Mult(short pos, short times) {
            if (pos < 0 || pos >= Queue.Count) throw new Exception("Intex out of queue");
            if (times <= 1) throw new Exception("Song must be multiplyed 2 or more times");
            if (Queue.Count + times > QMax) throw new Exception($"Queue is full (max {QMax} songs)");

            for (int i = 0; i < times - 1; i++) {
                Queue.Insert(pos, Queue[i]);
                //Queue.Insert(pos + 1, Queue[i]);
            }
        }

        //nice
        public PamelloSong Skip() {
            PamelloSong song;

            if (Queue.Count == 0) throw new Exception("Queue is empty");
            song = Queue[QN];

            if (IsPlaying) ctokenSource.Cancel();

            return song;
        }

        //nice
        public PamelloSong? Remove(short pos, short range) {
            PamelloSong song;

            if (pos >= 0 && pos < Queue.Count) {
                if (range == 1) {
                    song = Queue[pos];
                    Queue.RemoveAt(pos);

                    if (pos < QN) QN--;
                    else if (pos == QN) {
                        if (Queue.Count > 0) IsJumped = true;
                        PrevQN = -1;
                        ctokenSource.Cancel();
                    }

                    return song;
                }
                else if (range > 0 && pos + range <= Queue.Count) {
                    Queue.RemoveRange(pos, range);

                    if (pos + range - 1 < QN) QN -= range;
                    else if (QN >= pos && QN < pos + range) {
                        if (Queue.Count > 0) IsJumped = true;
                        QN = pos;
                        PrevQN = -1;
                        ctokenSource.Cancel();
                    }

                    return null;
                }
            }

            throw new Exception("Intex out of queue");
        }

        //nice
        public void SetMode(PlayerLoopMode plm) {
            if (plm == LoopMode)
                return;
            if (plm == PlayerLoopMode.UnLoop && LoopMode != PlayerLoopMode.UnLoop)
                Swap(QN, 0);

            LoopMode = plm;
        }

        //start song
        private Process? CreateStream(string path) {
            if (!File.Exists(path)) {
                throw new Exception($"file {path} doesn`t exist");
            }

            return Process.Start(new ProcessStartInfo {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }
    }
}
