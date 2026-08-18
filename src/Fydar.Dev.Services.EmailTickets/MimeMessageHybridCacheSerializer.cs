using Microsoft.Extensions.Caching.Hybrid;
using MimeKit;
using System;
using System.Buffers;
using System.IO;

namespace Fydar.Dev.Services.EmailTickets;

/// <summary>
/// Cache mime messages as their raw bytes.
/// </summary>
public class MimeMessageHybridCacheSerializer : IHybridCacheSerializer<MimeMessage>
{
    public MimeMessage Deserialize(ReadOnlySequence<byte> source)
    {
        using var stream = new MemoryStream(source.ToArray(), writable: false);
        return MimeMessage.Load(stream);
    }

    public void Serialize(MimeMessage value, IBufferWriter<byte> target)
    {
        using var stream = new MemoryStream();
        value.WriteTo(stream);
        target.Write(new ReadOnlySpan<byte>(stream.GetBuffer(), 0, (int)stream.Length));
    }
}
