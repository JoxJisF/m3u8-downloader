using System;

namespace m3u8_Linker
{
    public interface ILinker
    {
        string Name { get; set; }
        string Url { get; set; }

        string Quality { get; set; }
    }
}
