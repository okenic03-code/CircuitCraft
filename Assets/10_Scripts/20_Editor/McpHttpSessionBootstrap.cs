#if UNITY_EDITOR
using System;
using MCPForUnity.Editor.Services;
using UnityEditor;
using UnityEngine;

namespace CircuitCraft.Editor
{
    /// <summary>
    /// Temporary bootstrap to recover MCP HTTP session after editor restarts.
    /// </summary>
    [InitializeOnLoad]
    internal static class McpHttpSessionBootstrap
    {
        static McpHttpSessionBootstrap()
        {
            EditorApplication.delayCall += StartSession;
        }

        private static async void StartSession()
        {
            try
            {
                EditorPrefs.SetBool("MCPForUnity.UseHttpTransport", true);

                var bridge = new BridgeControlService();
                await bridge.StartAsync();
                var verification = await bridge.VerifyAsync();
                Debug.Log(
                    $"MCP_HTTP_BOOTSTRAP: success={verification.Success}, ping={verification.PingSucceeded}, handshake={verification.HandshakeValid}, message={verification.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"MCP_HTTP_BOOTSTRAP_ERROR: {ex.Message}");
            }
        }
    }
}
#endif
