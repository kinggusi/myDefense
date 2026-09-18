using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace MyDefense.Auth
{
    /// <summary>Windows first slice. Other platforms MUST supply a Keychain/Keystore adapter; no plaintext fallback.</summary>
    public sealed class SecureCredentialStore : ISecureCredentialStore
    {
        private readonly string path, apiBaseUrl;
        private readonly FileStream lease;
        public SecureCredentialStore(string directory, string endpoint)
        {
            apiBaseUrl = endpoint;
            path = Path.Combine(directory, ScopeName(endpoint) + ".bin");
            RequireSupported();
            Directory.CreateDirectory(directory);
            // OS file sharing lock is released on process exit. Two clients must choose different dev profiles.
            lease = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        public void Dispose() { lease.Dispose(); }
        public static string ScopeName(string endpoint)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(endpoint))).Replace("-", "").ToLowerInvariant();
        }
        public static string NewGuestSecret()
        {
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            string result = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            Array.Clear(bytes, 0, bytes.Length);
            return result;
        }
        public StoredCredential Load()
        {
            RequireSupported();
            if (!File.Exists(path)) return null;
            if (new FileInfo(path).Length > 65536) throw new InvalidDataException("Credential envelope too large.");
            byte[] plain = Transform(File.ReadAllBytes(path), false);
            try
            {
                var value = JsonUtility.FromJson<StoredCredential>(Encoding.UTF8.GetString(plain));
                if (value == null || value.version != 1 || value.apiBaseUrl != apiBaseUrl
                    || (string.IsNullOrEmpty(value.guestSecret) && string.IsNullOrEmpty(value.refreshToken))
                    || (!string.IsNullOrEmpty(value.guestSecret) && value.guestSecret.Length != 43))
                    throw new InvalidDataException("Invalid credential envelope.");
                return value;
            }
            finally { Array.Clear(plain, 0, plain.Length); }
        }
        public void Save(StoredCredential credential)
        {
            RequireSupported();
            if (credential == null || credential.apiBaseUrl != apiBaseUrl) throw new InvalidDataException("Credential scope mismatch.");
            byte[] plain = Encoding.UTF8.GetBytes(JsonUtility.ToJson(credential));
            byte[] encrypted;
            try { encrypted = Transform(plain, true); }
            finally { Array.Clear(plain, 0, plain.Length); }
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + ".pending";
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            { stream.Write(encrypted, 0, encrypted.Length); stream.Flush(true); }
            // Never erase the last working credential before its replacement is durable.
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
        }
        private static void RequireSupported()
        {
#if !UNITY_EDITOR_WIN && !UNITY_STANDALONE_WIN
            throw new PlatformNotSupportedException("Secure Keychain/Keystore adapter required for this platform.");
#endif
        }
        private static byte[] Transform(byte[] value, bool protect)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            var input = new Blob { size = value.Length, data = Marshal.AllocHGlobal(value.Length) };
            var output = new Blob();
            try
            {
                Marshal.Copy(value, 0, input.data, value.Length);
                bool ok = protect
                    ? CryptProtectData(ref input, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1, out output)
                    : CryptUnprotectData(ref input, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1, out output);
                if (!ok) throw new CryptographicException("Windows credential protection failed.");
                var result = new byte[output.size];
                Marshal.Copy(output.data, result, 0, result.Length);
                return result;
            }
            finally
            {
                for (int i = 0; i < input.size; i++) Marshal.WriteByte(input.data, i, 0);
                Marshal.FreeHGlobal(input.data);
                if (output.data != IntPtr.Zero)
                {
                    for (int i = 0; i < output.size; i++) Marshal.WriteByte(output.data, i, 0);
                    LocalFree(output.data);
                }
            }
#else
            throw new PlatformNotSupportedException();
#endif
        }
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        [StructLayout(LayoutKind.Sequential)] private struct Blob { public int size; public IntPtr data; }
        [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CryptProtectData(ref Blob input, string description, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, out Blob output);
        [DllImport("crypt32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CryptUnprotectData(ref Blob input, IntPtr description, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, out Blob output);
        [DllImport("kernel32.dll")] private static extern IntPtr LocalFree(IntPtr memory);
#endif
    }
}
