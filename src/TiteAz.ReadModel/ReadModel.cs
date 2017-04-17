using System;
using NEvilES.Pipeline;

namespace TiteAz.ReadModel
{
    public class ReadModel
    {
        public Int64 Id { get; set; }
        public Guid StreamId { get; set; }
        public string Type { get; set; }
        public string Body { get; set; }
        public DateTime LastUpdate { get; set; }
    }
}
