# Release Notes

#### FluentStorage.AWS 5.3.1
 - Fix: Incorrect extension method `MinIO` in AWS Storage Factory

#### FluentStorage 5.4.3
 - Support for DigitalOceanSpaces, MinIO, Wasabi

#### FluentStorage.AWS 5.3.0
 - New: `StorageFactory.Blobs.DigitalOceanSpaces` API for connecting to DigitalOcean Spaces
 - New: `StorageFactory.Blobs.MinIO ` API for connecting to MinIO storage servers
 - New: `StorageFactory.Blobs.Wasabi` API for connecting to Wasabi storage
 - New: Adds support for `serviceUrl` parameter in the `Aws.S3` connection string
 - New: Check to ensure `region` and `serviceUrl` are not specified together

#### FluentStorage.GCP 5.2.2

- Fix: `DeleteAsync` will delete all files in bucket if target object is not found
- GCP packages have been updated to their latest versions

#### Dependency Updates May 2024

The following packages have had their dependencies updated. No other changes were done.

- FluentStorage.Azure.Blobs 5.2.3
- FluentStorage.Azure.DataLake 5.2.2
- FluentStorage.Azure.EventHub 5.2.2
- FluentStorage.Azure.Files 5.2.2
- FluentStorage.Azure.KeyVault 5.2.2
- FluentStorage.Azure.Queues 5.2.2
- FluentStorage.Azure.ServiceBus 6.0.1
- FluentStorage.Azure.ServiceFabric 5.2.2
- FluentStorage.Databricks 5.2.2
- FluentStorage.FTP 5.3.1
- FluentStorage.SFTP 5.3.1

#### FluentStorage 5.4.2
(thanks timbze)
 - Fix: Disk blob storage `WriteAsync` uses `CopyToAsync` rather than sync

#### FluentStorage.FTP 5.3.0
(thanks beeradmoore)
 - New: `ListAsync` now supports recursion for listing directories using  `FtpListOption.Recursive`

#### FluentStorage.SFTP 5.3.0
(thanks beeradmoore)
 - New: `ListAsync` now supports recursion for listing directories using manual async recursion
 - Fix: `WriteAsync` now sets the length of destination stream manually to avoid resulting data being left

#### FluentStorage.Azure.ServiceBus 6.0.0
(thanks GiampaoloGabba)
 - Fix: Completely rewrite package for the new SDK `Azure.Messaging.ServiceBus`
 - New: New API for construction `AzureServiceBusTopicReceiver` and `AzureServiceBusQueueReceiver`
 - New: `IMessenger` interface uses structured string format for referencing queues/topics
 - New: `IAzureServiceBusMessenger` interface with API to send/create/delete/count a queue/topic/subscription

#### FluentStorage 5.4.1
 - Fix: Remove unused dependency package `Newtonsoft.Json` from main project

#### FluentStorage 5.4.0
(thanks dammitjanet)
 - New: Constructor for `SymmetricEncryptionSink` and `AesSymmetricEncryptionSink` to pass IV and key 
 - New: Constructor for `EncryptedSink` abstract base class to pass in IV and key
 - New: Additional tests for encryption/decryption repeatability when the decrpytion IV is known
 - Fix: Resolved Xunit errors and issue with with Xunit `FileData` attribute only finding a single file per test
 - Fix: Package updates and consolidation to latest Xunit

#### FluentStorage 5.3.0
(thanks dammitjanet)
 - New: Addition of `AesSymmetricEncryptionSink` and `WithAesSymmetricEncryption` extension
 - Fix: Obsolesence of `SymmetricEncryptionSink` and `WithSymmetricEncryption` extension
 - New: Updated Tests for `AesSymmetricEncryptionSink`
 - New: Additional Blob/Stream file tests and XUnit `FileDataAttribute` to support tests

#### FluentStorage.AWS 5.2.2
 - Fix: `AwsS3BlobStorage` checks if a bucket exists before trying to create one (thanks AntMaster7)

#### FluentStorage.Azure.Blobs 5.2.2
 - Fix: Upgrade `System.Text.Json` package from v4 to v7

#### FluentStorage 5.2.2
 - Fix: Upgrade `System.Text.Json` package from v4 to v7
 - Fix: Local storage: Handling of `LastModificationTime` and `CreatedTime`
 - Fix: Local storage: `LastAccessTimeUtc` is saved as a Universal sortable string in the Blob properties

#### FluentStorage.SFTP 5.2.3
 - Fix: Various fixes to `ListAsync` path handling
 - Fix: Upgrade `SSH.NET` package from v2016 to v2020

#### FluentStorage.SFTP 5.2.2
 - New: Added support for a root path in the SFTP connection string
 - Fix: `GetBlobsAsync` should return an array with a single null if the file does not exist
 - Fix: `WriteAsync` will create the directory if it does not exist

#### FluentStorage 5.2.1
 - New: Implement `LocalDiskMessenger.StartProcessorAsync`
 - Package: Add Nuget reference to `Newtonsoft.Json 13.0.3`
 - Package: Remove Nuget reference to `Newtonsoft.Json 12.x.x`
 - Package: Remove Nuget reference to `NetBox` and add the required utilities within this library

#### FluentStorage.AWS 5.2.1
 - New: Implement server-side filtering in `AwsS3DirectoryBrowser.ListAsync` by supplying a `FilePrefix` (thanks SRJames)

#### FluentStorage.FTP 5.2.1
 - Fix: Support for the append parameter in FluentFtpBlobStorage (thanks candoumbe)
 - Fix: `IBlobStorage.WriteAsync` will create the directory hierarchy if required (thanks candoumbe)

#### FluentStorage 5.1.1
 - Fix: Implementation of `LocalDiskMessenger.StartProcessorAsync` (issue [#14](https://github.com/robinrodricks/FluentStorage/issues/14))(`netstandard2.1` / `net6.0` and above

#### FluentStorage 5.0.0
 - New: Introducing the FluentStorage set of libraries created from Storage.NET
 - New: Added SFTP provider using [SSH.NET](https://github.com/sshnet/SSH.NET)
 - Fix: FTP provider [FluentFTP](https://github.com/robinrodricks/FluentFTP) updated to v44
 - Fix: AWS Nugets bumped to latest versions as of Jan 2023
 - Fix: All nuget packages now target `netstandard2.0`,`netstandard2.1`,`net50`,`net60`
 - Change: Refactored package structure to simplify naming scheme
 - Change: Refactored entire codebase to simplify code organization
 - New: Documentation wiki created with one page per provider