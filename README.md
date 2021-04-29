# Validating X.509 Certificates using the .NET APIs

Validating a certificate in .NET can be done with the help of the [X509Chain.Build()](https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509chain.build?view=net-5.0#System_Security_Cryptography_X509Certificates_X509Chain_Build_System_Security_Cryptography_X509Certificates_X509Certificate2_) method, which returns a boolean value indicating if a certificate under verification could be verified using the configured policy.

Ordinarily, this method works as expected; however when working with self-signed certificates (or attempting to verify a certificate against a specific root CA), there are issues that require additional verifications by the developer that are not well documented by the .NET docs.

This repo contains code samples demonstrating how to properly validate certificates with .NET Core 3.x and 5+, including for self-signed certificate authorities (CAs).

## Summary of issues

In .NET 5 and higher, a new [X509ChainPolicy.TrustMode property](https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509chainpolicy.trustmode?view=net-5.0) is available which can override the OS trust stores and perform certificate verification using **only** roots and intermediaries added to the [X509Chain.CustomTrustStore property](https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509chainpolicy.customtruststore?view=net-5.0), effectively explicitly pinning the root CA when performing verification; all is well.

In .NET Core 3.x and prior, the implementation has two 'gotchas' that are not well described in the X509-related class documentation:

1. Certificates are always verified against the OS trust store, plus certificates added to ExtraStore.

   This means that a `X509Chain.Build()` verification only tells us only that a chain terminated in **one** of the trusted certificates, but does not permit us to specify **which** should have matched.

2. When enabling the [`AllowUnknownCertificateAuthority` flag](https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509verificationflags?view=net-5.0) to work with self-signed root CAs, both the `UntrustedRoot` and `PartialChain` statuses are ignored.
   Therefore, `X509Chain.Build()` will return `true` even if your certificate under validation was not issued by any of the trusted root CAs in the OS trusted roots or ExtraStore (i.e., it considers a new chain consisting only the certificate under validation and determines that to be a partial chain, which is then ignored).
   [Up until very recently](https://github.com/dotnet/dotnet-api-docs/pull/6660), this behavior was undocumented and the .NET docs incorrectly described behavior when enabling this flag.

Both of these gotchas require a developer perform manual verification of correct chain termination (i.e. checking the last item in the chain is indeed the signing root CA we expect), and needs to be done **manually** and **separately** from `X509Chain.Build()`.

[dotnet/runtime#26449](https://github.com/dotnet/runtime/issues/26449) and [dotnet/runtime#49615](https://github.com/dotnet/runtime/issues/49615) have more details.

Thus, these code samples demonstrate both the older .NET Core-based method that includes an additional verification, as well as the newer .NET 5+ that supports verification against a specific root CA.

## Code samples

The samples are inline C# code that makes use of [dotnet script](https://github.com/filipw/dotnet-script). If you do not have it, install with:

```sh
dotnet tool install -g dotnet-script
```

The scripts will load PEM-formatted files (provided the file extension is `.pem`), otherwise it assumes DER-formatted input files. Run them without arguments to view usage instructions.

| I want to... | Your target .NET SDK | Code sample |
|-|-|-|
| Verify a certificate against CAs in OS trust store and/or ExtraStore | .NET Core 1.x - 3.x or .NET 5+ | `certvalidate-anysdk.csx` |
| Verify a certificate against a self-signed CA; or verify a certificate while pinning to a specific root CA | .NET Core 1.x - 3.x | `certvalidate-selfsigned-dotnetcore.csx` |
| Verify a certificate against a self-signed CA; or verify a certificate while pinning to a specific root CA | .NET 5 or higher | `certvalidate-selfsigned-dotnet5+.csx` |

Note that all of the scripts make use of `certvalidate-common.csx` which includes some helper methods.

### Generating sample data

Scripts to generate sample data are also included in the repo. Ensure you have OpenSSL installed and available on your `$PATH` to use them.

1. Generate self-issued certificates: creates 2 self-signed root CAs and a single certificate from each (`ca.foo.com` issuing `device01.foo.com` and `ca.bar.com` issuing `sensor01.bar.com`), storing the certificates into the `certificates` folder:

   ```sh
   ./create_certificates.sh
   ```

2. Well-known certificates: downloads the public X.509 certificates published by some well-known websites to the `certificates` folder:

   ```sh
   ./download_known_certificates.sh
   ```

### Running the code samples

1. Validate a well-known website's certificate against the OS trust store:

   ```sh
   dotnet-script certvalidate-anysdk.csx -- certificates/wikipedia.org.pem
   ```

2. Validate a self-issued X.509 certificate against a self-signed root CA (via .NET Core 1.x-3.x APIs, and then .NET 5+ APIs):

   ```sh
   dotnet-script certvalidate-selfsigned-dotnetcore.csx -- certificates/device01.foo.com.pem certificates/ca.foo.com.pem

   dotnet-script certvalidate-selfsigned-dotnet5+.csx -- certificates/device01.foo.com.pem certificates/ca.foo.com.pem
   ```

3. Now try it again, specifying the wrong root CA for the certificate under validation (we expect failures):

   ```sh
   dotnet-script certvalidate-selfsigned-dotnetcore.csx -- certificates/device01.foo.com.pem certificates/ca.bar.com.pem

   dotnet-script certvalidate-selfsigned-dotnet5+.csx -- certificates/device01.foo.com.pem certificates/ca.bar.com.pem
   ```

   *Note how `X509Chain.Build()` returned `true` in the .NET Core samples, even though the certificate under verification was entirely unrelated to the CA! This is the `PartialChain` gotcha described above. Only after manual verification of the chain is the issue revealed.*

4. Try validating an otherwise well-known certificate but pin it against an unrelated root CA (again, we expect failures):

   ```sh
   dotnet-script certvalidate-selfsigned-dotnetcore.csx -- certificates/mozilla.org.pem certificates/ca.bar.com.pem

   dotnet-script certvalidate-selfsigned-dotnet5+.csx -- certificates/mozilla.org.pem certificates/ca.bar.com.pem
   ```
