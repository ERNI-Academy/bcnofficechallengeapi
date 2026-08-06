using System.Security.Cryptography;

namespace bcnofficechallengeapi.Services;

public sealed class QrTokenService
{
    private const byte TokenVersion = 1;
    private readonly byte[] key;

    public QrTokenService(IConfiguration configuration)
    {
        var configuredKey = configuration["QrTokens:EncryptionKey"]
            ?? throw new InvalidOperationException("Missing QrTokens:EncryptionKey configuration.");

        try
        {
            key = Convert.FromBase64String(configuredKey);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("QrTokens:EncryptionKey must be Base64 encoded.", exception);
        }

        if (key.Length != 32)
            throw new InvalidOperationException("QrTokens:EncryptionKey must decode to exactly 32 bytes.");
    }

    public string Protect(Guid qrId)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintext = qrId.ToByteArray();
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        var associatedData = new[] { TokenVersion };

        using var aes = new AesGcm(key, tag.Length);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);

        var token = new byte[1 + nonce.Length + ciphertext.Length + tag.Length];
        token[0] = TokenVersion;
        Buffer.BlockCopy(nonce, 0, token, 1, nonce.Length);
        Buffer.BlockCopy(ciphertext, 0, token, 1 + nonce.Length, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, token, 1 + nonce.Length + ciphertext.Length, tag.Length);

        return Base64UrlEncode(token);
    }

    public bool TryUnprotect(string token, out Guid qrId)
    {
        qrId = Guid.Empty;

        try
        {
            var data = Base64UrlDecode(token);
            if (data.Length != 45 || data[0] != TokenVersion)
                return false;

            var nonce = data.AsSpan(1, 12);
            var ciphertext = data.AsSpan(13, 16);
            var tag = data.AsSpan(29, 16);
            Span<byte> plaintext = stackalloc byte[16];
            var associatedData = new[] { TokenVersion };

            using var aes = new AesGcm(key, tag.Length);
            aes.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
            qrId = new Guid(plaintext);
            return true;
        }
        catch (Exception exception) when (
            exception is FormatException or CryptographicException or ArgumentException)
        {
            return false;
        }
    }

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += (padded.Length % 4) switch
        {
            2 => "==",
            3 => "=",
            _ => string.Empty
        };
        return Convert.FromBase64String(padded);
    }
}
