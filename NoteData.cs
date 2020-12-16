namespace midi
{
    public class NoteData
    {
        public long AbsoluteTime { get; set; }

        public long Length { get; set; }

        public long Velocity { get; set; }

        public int NoteNumber { get; set; }

        public string NoteName { get; set; }

        public long RelativeTime { get; set; }
    }
}