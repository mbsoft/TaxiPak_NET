using System;

/// <summary>
/// Summary description for HDIResponseFilter
/// </summary>
namespace SUTI_svc
{
    public class HDIResponseFilter : System.IO.Stream
    {
        private System.IO.Stream baseStream;

        public HDIResponseFilter(System.IO.Stream responseStream)
        {
            if (responseStream == null)
            {
                throw new ArgumentNullException("ResponseStream");
            }

            //Assign stream to class variable for class-wide access.
            baseStream = responseStream;
        }

        public override bool CanRead
        {
            get { return baseStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return baseStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return baseStream.CanWrite; }
        }

        public override void Flush()
        {
            baseStream.Flush();
        }

        public override long Length
        {
            get { return baseStream.Length; }
        }

        public override long Position
        {
            get
            {
                return baseStream.Position;
            }
            set
            {
                baseStream.Position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return baseStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            return baseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            baseStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            //Get text from response stream.
            string originalText = System.Text.Encoding.UTF8.GetString(buffer, offset, count);

            //Alter the text.
            originalText = originalText.Replace("<html>", "");
            originalText = originalText.Replace("</html>", "");
            //Write the altered text to the response stream.
            buffer = System.Text.Encoding.UTF8.GetBytes(originalText);
            this.baseStream.Write(buffer, 0, buffer.Length);

        }
    }
}
