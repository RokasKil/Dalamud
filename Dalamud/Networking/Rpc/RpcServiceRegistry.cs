using System.Collections.Generic;
using System.Threading;
using SimpleJsonRpc;

namespace Dalamud.Networking.Rpc;

/// <summary>
/// Thread-safe registry of local RPC target objects that are exposed to every connected JsonRpc session.
/// New sessions get all previously registered targets; newly added targets are attached to all active sessions.
/// </summary>
internal class RpcServiceRegistry
{
    private readonly Lock sync = new();
    private readonly List<object> targets = [];
    private readonly List<(string Name, Delegate Handler)> methods = [];
    private readonly List<JsonRpc> activeRpcs = [];

    /// <summary>
    /// Registers a new standalone JSON-RPC method.
    /// </summary>
    /// <param name="name">The name of the method to add.</param>
    /// <param name="handler">The handler to add.</param>
    public void AddMethod(string name, Delegate handler)
    {
        lock (this.sync)
        {
            this.methods.Add((name, handler));
            foreach (var rpc in this.activeRpcs)
            {
                rpc.RegisterMethod(name, handler);
            }
        }
    }

    /// <summary>
    /// Attaches a JsonRpc instance <paramref name="rpc"/> to the registry so it receives all existing service targets.
    /// </summary>
    /// <param name="rpc">The JsonRpc instance to attach and populate with current targets.</param>
    internal void Attach(JsonRpc rpc)
    {
        lock (this.sync)
        {
            this.activeRpcs.Add(rpc);
            foreach (var m in this.methods)
            {
                rpc.RegisterMethod(m.Name, m.Handler);
            }
        }
    }

    /// <summary>
    /// Detaches a JsonRpc instance <paramref name="rpc"/> from the registry (e.g. when a client disconnects).
    /// </summary>
    /// <param name="rpc">The JsonRpc instance being detached.</param>
    internal void Detach(JsonRpc rpc)
    {
        lock (this.sync)
        {
            this.activeRpcs.Remove(rpc);
        }
    }
}
