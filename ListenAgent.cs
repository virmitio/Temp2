﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CoApp.Toolkit.Extensions;
using CoApp.Toolkit.Pipes;
using CoApp.Toolkit.Tasks;
using CoApp.Toolkit.Utility;
using Newtonsoft.Json.Linq;

namespace AutoBuild
{
    public class RequestHandler
    {
        public virtual Task Put(HttpListenerResponse response, string relativePath, byte[] data)
        {
            return null;
        }

        public virtual Task Get(HttpListenerResponse response, string relativePath, UrlEncodedMessage message)
        {
            return null;
        }

        public virtual Task Post(HttpListenerResponse response, string relativePath, UrlEncodedMessage message)
        {
            return null;
        }

        public virtual Task Head(HttpListenerResponse response, string relativePath, UrlEncodedMessage message)
        {
            return null;
        }
    }

    public class Listener
    {
        private readonly HttpListener _listener = new HttpListener();
        private readonly List<string> _hosts = new List<string>();
        private readonly List<int> _ports = new List<int>();
        private readonly Dictionary<string, RequestHandler> _paths = new Dictionary<string, RequestHandler>();
        private Task<HttpListenerContext> _current = null;

        public Listener()
        {}

        Regex ipAddrRx = new Regex(@"^([1-9]|[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5])(\.([0-9]|[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5])){3}$");
        Regex hostnameRx = new Regex(@"(([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\-]*[a-zA-Z0-9])\.)*");

        public void AddHost(string host)
        {
            if (string.IsNullOrEmpty(host))
                return;

            host = host.ToLower();

            if (_hosts.Contains(host))
                return;

            if (host == "+" || host == "*" || ipAddrRx.IsMatch(host) || hostnameRx.IsMatch(host))
            {
                _hosts.Add(host);
                if (_current != null)
                    Restart();
                return;
            }
        }

        public void RemoveHost(string host)
        {
            if (string.IsNullOrEmpty(host))
                return;

            host = host.ToLower();

            if (_hosts.Contains(host))
            {
                _hosts.Remove(host);
                if (_current != null)
                    Restart();
            }
        }

        public void AddPort(int port)
        {
            if (port <= 0 || port > 65535)
                return;

            if (_ports.Contains(port))
                return;

            _ports.Add(port);
            if (_current != null)
                Restart();
        }

        public void RemovePort(int port)
        {
            if (_ports.Contains(port))
            {
                _ports.Remove(port);
                if (_current != null)
                    Restart();
            }
        }

        public void AddHandler(string path, RequestHandler handler)
        {
            if (string.IsNullOrEmpty(path))
                path = "/";
            path = path.ToLower();

            if (!path.StartsWith("/"))
                path = "/" + path;

            if (!path.EndsWith("/"))
                path = path + "/";

            if (_paths.ContainsKey(path))
                return;

            _paths.Add(path, handler);
            if (_current != null)
                Restart();
        }

        public void RemoveHandler(string path)
        {
            if (string.IsNullOrEmpty(path))
                path = "/";

            path = path.ToLower();

            if (!path.StartsWith("/"))
                path = "/" + path;

            if (!path.EndsWith("/"))
                path = path + "/";

            if (_paths.ContainsKey(path))
            {
                _paths.Remove(path);
                if (_current != null)
                    Restart();
            }
        }


        public void Restart()
        {
            try
            { Stop(); }
            catch
            {}

            try
            { Start(); }
            catch
            {}

        }

        public void Stop()
        {
            _listener.Stop();
            _current = null;
        }


