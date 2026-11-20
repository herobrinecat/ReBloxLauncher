using Microsoft.CSharp.RuntimeBinder;
using ReBloxLauncher.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;
namespace ReBloxLauncher
{
    public class SOAP
    {
        private class MyWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest w = base.GetWebRequest(address);
                w.Timeout = (int)Math.Floor(TimeSpan.FromSeconds(3).TotalMilliseconds);
                return w;
            }
        }

        private static string GenerateContentXML(string baseUrl, string baseContent)
        {
            return $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:SOAP-ENC=\"http://schemas.xmlsoap.org/soap/encoding/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:ns2=\"http://{baseUrl}/RCCServiceSoap\" xmlns:ns1=\"http://{baseUrl}/\" xmlns:ns3=\"http://{baseUrl}/RCCServiceSoap12\">\r\n\t<SOAP-ENV:Body>{baseContent}\t</SOAP-ENV:Body>\r\n</SOAP-ENV:Envelope>";
        }

        private static string GenerateRBXGSContentXML(string baseContent)
        {
            return $"<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:soapenc=\"http://schemas.xmlsoap.org/soap/encoding/\" xmlns:roblox=\"urn:Roblox\">\r\n\t<soap:Body>\r\n\t\t{baseContent}\r\n\t</soap:Body>\r\n</soap:Envelope>";
        }

        private static string SendRequestToGameServer(
       int servicePort = 64989,
       string action = "HelloWorld",
       string content = "\t\t<ns1:HelloWorld>\r\n\t\t</ns1:HelloWorld>",
       string url = "roblox.com",
       string ip = "127.0.0.1",
       bool isRBXGS = false)
        {
            using (MyWebClient webClient = new MyWebClient())
            {
                try
                {
                    string data = isRBXGS ? GenerateRBXGSContentXML(content) : GenerateContentXML(url, content);
                    webClient.Encoding = Encoding.UTF8;
                    webClient.Headers.Add("Accept", "text/xml");
                    webClient.Headers.Add("Expect", "text/xml");
                    webClient.Headers.Add("Cache-Control", "no-cache");
                    webClient.Headers.Add("Pragma", "no-cache");
                    webClient.Headers.Add("SOAPAction", action);
                    return isRBXGS ? webClient.UploadString($"http://{ip}/RBXGS/WebService.dll", data) : webClient.UploadString($"http://{ip}:{servicePort}", data);
                }
                catch (WebException ex1)
                {
                    Console.WriteLine($"<ERROR> Error whilst sending {action} request to service port {servicePort.ToString()}: {ex1.Message}");
                    try
                    {
                        if (ex1.Response == null)
                            return "Error";
                        using (Stream responseStream = ex1.Response.GetResponseStream())
                        {
                            using (StreamReader streamReader = new StreamReader(responseStream))
                            {
                                int num2 = (int)MessageBox.Show(streamReader.ReadToEnd());
                                return streamReader.ReadToEnd();
                            }
                        }
                    }
                    catch (WebException)
                    {
                        return "Error";
                    }
                }
            }
        }

        // Token: 0x06000004 RID: 4 RVA: 0x000022BC File Offset: 0x000004BC
        private static string ParseArguments(List<LuaValueNew> args = null, bool rbxgsMode = false)
        {
            if (args == null)
            {
                return "";
            }
            if (rbxgsMode)
            {
                string text = "";
                foreach (LuaValueNew luaValueNew in args)
                {
                    CallSite<Func<CallSite, object, string, object>> callsite1 = null;
                    CallSite<Func<CallSite, string, object, object>> callsite2 = null;
                    CallSite<Func<CallSite, object, string>> callsite3 = null;
                    CallSite<Func<CallSite, string, object, object>> callsite4 = null;
                    if (callsite3 == null)
                    {
                        callsite3 = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.ConvertExplicit, typeof(string), typeof(SOAP)));
                    }
                    Func<CallSite, object, string> target = callsite3.Target;
                    CallSite<Func<CallSite, object, string>> p__ = callsite3;
                    if (callsite2 == null)
                    {
                        callsite2 = CallSite<Func<CallSite, string, object, object>>.Create(Binder.BinaryOperation(CSharpBinderFlags.None, ExpressionType.AddAssign, typeof(SOAP), new CSharpArgumentInfo[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
                    }
                    Func<CallSite, string, object, object> target2 = callsite2.Target;
                    CallSite<Func<CallSite, string, object, object>> p__2 = callsite2;
                    string arg = text;
                    if (callsite1 == null)
                    {
                        callsite1 = CallSite<Func<CallSite, object, string, object>>.Create(Binder.BinaryOperation(CSharpBinderFlags.None, ExpressionType.Add, typeof(SOAP), new CSharpArgumentInfo[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType | CSharpArgumentInfoFlags.Constant, null)
                        }));
                    }
                    Func<CallSite, object, string, object> target3 = callsite1.Target;
                    CallSite<Func<CallSite, object, string, object>> p__3 = callsite1;
                    if (callsite4 == null)
                    {
                        callsite4 = CallSite<Func<CallSite, string, object, object>>.Create(Binder.BinaryOperation(CSharpBinderFlags.None, ExpressionType.Add, typeof(SOAP), new CSharpArgumentInfo[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
                    }
                    text = target(p__, target2(p__2, arg, target3(p__3, callsite4.Target(callsite4, "<ns1:LuaValue><ns1:type>" + luaValueNew.type + "</ns1:type><ns1:value>", luaValueNew.value), "</ns1:value></ns1:LuaValue>")));
                }
                return text;
            }
            return "";
        }

        private static string CreateScriptExecutionModel(Script script = null, bool rbxgsMode = false)
        {
            return rbxgsMode ? $"<environmentID>{script.environmentID}</environmentID><script>{script.script.Replace("<", "&lt;").Replace("&", "&amp;")}</script><arguments><count>{script.arguments.Count.ToString()}</count><items soapenc:arrayType=\"roblox:LuaValue1[0]\">{SOAP.ParseArguments(script.arguments, true)}</items></arguments><name>{script.name}</name>" : $"<ns1:script>\r\n\t\t\t<ns1:name>{script.name}</ns1:name>\r\n\t\t\t<ns1:script>{script.script.Replace("<", "&lt;").Replace("&", "&amp;")}</ns1:script>\r\n\t\t\t<ns1:arguments>{SOAP.ParseArguments(script.arguments)}</ns1:arguments></ns1:script>";
        }

        public static string Execute(int servicePort = 64989, string jobID = "Test", Script script = null, string url = "roblox.com", string ip = "127.0.0.1", bool rbxgsMode = false)
        {
            if (rbxgsMode)
            {
                return SOAP.SendRequestToGameServer(0, "Execute", "<roblox:Execute>" + SOAP.CreateScriptExecutionModel(script, true) + "</roblox:Execute>", "", ip, true);
            }
            return SOAP.SendRequestToGameServer(servicePort, "Execute", string.Concat(new string[]
            {
                "\t\t<ns1:Execute>\r\n\t\t\t<ns1:jobID>",
                jobID,
                "</ns1:jobID>\r\n\t\t\t",
                SOAP.CreateScriptExecutionModel(script, false),
                "\r\n\t\t</ns1:Execute>"
            }), url, ip, false);
        }

        public static string HelloWorld(int servicePort = 64989, string url = "roblox.com", string ip = "127.0.0.1", bool rbxgsMode = false)
        {
            return SOAP.SendRequestToGameServer(servicePort, content: rbxgsMode ? "<roblox:HelloWorld/>" : "\t\t<ns1:HelloWorld>\r\n\t\t</ns1:HelloWorld>", url: url, ip: ip, isRBXGS: rbxgsMode);
        }


    }

}
