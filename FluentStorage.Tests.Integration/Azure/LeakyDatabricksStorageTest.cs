﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Databricks.Client;
using FluentStorage.Blobs;
using FluentStorage.Databricks;
using Xunit;

namespace FluentStorage.Tests.Integration.Azure {
#if DEBUG
	[Trait("Category", "Blobs")]
	public class LeakyDatabricksStorageTest {
		private readonly IDatabricksStorage _storage;

		public LeakyDatabricksStorageTest() {
			ITestSettings settings = Settings.Instance;
			_storage = (IDatabricksStorage)StorageFactory.Blobs.Databricks(settings.DatabricksBaseUri, settings.DatabricksToken);
		}

		[Fact]
		public async Task List_root() {
			IReadOnlyCollection<IBlob> roots = await _storage.ListAsync();

			Assert.Equal(2, roots.Count); // dbfs + notebooks
		}

		[Fact]
		public async Task List_dbfs_root() {
			IReadOnlyCollection<IBlob> roots = await _storage.ListAsync("/dbfs");

			Assert.True(roots.Count > 0);
		}

		[Fact]
		public async Task List_notebooks_root() {
			IReadOnlyCollection<IBlob> roots = await _storage.ListAsync("/workspace");

			Assert.True(roots.Count > 0);
		}

		[Fact]
		public async Task Export_notebook() {
			IReadOnlyCollection<IBlob> roots = await _storage.ListAsync("/workspace", recurse: true);
			IBlob notebook = roots.FirstOrDefault(b => b.TryGetProperty("ObjectType", out ObjectType? ot) && ot == ObjectType.NOTEBOOK);
			Assert.NotNull(notebook);

			string defaultSource = await _storage.ReadTextAsync(notebook.FullPath);
			string sourceSource = await _storage.ReadTextAsync(notebook + "#source");
			string jupyterSource = await _storage.ReadTextAsync(notebook + "#jupyter");
			string htmlSource = await _storage.ReadTextAsync(notebook + "#html");
			string dbcSource = await _storage.ReadTextAsync(notebook + "#dbc");

		}

		[Fact]
		public async Task Import_notebook() {
			await _storage.WriteTextAsync("/workspace/integration/one/mine.scala", $"import sys # generated {DateTime.Now}");
		}

		[Fact]
		public async Task List_secret_scopes() {
			await _storage.CreateSecretsScope("ivan");

			IReadOnlyCollection<IBlob> scopes = await _storage.ListAsync("/secrets");

			Assert.True(scopes.Count > 0);
			Assert.Contains("ivan", scopes.Select(s => s.Name));
		}

		[Fact]
		public async Task List_secrets() {
			await _storage.CreateSecretsScope("ivan");
			await _storage.WriteTextAsync("/secrets/ivan/one", "dfadfd");

			IReadOnlyCollection<IBlob> secrets = await _storage.ListAsync("/secrets/ivan");
			Assert.True(secrets.Count > 0);
		}

		[Fact]
		public async Task Put_secret() {
			string value = Guid.NewGuid().ToString();

			await _storage.WriteTextAsync("secrets/ivan/tag", value);

			string value1 = await _storage.ReadTextAsync("secrets/ivan/tag");

			Assert.Equal("[REDACTED]", value1);
		}

		[Fact]
		public async Task List_jobs() {
			IReadOnlyCollection<IBlob> jobs = await _storage.ListAsync("/jobs");

			Assert.True(jobs.Count > 0);
		}

		[Fact]
		public async Task List_job_runs() {
			IBlob job = (await _storage.ListAsync("/jobs")).First();

			IReadOnlyCollection<IBlob> runs = await _storage.ListAsync(job.FullPath);

			Assert.True(runs.Count > 0);
		}

		[Fact]
		public async Task Download_job_run_output() {
			IBlob job = (await _storage.ListAsync("/jobs")).First();
			IBlob run = (await _storage.ListAsync(job.FullPath)).Last();

			string output = await _storage.ReadTextAsync($"/jobs/{job.Name}/{run.Name}");
		}
	}
#endif
}
