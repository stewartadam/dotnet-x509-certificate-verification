#!/usr/bin/env dotnet-script
#load "certvalidate-common.csx"

using System.Security.Cryptography.X509Certificates;

if (Args.Count < 1)
{
  Console.WriteLine($"Usage: dotnet-script filename.csx -- CERTIFICATE_UNDER_VERIFICATION");
  Environment.Exit(1);
}

var certificatePath = Args[0];

var certificateUnderValidation = LoadCertificateFromFile(certificatePath);

var chain = new X509Chain();
chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
var isVerified = chain.Build(certificateUnderValidation);

PrintX509VerificationResults(isVerified, chain);

Environment.Exit(isVerified ? 0 : 1);