﻿//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010-2013 Garrett Serack and CoApp Contributors. 
//     Contributors can be discovered using the 'git log' command.
//     All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace ClrPlus.Powershell.Azure.Provider {
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Management.Automation;
    using System.Management.Automation.Provider;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using ClrPlus.Core.Extensions;
    using Platform;
    using Powershell.Provider.Base;

    [CmdletProvider("Azure", ProviderCapabilities.Credentials | ProviderCapabilities.Filter)]
    public class AzureProvider : UniversalProvider<AzureProviderInfo> {

        public static readonly Regex AccountFromHostPortRegex = new Regex(@"(?<account>\w+).blob.core.windows.net(?::\d+)?");
        static AzureProvider() {
            try {
                var file = Assembly.GetExecutingAssembly().ExtractFileResourceToPath("Azure.format.ps1xml", Path.Combine(FilesystemExtensions.TempPath, "Azure.format.ps1xml"));
                // var file = Assembly.GetExecutingAssembly().ExtractFileResourceToTemp("Azure.format.ps1xml");
                new SessionState().InvokeCommand.InvokeScript("Update-FormatData -PrependPath '{0}'".format(file));
            } catch (Exception) {
             //   Console.WriteLine("{0}/{1}/{2}",e.GetType().Name, e.Message, e.StackTrace);
            }
        }

        /// <summary>
        ///     Gives the provider the opportunity to initialize itself.
        /// </summary>
        /// <param name="providerInfo"> The information about the provider that is being started. </param>
        /// <returns> Either the providerInfo that was passed or a derived class of ProviderInfo that was initialized with the provider information that was passed. </returns>
        /// <remarks>
        ///     The default implementation returns the ProviderInfo instance that
        ///     was passed.To have session state maintain persisted data on behalf
        ///     of the provider, the provider should derive from
        ///     <see cref="System.Management.Automation.ProviderInfo" />
        ///     and add any properties or methods for the data it wishes to persist.
        ///     When Start gets called the provider should construct an instance of
        ///     its derived ProviderInfo using the providerInfo that is passed in
        ///     and return that new instance.
        /// </remarks>
        protected override ProviderInfo Start(ProviderInfo providerInfo) {
            return new AzureProviderInfo(providerInfo);
        }

        // Start

        /// <summary>
        ///     Gives the provider an opportunity to validate the drive that is
        ///     being added.  It also allows the provider to modify parts of the
        ///     PSDriveInfo object.  This may be done for performance or
        ///     reliability reasons or to provide extra data to all calls using
        ///     the Drive.
        /// </summary>
        /// <param name="drive"> The proposed new drive. </param>
        /// <returns>
        ///     The new drive that is to be added to the Windows PowerShell namespace. This can either be the same
        ///     <paramref
        ///         name="drive" />
        ///     object that was passed in or a modified version of it. The default implementation returns the drive that was passed.
        /// </returns>
        /// <remarks>
        ///     This method gives the provider an opportunity to associate
        ///     provider specific information with a drive. This is done by
        ///     deriving a new class from
        ///     <see cref="System.Management.Automation.PSDriveInfo" />
        ///     and adding any properties, methods, or fields that are necessary.
        ///     When this method gets called, the override should create an instance
        ///     of the derived PSDriveInfo using the passed in PSDriveInfo. The derived
        ///     PSDriveInfo should then be returned. Each subsequent call into the provider
        ///     that uses this drive will have access to the derived PSDriveInfo via the
        ///     PSDriveInfo property provided by the base class. Implementers of this
        ///     method should verify that the root exists and that a connection to
        ///     the data store (if there is one) can be made.  Any failures should
        ///     be sent to the
        ///     <see cref="System.Management.Automation.Provider.CmdletProvider.WriteError(ErrorRecord)" />
        ///     method and null should be returned.
        /// </remarks>
        protected override PSDriveInfo NewDrive(PSDriveInfo drive) {
            if (drive is AzureDriveInfo) {
                return drive;
            }
            return new AzureDriveInfo(drive);
        }


        // NewDrive

        /// <summary>
        ///     Allows the provider to map drives after initialization.
        /// </summary>
        /// <returns>
        ///     A drive collection with the drives that the provider wants to be added to the session upon initialization. The default implementation returns an empty
        ///     <see
        ///         cref="System.Management.Automation.PSDriveInfo" />
        ///     collection.
        /// </returns>
        /// <remarks>
        ///     After the Start method is called on a provider, the
        ///     InitializeDefaultDrives method is called. This is an opportunity
        ///     for the provider to mount drives that are important to it. For
        ///     instance, the Active Directory provider might mount a drive for
        ///     the defaultNamingContext if the machine is joined to a domain.
        ///     All providers should mount a root drive to help the user with
        ///     discoverability. This root drive might contain a listing of a set
        ///     of locations that would be interesting as roots for other mounted
        ///     drives. For instance, the Active Directory provider may create a
        ///     drive that lists the naming contexts found in the namingContext
        ///     attributes on the RootDSE. This will help users discover
        ///     interesting mount points for other drives.
        /// </remarks>
        protected override Collection<PSDriveInfo> InitializeDefaultDrives() {
            var rootDrive = new AzureDriveInfo(AzureDriveInfo.ProviderScheme, ProviderInfo, string.Empty, "Azure namespace", null);

            UniversalProviderInfo.AddingDrives.Add(rootDrive);

            foreach (var alias in UniversalProviderInfo.Aliases) {
                UniversalProviderInfo.AddingDrives.Add(new AzureDriveInfo(alias, ProviderInfo));
            }

            return UniversalProviderInfo.AddingDrives;
        }

        


        // InitializeDefaultDrives



        public static string GetAccountFromHostAndPort(string hostAndPort) {
            var match = AccountFromHostPortRegex.Match(hostAndPort);
            if (match == Match.Empty)
                return null;
            return match.Groups["account"].Value;
        }
    }
}