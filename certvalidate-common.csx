using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

private static byte[] ConvertPEMtoDER(byte[] pemData)
{
  // strips ---HEADERS--- then base64-decodes the PEM body to arrive at the
  // DER-encoded certificate data
  string b64String = Encoding.UTF8.GetString(pemData);
  b64String = Regex.Replace(b64String, "-+BEGIN CERTIFICATE-+", "");
  b64String = Regex.Replace(b64String, "-+END CERTIFICATE-+", "");
  return Convert.FromBase64String(b64String.Trim());
}

private static X509Certificate2 LoadCertificateFromFile(string certificatePath)
{
  byte[] derData;

  if (certificatePath.EndsWith(".pem"))
  {
    var pemData = File.ReadAllBytes(certificatePath);
    derData = ConvertPEMtoDER(pemData);
  }
  else {
    // Already using a DER-formatted certificate
     derData = File.ReadAllBytes(certificatePath);
  }
  return new X509Certificate2(derData);
}

private static void PrintX509VerificationResults(bool isVerified, X509Chain chain)
{
  Console.WriteLine($"X509Chain.Build() verification: {isVerified}\n");

  if (chain.ChainStatus.Length == 0)
  {
    Console.WriteLine("Chain status: N/A (no flags)");
  }
  else
  {
    Console.WriteLine("Chain status:");
    foreach (var status in chain.ChainStatus)
    {
      Console.WriteLine($"- {status.Status}: {status.StatusInformation}");
    }
    Console.WriteLine();
  }


  Console.WriteLine("Chain elements:");
  foreach (var element in chain.ChainElements)
  {
    Console.WriteLine($"- {element.Certificate.Thumbprint} ({element.Certificate.Subject}, expiry {element.Certificate.GetExpirationDateString()})");
  }
  Console.WriteLine();
}