        public void Start()
        {
            if (_current == null)
            {
                _listener.Prefixes.Clear();
                foreach (var host in _hosts)
                {
                    foreach (var port in _ports)
                    {
                        foreach (var path in _paths.Keys)
                        {
                            Console.WriteLine("Adding `http://{0}:{1}{2}`".format(host, port, path));
                            _listener.Prefixes.Add("http://{0}:{1}{2}".format(host, port, path));
                        }
                    }
                }
            }

            _listener.Start();

            _current = Task.Factory.FromAsync<HttpListenerContext>(_listener.BeginGetContext, _listener.EndGetContext, _listener);

            _current.ContinueWith(antecedent =>
            {
                if (antecedent.IsCanceled || antecedent.IsFaulted)
                {
                    _current = null;
                    return;
                }
                Start(); // start a new listener.

                try
                {
                    var request = antecedent.Result.Request;
                    var response = antecedent.Result.Response;
                    var url = request.Url;
                    var path = url.AbsolutePath.ToLower();

                    var handlerKey = _paths.Keys.OrderByDescending(each => each.Length).Where(path.StartsWith).FirstOrDefault();
                    if (handlerKey == null)
                    {
                        // no handler
                        response.StatusCode = 404;
                        response.Close();
                        return;
                    }

                    var relativePath = path.Substring(handlerKey.Length);

                    if (string.IsNullOrEmpty(relativePath))
                        relativePath = "index";

                    var handler = _paths[handlerKey];
                    Task handlerTask = null;
                    var length = request.ContentLength64;

                    switch (request.HttpMethod)
                    {

                        case "PUT":
                            try
                            {
                                var putData = new byte[length];
                                var read = 0;
                                var offset = 0;
                                do
                                {
                                    read = request.InputStream.Read(putData, offset, (int)length - offset);
                                    offset += read;
                                } while (read > 0 && offset < length);

                                handlerTask = handler.Put(response, relativePath, putData);
                            }
                            catch (Exception e)
                            {
                                HandleException(e);
                                response.StatusCode = 500;
                                response.Close();
                            }
                            break;

                        case "HEAD":
                            try
                            {
                                handlerTask = handler.Head(response, relativePath, new UrlEncodedMessage(relativePath + "?" + url.Query));
                            }
                            catch (Exception e)
                            {
                                HandleException(e);
                                response.StatusCode = 500;
                                response.Close();
                            }
                            break;

                        case "GET":
                            try
                            {
                                handlerTask = handler.Get(response, relativePath, new UrlEncodedMessage(relativePath + "?" + url.Query));
                            }
                            catch (Exception e)
                            {
                                HandleException(e);
                                response.StatusCode = 500;
                                response.Close();
                            }
                            break;

                        case "POST":
                            try
                            {
                                var postData = new byte[length];
                                var read = 0;
                                var offset = 0;
                                do
                                {
                                    read = request.InputStream.Read(postData, offset, (int)length - offset);
                                    offset += read;
                                } while (read > 0 && offset < length);

                                handlerTask = handler.Post(response, relativePath, new UrlEncodedMessage(relativePath + "?" + Encoding.UTF8.GetString(postData)));
                            }
                            catch (Exception e)
                            {
                                HandleException(e);
                                response.StatusCode = 500;
                                response.Close();
                            }
                            break;
                    }

                    if (handlerTask != null)
                    {
                        handlerTask.ContinueWith((antecedent2) =>
                        {
                            if (antecedent2.IsFaulted && antecedent2.Exception != null)
                            {
                                var e = antecedent2.Exception.InnerException;
                                HandleException(e);
                                response.StatusCode = 500;
                            }

                            response.Close();
                        }, TaskContinuationOptions.AttachedToParent);
                    }
                    else
                    {
                        // nothing retured? must be unimplemented.
                        response.StatusCode = 405;
                        response.Close();
                    }
                }
                catch (Exception e)
                {
                    HandleException(e);
                }
            }, TaskContinuationOptions.AttachedToParent);
        }

        public static void HandleException(Exception e)
        {
            if (e is AggregateException)
                e = (e as AggregateException).Flatten().InnerExceptions[0];

            Console.WriteLine("{0} -- {1}\r\n{2}", e.GetType(), e.Message, e.StackTrace);
        }
    }

    public static class HttpListenerResponseExtensions
    {
        public static void WriteString(this HttpListenerResponse response, string format, params string[] args)
        {
            var text = string.Format(format, args);
            var buffer = Encoding.UTF8.GetBytes(text);
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Flush();
        }
    }


    public class PostHandler : RequestHandler
    {
        private ProcessUtility _cmdexe = new ProcessUtility("cmd.exe");
        public PostHandler()
        {
        }

