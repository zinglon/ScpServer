﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Xml;
using log4net;
using Libarius.Filesystem;
using PropertyChanged;
using ScpControl.ScpCore;
using ScpControl.Shared.Core;

namespace ScpControl.Profiler
{
    [ImplementPropertyChanged]
    public class DualShockProfileManager : SingletonBase<DualShockProfileManager>
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly FileSystemWatcher _fswProfileFiles = new FileSystemWatcher(ProfilesPath, ProfileFileFilter);

        private DualShockProfileManager()
        {
            LoadProfiles();

            _fswProfileFiles.Changed += FswProfileFilesOnChanged;
            _fswProfileFiles.Created += FswProfileFilesOnChanged;
            _fswProfileFiles.Renamed += FswProfileFilesOnChanged;
            _fswProfileFiles.Deleted += FswProfileFilesOnChanged;
            
            _fswProfileFiles.Error += (sender, args) =>
            {
                Log.ErrorFormat("Unexpected error in Profiles FileSystemWatcher: {0}", args.GetException());
            };

            _fswProfileFiles.EnableRaisingEvents = true;
        }

        private void LoadProfiles()
        {
            lock (this)
            {
                var profiles = new List<DualShockProfile>();

                foreach (var file in Directory.GetFiles(ProfilesPath, ProfileFileFilter)
                    )
                {
                    Log.InfoFormat("Loading profile from file {0}", file);

                    var profile = Load(file);
                    profiles.Add(profile);

                    Log.InfoFormat("Successfully loaded profile {0}", profile.Name);
                }

                Profiles = profiles.AsReadOnly();
            }
        }

        private void FswProfileFilesOnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            if (fileSystemEventArgs.ChangeType != WatcherChangeTypes.Deleted)
            {
                // file might still be written to, just wait until it's handles are closed
                while (FilesystemHelper.IsFileLocked(new FileInfo(fileSystemEventArgs.FullPath)))
                {
                    Thread.Sleep(100);
                }
            }

            LoadProfiles();
        }

        public static string ProfileFileFilter
        {
            get { return "*.xml"; }
        }

        public static string ProfilesPath
        {
            get { return Path.Combine(GlobalConfiguration.AppDirectory, "Profiles"); }
        }

        /// <summary>
        ///     Gets a list of all available profiles loaded from memory.
        /// </summary>
        public IReadOnlyList<DualShockProfile> Profiles { get; private set; }

        /// <summary>
        ///     Feeds the supplied HID report through all loaded mapping profiles.
        /// </summary>
        /// <param name="report">The extended HID report.</param>
        public void PassThroughAllProfiles(ScpHidReport report)
        {
            foreach (var profile in Profiles.Where(p => p.IsActive))
            {
                profile.Remap(report);
            }
        }

        /// <summary>
        ///     Stores a new <see cref="DualShockProfile"/> or overwrites an existing one.
        /// </summary>
        /// <param name="profile">The <see cref="DualShockProfile"/> to save.</param>
        public void SubmitProfile(DualShockProfile profile)
        {
            Save(profile, Path.Combine(GlobalConfiguration.ProfilesPath, profile.FileName));
        }

        /// <summary>
        ///     Removes a given <see cref="DualShockProfile"/>.
        /// </summary>
        /// <param name="profile">The <see cref="DualShockProfile"/>to remove.</param>
        public void RemoveProfile(DualShockProfile profile)
        {
            File.Delete(Path.Combine(GlobalConfiguration.ProfilesPath, profile.FileName));
        }

        private static DualShockProfile Load(string file)
        {
            try
            {
                Log.DebugFormat("Loading profile from file: {0}", file);

                var serializer = new DataContractSerializer(typeof (DualShockProfile));

                using (var fs = File.OpenText(file))
                {
                    using (var xml = XmlReader.Create(fs))
                    {
                        return (DualShockProfile) serializer.ReadObject(xml);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.FatalFormat("Couldn't load profile {0} due to an unexpected error: {1}", file, ex);
                return null;
            }
        }

        private static void Save(DualShockProfile profile, string file)
        {
            var serializer = new DataContractSerializer(profile.GetType());

            var path = Path.GetDirectoryName(file) ?? GlobalConfiguration.AppDirectory;

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            using (var xml = XmlWriter.Create(file, new XmlWriterSettings { Indent = true }))
            {
                serializer.WriteObject(xml, profile);
            }
        }
    }
}
