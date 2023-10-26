﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Config.Net;
using FluentStorage.Blobs;

namespace FluentStorage.Tests.Integration.Blobs {
	public abstract class BlobFixture : IDisposable {
		private static readonly ITestSettings _settings = Settings.Instance;

		private string _testDir;
		private bool _initialised;

		protected BlobFixture(string blobPrefix = null) {
			Storage = CreateStorage(_settings);
			BlobPrefix = blobPrefix;
		}

		protected abstract IBlobStorage CreateStorage(ITestSettings settings);

		public IBlobStorage Storage { get; private set; }
		public string BlobPrefix { get; }

		public string TestDir {
			get {
				if (_testDir == null) {
					string buildDir = new FileInfo(new Uri(Assembly.GetExecutingAssembly().Location).LocalPath).Directory.FullName;
					_testDir = Path.Combine(buildDir, "TEST-" + Guid.NewGuid());
				}

				return _testDir;
			}
		}

		public async Task InitAsync() {
			if (_initialised)
				return;

			//drop all blobs in test storage

			IReadOnlyCollection<IBlob> topLevel = (await Storage.ListAsync(folderPath: BlobPrefix, recurse: false)).ToList();

			try {
				await Storage.DeleteAsync(topLevel.Select(f => f.FullPath));
			}
			catch {
				//absolutely doesn't matter if it fails, this is only a perf improvement on tests
			}

			_initialised = true;
		}

		public Task DisposeAsync() {
			return Task.CompletedTask;
		}

		public void Dispose() {
			Storage.Dispose();

			if (_testDir != null) {
				try {
					Directory.Delete(_testDir, true);
				}
				catch (DirectoryNotFoundException) {
				}
				_testDir = null;
			}
		}
	}
}
