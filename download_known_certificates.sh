#!/bin/bash
# Downloads a PEM-formatted certificate used by a website.

set -euo pipefail

base_dir="certificates"

download_certificate() {
  HOST="$1"
  PORT="${2:-443}"
  output_file="${base_dir}/${HOST}.pem"

  true | openssl s_client -servername "${HOST}" -connect "${HOST}:${PORT}" 2>&1 | sed -ne '/-BEGIN CERTIFICATE-/,/-END CERTIFICATE-/p' > "${output_file}"
  echo "Certificate for ${HOST} saved to ${output_file}"
}

mkdir -p "${base_dir}"

download_certificate microsoft.com
download_certificate apple.com
download_certificate google.com
download_certificate mozilla.org
download_certificate wikipedia.org