        public override Task Post(HttpListenerResponse response, string relativePath, UrlEncodedMessage message)
        {
            var payload = (string)message["payload"];
            if (payload == null)
            {
                response.StatusCode = 500;
                response.Close();
                return "".AsResultTask();
            }

            var result = Task.Factory.StartNew(() =>
            {
                try
                {
                    var json = JObject.Parse(payload);
                    Console.WriteLine("MSG Process begin {0}", json.commits.Count);

                    var count = json.commits.Count;
                    var doSiteRebuild = false;
                    for (int i = 0; i < count; i++)
                    {
                        var username = json.commits[i].author.username.Value;
                        var commitMessage = json.commits[i].message.Value;
                        var repository = json["repository"]["name"].ToString();
                        var myref = json["ref"].ToString();

                        var url = (string)json.commits[i].url.Value;
                        if (repository == "coapp.org")
                        {
                            doSiteRebuild = true;
                        }

                        Bitly.Shorten(url).ContinueWith((bitlyAntecedent) =>
                        {
                            var commitUrl = bitlyAntecedent.Result;

                            var handle = _aliases.ContainsKey(username) ? _aliases[username] : username;
                            var sz = repository.Length + handle.Length + commitUrl.Length + commitMessage.Length + 10;
                            var n = 140 - sz;

                            if (n < 0)
                            {
                                commitMessage = commitMessage.Substring(0, (commitMessage.Length + n) - 3) + "...";
                            }
                            _tweeter.Tweet("{0} => {1} via {2} {3}", repository, commitMessage, handle, commitUrl);
                            Console.WriteLine("{0} => {1} via {2} {3}", repository, commitMessage, handle, commitUrl);
                        });

                    }
                    // just rebuild the site once for a given batch of rebuild commit messages.
                    if (doSiteRebuild)
                    {
                        Task.Factory.StartNew(() =>
                        {
                            try
                            {
                                Console.WriteLine("Rebuilding website.");
                                Environment.CurrentDirectory = Environment.GetEnvironmentVariable("STORAGE");
                                if (_cmdexe.Exec(@"/c rebuild_site.cmd") != 0)
                                {
                                    Console.WriteLine("Site rebuild result:\r\n{0}", _cmdexe.StandardOut);
                                    return;
                                }

                                Console.WriteLine("Rebuilt Website.");
                            }
                            catch (Exception e)
                            {
                                Listener.HandleException(e);
                            }

                        });
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error handling uploaded package: {0} -- {1}\r\n{2}", e.GetType(), e.Message, e.StackTrace);
                    Listener.HandleException(e);
                    response.StatusCode = 500;
                    response.Close();
                }
            }, TaskCreationOptions.AttachedToParent);

            result.ContinueWith(antecedent =>
            {
                if (result.IsFaulted)
                {
                    var e = antecedent.Exception.InnerException;
                    Console.WriteLine("Error handling commit message: {0} -- {1}\r\n{2}", e.GetType(), e.Message, e.StackTrace);
                    Listener.HandleException(e);
                    response.StatusCode = 500;
                    response.Close();
                }
            }, TaskContinuationOptions.OnlyOnFaulted);

            return result;
        }
    }



    class ListenAgent
    {
        int main(string[] args)
        {
            var hosts = new string[] { "*" };
            var ports = new int[] { 80 };
            var commitMessage = "trigger";
            var packageUpload = "upload";

            try
            {
                var listener = new Listener();

                // get startup information.

                foreach (var host in hosts)
                {
                    listener.AddHost(host);
                }

                foreach (var port in ports)
                {
                    listener.AddPort(port);
                }


                listener.AddHandler(commitMessage, new CommitMessageHandler(tweetCommits, aliases));

                listener.Start();

                Console.WriteLine("Press ctrl-c to stop the listener.");

                while (true)
                {
                    // one day, Ill put a check to restart the server in here.
                    Thread.Sleep(60 * 1000);

                }

                listener.Stop();
            }
            catch (Exception e)
            {
                Listener.HandleException(e);
                CancellationTokenSource.Cancel();
            }


            return -1;
        }
    }
}