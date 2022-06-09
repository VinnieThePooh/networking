namespace Sockets.Common.Framing;
public class Framer
{
    public static byte[] NextToken(Stream input, byte[] delimiter)
    {
        int nextByte;

        if ((nextByte = input.ReadByte()) == -1)
            return null;

        var tokenBuffer = new MemoryStream();

        do
        {
            tokenBuffer.WriteByte((byte)nextByte);
            byte[] currentToken = tokenBuffer.ToArray();

            if (EndsWith(currentToken, delimiter))
            {
                int tokenLength = currentToken.Length - delimiter.Length;
                byte[] token = new byte[tokenLength];
                Array.Copy(currentToken, 0, token, 0, tokenLength);

                return token;
            }
        } while ((nextByte = input.ReadByte()) != -1);

        return tokenBuffer.ToArray();
    }

    private static bool EndsWith(byte[] value, byte[] suffix)
    {
        if (value.Length < suffix.Length)
            return false;

        for (int offset = 1; offset <= suffix.Length; offset++)
            if (value[value.Length - offset] != suffix[suffix.Length - offset])
                return false;

        return true;
    }
}
