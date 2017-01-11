using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Text;
using FaugaCalc.Business.Entities;
using System.Xml.Linq;

namespace FaugaCalc.Web
{
    public static class Tracer
    {
        private const string TraceDateTimeFormat = "dd.MM.yyyy HH:mm:ss.fff";

        public static TraceSwitch DefaultSwitch = new TraceSwitch("traceLevel", "A TraceSwitch to control tracing");
        public static TraceSwitch ServiceMethodsSwitch = new TraceSwitch("serviceMethodsTraceLevel", "A TraceSwitch to control tracing");

        #region Tracing methods

        public static void TraceException(Exception exc)
        {
            Trace.Indent();

            // form exception message
            string message, details;
            ExceptionHelper.CollectDetails(exc, out message, out details);
            StringBuilder sb = new StringBuilder();
            sb.Append(message).Append(Environment.NewLine).Append(details);

            // write exception
            TraceMessage(sb.ToString());
            //EventLog.WriteEntry("Application", sb.ToString(), EventLogEntryType.Error);
            Trace.Unindent();
        }

        public static void TraceExceptionIf(bool traceLevel, Exception e)
        {
            if (traceLevel)
                TraceException(e);
        }

        public static void TraceMessage(string text)
        {
			int indentLevel = Trace.IndentLevel;
			string date = DateTime.Now.ToString(TraceDateTimeFormat);
			Trace.IndentLevel = 0;
			var message = date + text.PadLeft(text.Length + Trace.IndentSize * indentLevel + 1);
			Trace.WriteLine(message);
			MailManager.SendErrorReport(message);
			Trace.IndentLevel = indentLevel;
        }

		public static void TraceMessageInLogOnly(string text)
		{
			int indentLevel = Trace.IndentLevel;
			string date = DateTime.Now.ToString(TraceDateTimeFormat);
			Trace.IndentLevel = 0;
			var message = date + text.PadLeft(text.Length + Trace.IndentSize * indentLevel + 1);
			Trace.WriteLine(message);
			Trace.IndentLevel = indentLevel;
		}

        public static void TraceMessage(string format, params object[] args)
        {
            TraceMessage(string.Format(format, args));
        }

        public static void TraceMessageIf(bool traceLevel, string text)
        {
            if (traceLevel)
                TraceMessage(text);
        }

        // for consistency - all tracing functions will be called from Utils class
        public static void Indent()
        {
            Trace.Indent();
        }

        // for consistency - all tracing functions will be called from Utils class
        public static void Unindent()
        {
            Trace.Unindent();
        }

        #endregion

		public static void TraceProjectSave(Project project)
		{
			var directory = HttpContext.Current.Server.MapPath("Saves");
			if (!Directory.Exists(directory))
				Directory.CreateDirectory(directory);

			var projectDirectory = Path.Combine(directory, project.ProjectId);
			if (!Directory.Exists(projectDirectory))
				Directory.CreateDirectory(projectDirectory);

			try
			{
				var serialized = SerializationHelper.Serialize<Project>(project);
				XDocument.Parse(serialized).Save(Path.Combine(projectDirectory, DateTime.Now.ToString("yyyy_MM_dd HH_mm") + ".xml"));
			}
			catch (Exception e)
			{
				var xDoc = new XDocument(new XElement("Root", e.ToString()));
				xDoc.Save(Path.Combine(projectDirectory, DateTime.Now.ToString("yyyy_MM_dd HH_mm") + ".xml"));
			}
		}
    }

    public static class ExceptionHelper
    {
        /// <summary>
        /// Builds string with exception message(s) and string with exception details
        /// that include stack trace. Processes inner exceptions as well.
        /// </summary>
        /// <param name="exc"></param>
        /// <param name="message"></param>
        /// <param name="details"></param>
        public static void CollectDetails(Exception exc, out string message, out string details)
        {
            StringBuilder sbMessage = new StringBuilder();
            StringBuilder sbStackTrace = new StringBuilder();

            do
            {
                sbStackTrace.AppendFormat("{0}: {1}", exc.GetType().ToString(), exc.Message).Append(Environment.NewLine);
                sbStackTrace.Append(exc.StackTrace);
               
                // next inner exception
                exc = exc.InnerException;
            }
            while (exc != null);

            message = sbMessage.ToString();
            details = sbStackTrace.ToString();
        }
    }
}
