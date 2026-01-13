using System.Collections.Generic;

namespace DSPMCP.Handlers
{
    /// <summary>
    /// Interface for method handlers. Each handler manages a group of related methods.
    /// </summary>
    public interface IMethodHandler
    {
        /// <summary>
        /// Returns the methods this handler supports.
        /// Key: method name, Value: description
        /// </summary>
        Dictionary<string, string> GetMethods();

        /// <summary>
        /// Handle a method call.
        /// </summary>
        /// <param name="method">The method name</param>
        /// <param name="requestId">The JSON-RPC request ID</param>
        /// <param name="params">Optional parameters (raw JSON string)</param>
        /// <returns>JSON-RPC response string</returns>
        string Handle(string method, string requestId, string paramsJson);
    }
}
