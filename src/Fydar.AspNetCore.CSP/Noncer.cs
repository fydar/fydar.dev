using System;

namespace Fydar.AspNetCore.CSP;

public class Noncer
{
    public string Nonce
    {
        get
        {
            if (string.IsNullOrEmpty(field))
            {
                using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
                byte[] nonceBytes = new byte[32];
                rng.GetBytes(nonceBytes);
                field = Convert.ToBase64String(nonceBytes);
            }
            return field;
        }
    }
}
