#!/usr/bin/env dotnet-script
#load "certvalidate-common.csx"

using System.Security.Cryptography.X509Certificates;

if (Args.Count < 2)
{
  Console.WriteLine($"Usage: dotnet-script filename.csx -- CERTIFICATE_UNDER_VERIFICATION [INTERMEDIATE1] [INTERMEDIATE2] ROOT_CA");
}

var certificatePath = Args[0];
var caAndChainPaths = Args.Skip(1);

var certificateUnderValidation = LoadCertificateFromFile(certificatePath);
var caAndChain = new X509Certificate2Collection(caAndChainPaths.Select(file => LoadCertificateFromFile(file)).ToArray());

var chain = new X509Chain();
// .NET 5+ has a new 'CustomTrustStore' mode that permits ignoring the OS trust
// and ExtraTrust stores, and explicitly verify against an expected root CA (and
// its chain). This avoids the PartialChain issues in .NET Core 3 arising from
// the use of AllowUnknownCertificateAuthority, and allows us to trust the
// X509Chain.Build() verification result without extra steps.
chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
chain.ChainPolicy.CustomTrustStore.AddRange(caAndChain);
chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
var isVerified = chain.Build(certificateUnderValidation);

PrintX509VerificationResults(isVerified, chain);

Environment.Exit(isVerified ? 0 : 1);