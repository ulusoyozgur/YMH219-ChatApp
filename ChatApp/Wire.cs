using System.Buffers.Binary;
using System.Text.Json;
namespace ChatApp;

public sealed record ChatMessage(string Kind, string Name, string Text, DateTimeOffset Time)
{
    public override string ToString() => $"[{Time.LocalDateTime:HH:mm:ss}] {Name}: {Text}";
}

internal static class Wire
{
    public static async Task WriteAsync(Stream stream, ChatMessage message, CancellationToken token)
    {
        byte[] data = JsonSerializer.SerializeToUtf8Bytes(message);
        if (data.Length > 32768) throw new InvalidDataException("Mesaj çok büyük.");
        byte[] header = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(header, data.Length);
        await stream.WriteAsync(header, token).ConfigureAwait(false);
        await stream.WriteAsync(data, token).ConfigureAwait(false);
    }
    public static async Task<ChatMessage?> ReadAsync(Stream stream, CancellationToken token)
    {
        byte[] header = new byte[4];
        if (await stream.ReadAsync(header.AsMemory(0, 1), token).ConfigureAwait(false) == 0) return null;
        await stream.ReadExactlyAsync(header.AsMemory(1), token).ConfigureAwait(false);
        int length = BinaryPrimitives.ReadInt32BigEndian(header);
        if (length is <= 0 or > 32768) throw new InvalidDataException("Geçersiz paket uzunluğu.");
        byte[] data = new byte[length];
        await stream.ReadExactlyAsync(data, token).ConfigureAwait(false);
        var result = JsonSerializer.Deserialize<ChatMessage>(data);
        if (result == null || result.Kind == null || result.Name == null || result.Text == null)
            throw new InvalidDataException("Eksik mesaj alanları.");
        return result;
    }
}
