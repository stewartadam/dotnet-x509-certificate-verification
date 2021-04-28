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
chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
// This ignores UntrustedRoot but *also* PartialChain - must validate manually below
chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
chain.ChainPolicy.ExtraStore.AddRange(caAndChain);
var isVerified = chain.Build(certificateUnderValidation);

PrintX509VerificationResults(isVerified, chain);

// Because PartialChain is also ignored by AllowUnknownCertificateAuthority,
// X509Chain.Build() will return true even when the chain is incomplete -
// i.e., even when the certificate under validation was *not* issued by any of
// the CAs in the OS trust store and those added to ExtraStore.
//
// As a result, an explicit verification on the last element in the chain is
// necessary in addition to X509Chain.Build() return value to ensure a completed
// chain that terminates with the intended root certificate:
var isSignedByExpectedRoot = chain.ChainElements[^1].Certificate.RawData.SequenceEqual(caAndChain[^1].RawData);
Console.WriteLine($"Certificate chain terminates at expected CA root ({caAndChain[^1].Thumbprint}): {isSignedByExpectedRoot}");

// Per above, be wary not to blindly trust the result of X509Chain.Build() when
// using AllowUnknownCertificateAuthority.
var isSuccess = isVerified && isSignedByExpectedRoot;
Console.WriteLine($"Overall success: {isSuccess}");

Environment.Exit(isSuccess ? 0 : 1);