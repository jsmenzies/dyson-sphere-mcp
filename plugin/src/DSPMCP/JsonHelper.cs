using System;
using System.Collections.Generic;
using System.Text;

namespace DSPMCP
{
    /// <summary>
    /// Lightweight JSON builder for creating JSON responses without external dependencies.
    /// </summary>
    public class JsonBuilder
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private bool _firstItem = true;
        private readonly Stack<char> _closers = new Stack<char>();

        public JsonBuilder StartObject()
        {
            AppendCommaIfNeeded();
            _sb.Append('{');
            _closers.Push('}');
            _firstItem = true;
            return this;
        }

        public JsonBuilder EndObject()
        {
            _sb.Append(_closers.Pop());
            _firstItem = false;
            return this;
        }

        public JsonBuilder StartArray()
        {
            AppendCommaIfNeeded();
            _sb.Append('[');
            _closers.Push(']');
            _firstItem = true;
            return this;
        }

        public JsonBuilder EndArray()
        {
            _sb.Append(_closers.Pop());
            _firstItem = false;
            return this;
        }

        public JsonBuilder Key(string key)
        {
            AppendCommaIfNeeded();
            _sb.Append('"').Append(EscapeString(key)).Append("\":");
            _firstItem = true; // Next value doesn't need comma
            return this;
        }

        public JsonBuilder Value(string value)
        {
            AppendCommaIfNeeded();
            if (value == null)
                _sb.Append("null");
            else
                _sb.Append('"').Append(EscapeString(value)).Append('"');
            return this;
        }

        public JsonBuilder Value(int value)
        {
            AppendCommaIfNeeded();
            _sb.Append(value);
            return this;
        }

        public JsonBuilder Value(long value)
        {
            AppendCommaIfNeeded();
            _sb.Append(value);
            return this;
        }

        public JsonBuilder Value(float value)
        {
            AppendCommaIfNeeded();
            _sb.Append(value.ToString("G"));
            return this;
        }

        public JsonBuilder Value(double value)
        {
            AppendCommaIfNeeded();
            _sb.Append(value.ToString("G"));
            return this;
        }

        public JsonBuilder Value(bool value)
        {
            AppendCommaIfNeeded();
            _sb.Append(value ? "true" : "false");
            return this;
        }

        public JsonBuilder Prop(string key, string value)
        {
            return Key(key).Value(value);
        }

        public JsonBuilder Prop(string key, int value)
        {
            return Key(key).Value(value);
        }

        public JsonBuilder Prop(string key, long value)
        {
            return Key(key).Value(value);
        }

        public JsonBuilder Prop(string key, float value)
        {
            return Key(key).Value(value);
        }

        public JsonBuilder Prop(string key, double value)
        {
            return Key(key).Value(value);
        }

        public JsonBuilder Prop(string key, bool value)
        {
            return Key(key).Value(value);
        }

        public JsonBuilder AppendRaw(string rawJson)
        {
            AppendCommaIfNeeded();
            _sb.Append(rawJson);
            return this;
        }

        private void AppendCommaIfNeeded()
        {
            if (!_firstItem)
                _sb.Append(',');
            _firstItem = false;
        }

        private static string EscapeString(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        public override string ToString() => _sb.ToString();
    }

    /// <summary>
    /// Helper for creating JSON-RPC responses.
    /// </summary>
    public static class JsonRpc
    {
        public static string Success(string id, string resultJson)
        {
            return $"{{\"jsonrpc\":\"2.0\",\"result\":{resultJson},\"id\":{id}}}";
        }

        public static string Error(string id, int code, string message)
        {
            var escaped = message?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "Unknown error";
            return $"{{\"jsonrpc\":\"2.0\",\"error\":{{\"code\":{code},\"message\":\"{escaped}\"}},\"id\":{id}}}";
        }

        public static string GameNotLoaded(string id)
        {
            return Error(id, -32001, "Game not loaded (main menu?)");
        }
    }
}
