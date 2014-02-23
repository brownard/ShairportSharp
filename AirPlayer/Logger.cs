using log4net;
using log4net.Appender;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using MediaPortal.GUI.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AirPlayer
{
    class Logger : ShairportSharp.ILog
    {
        static Logger instance;
        public static Logger Instance
        {
            get
            {
                if (instance == null)
                    instance = new Logger();
                return instance;
            }
        }

        log4net.Core.Level minLevel = log4net.Core.Level.All;
		log4net.ILog logger;

        private Logger() 
		{
			using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.MPSettings())
			{
				var MPminLevel = (MediaPortal.Services.Level)Enum.Parse(typeof(MediaPortal.Services.Level), xmlreader.GetValueAsString("general", "loglevel", "2"));
				switch(MPminLevel)
				{
					case MediaPortal.Services.Level.Information: minLevel = log4net.Core.Level.Info; break;
					case MediaPortal.Services.Level.Warning: minLevel = log4net.Core.Level.Warn; break;
					case MediaPortal.Services.Level.Error: minLevel = log4net.Core.Level.Error; break;
				}
			}

			Hierarchy hierarchy = (Hierarchy)LogManager.CreateRepository("AirPlay");
			PatternLayout patternLayout = new PatternLayout();
			patternLayout.ConversionPattern = "[%date{MM-dd HH:mm:ss,fff}] [%-12thread] [%-5level] %message%newline";
			patternLayout.ActivateOptions();

			RollingFileAppender roller = new RollingFileAppender();
			roller.Encoding = System.Text.UTF8Encoding.UTF8;
			roller.Layout = patternLayout;
			roller.LockingModel = new FileAppender.MinimalLock();
			roller.AppendToFile = true;
			roller.RollingStyle = RollingFileAppender.RollingMode.Once;
			roller.PreserveLogFileNameExtension = true;
			roller.MaxSizeRollBackups = 1;
			roller.MaximumFileSize = "10MB";
			roller.StaticLogFileName = true;
			roller.File = MediaPortal.Configuration.Config.GetFile(MediaPortal.Configuration.Config.Dir.Log, "AirPlay.log");
			roller.ActivateOptions();
			hierarchy.Root.AddAppender(roller);

			hierarchy.Root.Level = minLevel;
			hierarchy.Configured = true;

            logger = log4net.LogManager.GetLogger("AirPlay", "AirPlay");
		}	

        public void Info(string format, params object[] args)
        {
            logger.Info(getString(format, args));
        }
        
        public void Debug(string format, params object[] args)
        {
            logger.Debug(getString(format, args));
        }

        public void Warn(string format, params object[] args)
        {
            logger.Warn(getString(format, args));
        }

        public void Error(string format, params object[] args)
        {
            logger.Error(getString(format, args));
        }

        public void Error(string message, Exception ex)
        {
            logger.Error(string.Format("{0} {1}\r\n{2}", message, ex.Message, ex.StackTrace));
        }

        string getString(string format, params object[] args)
        {
            if (args == null || args.Length < 1)
                return format;
            return string.Format(format, args);
        }
    }
}
