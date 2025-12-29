using System;
using System.Security.Cryptography;
using System.IO;
using System.Text;

var password = "4jafero:!34ouOI;";
var privateKey = "123456789012345678901234";

var tDes = TripleDES.Create();
tDes.Key = new ASCIIEncoding().GetBytes(privateKey);
tDes.IV = new ASCIIEncoding().GetBytes(privateKey.Substring(privateKey.Length - 8));

using var ms = new MemoryStream();
using (var cs = new CryptoStream(ms, TripleDES.Create().CreateEncryptor(tDes.Key, tDes.IV), CryptoStreamMode.Write))
{
    var toEncrypt = new UnicodeEncoding().GetBytes(password);
    cs.Write(toEncrypt, 0, toEncrypt.Length);
    cs.FlushFinalBlock();
}

var encrypted = Convert.ToBase64String(ms.ToArray());
Console.WriteLine("Encrypted: " + encrypted);
Console.WriteLine("Expected:  R8KPHiVZSHE3q8g9JP7vVGi3Ar6aydgKZsF501xK+z2UN3n0M99yng==");
Console.WriteLine("Match: " + (encrypted == "R8KPHiVZSHE3q8g9JP7vVGi3Ar6aydgKZsF501xK+z2UN3n0M99yng=="));
