using System;
using System.Runtime.InteropServices;

namespace WheresMyImplant
{
    class vaultcli
    {
        [DllImport("vaultcli.dll", CharSet = CharSet.Auto)]
        public static extern int VaultEnumerateVaults(
            Int32 unknown,
            ref Int32 dwVaults,
            ref IntPtr ppVaultGuids
        );
    }
}