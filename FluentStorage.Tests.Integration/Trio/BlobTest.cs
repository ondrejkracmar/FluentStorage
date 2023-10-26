﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentStorage.Blobs;
using FluentStorage.Tests.Integration.Util;
using Xunit;
using FluentStorage.Utils.Extensions;
using FluentStorage.Utils.Generator;

namespace FluentStorage.Tests.Integration.Blobs {
	[Trait("Category", "Blobs")]
	public abstract class BlobTest : IAsyncLifetime {
		private readonly IBlobStorage _storage;
		private readonly string _blobPrefix;
		private readonly BlobFixture _fixture;

		public BlobTest(BlobFixture fixture) {
			_storage = fixture.Storage;
			_blobPrefix = fixture.BlobPrefix;
			_fixture = fixture;
		}

		public Task InitializeAsync() {
			return _fixture.InitAsync();
		}

		public Task DisposeAsync() {
			return _fixture.DisposeAsync();
		}

		private async Task<string> GetRandomStreamIdAsync(string prefix = null) {
			string id = RandomBlobPath();
			if (prefix != null)
				id = prefix + "/" + id;

			using (Stream s = "kjhlkhlkhlkhlkh".ToMemoryStream()) {
				await _storage.WriteAsync(id, s);
			}

			return id;
		}

		[Fact]
		public async Task List_All_DoesntCrash() {
			await _storage.ListAsync();
		}

		[Fact]
		public async Task List_RootFolder_HasAtLeastOne() {
			string targetId = RandomBlobPath();

			await _storage.WriteTextAsync(targetId, "test");

			IReadOnlyCollection<IBlob> rootContent = await _storage.ListAsync();

			Assert.NotEmpty(rootContent);
		}

		[Fact]
		public async Task List_ByFilePrefix_Filtered() {
			string prefix = RandomGenerator.RandomString;

			int countBefore = (await _storage.ListAsync(new ListOptions { FolderPath = _blobPrefix, FilePrefix = prefix })).Count;

			string id1 = RandomBlobPath(prefix);
			string id2 = RandomBlobPath(prefix);
			string id3 = RandomBlobPath();

			await _storage.WriteTextAsync(id1, RandomGenerator.RandomString);
			await _storage.WriteTextAsync(id2, RandomGenerator.RandomString);
			await _storage.WriteTextAsync(id3, RandomGenerator.RandomString);

			IReadOnlyCollection<IBlob> items = (await _storage.ListAsync(new ListOptions { FolderPath = _blobPrefix, FilePrefix = prefix }));
			Assert.Equal(2 + countBefore, items.Count); //2 files + containing folder
		}

		[Fact]
		public async Task List_FilesInFolder_NonRecursive() {
			string id = RandomBlobPath();

			await _storage.WriteTextAsync(id, RandomGenerator.RandomString);

			List<IBlob> items = (await _storage.ListAsync(new ListOptions { FolderPath = _blobPrefix, Recurse = false })).ToList();

			Assert.True(items.Count > 0);

			IBlob tid = items.FirstOrDefault(i => i.FullPath == id);
			Assert.NotNull(tid);
		}

		[Fact]
		public async Task List_FilesInFolder_Recursive() {
			string folderPath = RandomBlobPath();
			string id1 = StoragePath.Combine(folderPath, "1.txt");
			string id2 = StoragePath.Combine(folderPath, "sub", "2.txt");
			string id3 = StoragePath.Combine(folderPath, "sub", "3.txt");

			try {
				await _storage.WriteTextAsync(id1, RandomGenerator.RandomString);
				await _storage.WriteTextAsync(id2, RandomGenerator.RandomString);
				await _storage.WriteTextAsync(id3, RandomGenerator.RandomString);

				IReadOnlyCollection<IBlob> items = await _storage.ListAsync(recurse: true, folderPath: folderPath);
				Assert.Equal(4, items.Count); //1.txt + sub (folder) + 2.txt + 3.txt

			}
			catch (NotSupportedException) {
				//it ok for providers not to support hierarchy
			}
		}

		[Fact]
		public async Task List_InNonExistingFolder_EmptyCollection() {
			IEnumerable<IBlob> objects = await _storage.ListAsync(new ListOptions { FolderPath = RandomBlobPath() });

			Assert.NotNull(objects);
			Assert.True(objects.Count() == 0);
		}

		[Fact]
		public async Task List_FilesInNonExistingFolder_EmptyCollection() {
			IEnumerable<IBlob> objects = await _storage.ListFilesAsync(new ListOptions { FolderPath = RandomBlobPath() });

			Assert.NotNull(objects);
			Assert.True(objects.Count() == 0);
		}

		[Fact]
		public async Task List_VeryLongPrefix_NoResultsNoCrash() {
			await Assert.ThrowsAsync<ArgumentException>(async () => await _storage.ListAsync(new ListOptions { FilePrefix = RandomGenerator.GetRandomString(100000, false) }));
		}

		[Fact]
		public async Task List_limited_number_of_results() {
			string prefix = RandomGenerator.RandomString;
			string id1 = RandomBlobPath(prefix);
			string id2 = RandomBlobPath(prefix);
			await _storage.WriteTextAsync(id1, RandomGenerator.RandomString);
			await _storage.WriteTextAsync(id2, RandomGenerator.RandomString);

			int countAll = (await _storage.ListFilesAsync(new ListOptions { FolderPath = _blobPrefix, FilePrefix = prefix })).Count;
			int countOne = (await _storage.ListAsync(new ListOptions { FolderPath = _blobPrefix, FilePrefix = prefix, MaxResults = 1 })).Count;

			Assert.Equal(2, countAll);
			Assert.Equal(1, countOne);
		}

		[Fact]
		public async Task List_with_browsefilter_calls_filter() {
			string id1 = RandomBlobPath();
			string id2 = RandomBlobPath();
			await _storage.WriteTextAsync(id1, RandomGenerator.RandomString);
			await _storage.WriteTextAsync(id2, RandomGenerator.RandomString);

			//dump compare
			IReadOnlyCollection<IBlob> files = await _storage.ListFilesAsync(new ListOptions {
				FolderPath = _blobPrefix,
				Recurse = true
			});
			Assert.Contains(files, f => f.FullPath == id1 && f.Kind == BlobItemKind.File);

			//server-side filtering
			files = await _storage.ListFilesAsync(new ListOptions {
				FolderPath = _blobPrefix,
				Recurse = true,
				BrowseFilter = id => (id.Kind != BlobItemKind.File || id.FullPath == id1)
			});


			Assert.Single(files);
			Assert.Equal(id1, files.First().FullPath);
		}

		//[Fact]
		public async Task List_large_number_of_results() {
			const int count = 500;
			//arrange

			//something like FTP doesn't support multiple connections, however this should be implemented in FTP provider itself

			for (int it = 0; it < 50; it++) {
				await Task.WhenAll(Enumerable.Range(0, 10).Select(i => _storage.WriteTextAsync(RandomBlobPath(), "123")));
			}

			//act
			IReadOnlyCollection<IBlob> blobs = await _storage.ListAsync(folderPath: _blobPrefix);

			//assert
			Assert.True(blobs.Count >= count, $"expected over {count}, but received only {blobs.Count}");
		}

		[Fact]
		public async Task List_folder_nonrecursively_no_children() {
			try {
				string sub = RandomBlobPath() + "/";

				await _storage.WriteTextAsync(sub + "one.txt", "test");
				await _storage.WriteTextAsync(sub + "sub/two.txt", "test");

				IReadOnlyCollection<IBlob> subItems = await _storage.ListAsync(recurse: false, folderPath: sub);
				Assert.Equal(2, subItems.Count);


				Assert.Contains(new Blob(sub + "one.txt"), subItems);
				Assert.Contains(new Blob(sub + "sub", BlobItemKind.Folder), subItems);
			}
			catch (NotSupportedException) {
				//hierarchy not supported
			}
		}

		[Fact]
		public async Task GetBlob_for_one_file_succeeds() {
			string content = RandomGenerator.GetRandomString(1000, false);
			string id = RandomBlobPath();

			await _storage.WriteTextAsync(id, content);

			IBlob meta = await _storage.GetBlobAsync(id);

			long size = Encoding.UTF8.GetBytes(content).Length;
			string md5 = content.MD5();

			if (meta.Size != null)
				Assert.Equal(size, meta.Size);
			if (meta.MD5 != null)
				Assert.Equal(md5, meta.MD5);
			if (meta.LastModificationTime != null)
				Assert.Equal(DateTime.UtcNow.RoundToDay(), meta.LastModificationTime.Value.DateTime.RoundToDay());
		}

		[Fact]
		public async Task GetBlob_doesnt_exist_returns_null() {
			string id = RandomBlobPath();

			IBlob meta = (await _storage.GetBlobsAsync(new[] { id })).First();

			Assert.Null(meta);
		}

		[Fact]
		public async Task GetBlob_Root_doesnt_exist_returns_null() {
			string id = "/" + Guid.NewGuid().ToString();
			//string id = "test";

			Assert.Null(await _storage.GetBlobAsync(id));
		}

		[Fact]
		public async Task GetBlob_Root_valid_returns_some() {
			string id = RandomBlobPath();

			string root = StoragePath.Split(id)[0];

			try {
				IBlob rb = await _storage.GetBlobAsync(root);
			}
			catch (NotSupportedException) {

			}
		}


		[Fact]
		public async Task Open_doesnt_exist_returns_null() {
			string id = RandomBlobPath();

			Assert.Null(await _storage.OpenReadAsync(id));
		}

		[Fact]
		public async Task Open_copy_to_memory_stream_succeeds() {
			string id = await GetRandomStreamIdAsync();
			IBlobStorage ms = StorageFactory.Blobs.InMemory();

			//if this doesn't crash it means the returned stream is compatible with usual .net streaming
			await _storage.CopyToAsync(id, ms, id);
		}

		[Fact]
		public async Task Write_with_writeasync_succeeds() {
			string id = RandomBlobPath();
			byte[] data = Encoding.UTF8.GetBytes("oh my");

			await _storage.WriteAsync(id, new MemoryStream(data));

			//read and check
			string result = await _storage.ReadTextAsync(id);
			Assert.Equal("oh my", result);
		}

		[Fact]
		public async Task Write_nullDataStream_argumentnullexception() {
			await Assert.ThrowsAsync<ArgumentNullException>(() => _storage.WriteAsync(RandomBlobPath(), (Stream)null, false));
		}

		[Fact]
		public async Task Write_non_seekable_stream_succeeds() {
			string s = "test content";
			string id = RandomBlobPath();

			var nonSeekable = new NonSeekableStream(new MemoryStream(Encoding.UTF8.GetBytes(s)));

			await _storage.WriteAsync(id, nonSeekable);

			Assert.Equal(s, await _storage.ReadTextAsync(id));
		}

		[Fact]
		public async Task Exists_non_existing_blob_returns_false() {
			Assert.False(await _storage.ExistsAsync(RandomBlobPath()));
		}

		[Fact]
		public async Task Exists_existing_blob_returns_true() {
			string id = RandomBlobPath();
			await _storage.WriteTextAsync(id, "test");

			Assert.True(await _storage.ExistsAsync(id));
		}

		[Fact]
		public async Task Delete_create_and_delete_doesnt_exist() {
			string path = RandomBlobPath();
			await _storage.WriteTextAsync(path, "test");
			await _storage.DeleteAsync(path);

			Assert.False(await _storage.ExistsAsync(path));
		}

		[Fact]
		public async Task Delete_non_existing_file_ignores() {
			string path = RandomBlobPath();
			await _storage.DeleteAsync(path);
		}

		[Fact]
		public async Task Delete_folder_removes_everything() {
			//setup
			string prefix = RandomBlobPath();
			string file1 = StoragePath.Combine(prefix, "1.txt");
			string file2 = StoragePath.Combine(prefix, "sub", "2.txt");


			try {
				//setup
				await _storage.WriteTextAsync(file1, "1");
				await _storage.WriteTextAsync(file2, "2");

				//act
				await _storage.DeleteAsync(prefix);
			}
			catch (NotSupportedException) {

			}

			//assert
			IReadOnlyCollection<IBlob> files = await _storage.ListAsync(prefix, recurse: true);
			Assert.True(files.Count == 0);
		}

		[Fact]
		public async Task Rename_File_Renames() {
			string prefix = RandomBlobPath();
			string file = StoragePath.Combine(prefix, "1");

			try {
				await _storage.WriteTextAsync(file, "test");
				await _storage.RenameAsync(file, StoragePath.Combine(prefix, "2"));
				IReadOnlyCollection<IBlob> list = await _storage.ListAsync(prefix);

				Assert.Single(list);
				Assert.True(list.First().Name == "2");
			}
			catch (NotSupportedException) {

			}
		}

		[Fact]
		public async Task Rename_OldPathNull_ThowsArgumentNull() {
			await Assert.ThrowsAsync<ArgumentNullException>(() => _storage.RenameAsync(null, "test/1"));
		}

		[Fact]
		public async Task Rename_NewPathNull_ThowsArgumentNull() {
			await Assert.ThrowsAsync<ArgumentNullException>(() => _storage.RenameAsync("test/1", null));
		}


		[Fact]
		public async Task Rename_Folder_Renames() {
			string prefix = RandomBlobPath();
			string file1 = StoragePath.Combine(prefix, "old", "1.txt");
			string file11 = StoragePath.Combine(prefix, "old", "1", "1.txt");
			string file111 = StoragePath.Combine(prefix, "old", "1", "1", "1.txt");

			try {
				await _storage.WriteTextAsync(file1, string.Empty);
			}
			catch (NotSupportedException) {
				return;
			}

			await _storage.WriteTextAsync(file11, string.Empty);
			await _storage.WriteTextAsync(file111, string.Empty);

			await _storage.RenameAsync(StoragePath.Combine(prefix, "old"), StoragePath.Combine(prefix, "new"));

			IReadOnlyCollection<IBlob> list = await _storage.ListAsync(prefix);
		}

		[Fact]
		public async Task Read_larger_file() {
			string text = RandomGenerator.GetRandomString(1024 * 1024, false);

			try {
				await _storage.WriteTextAsync("test/test", text);

				string text2 = await _storage.ReadTextAsync("test/test");

				Assert.Equal(text, text2);
			}
			catch (NotSupportedException) {

			}
		}

		[Fact]
		public async Task UserMetadata_write_readsback() {
			var blob = new Blob(RandomBlobPath());
			blob.Metadata["user"] = "ivan";
			blob.Metadata["fun"] = "no";

			await _storage.WriteTextAsync(blob, "test");
			IBlob blob2 = await _storage.GetBlobAsync(blob);

			try {
				await _storage.SetBlobAsync(blob);
				blob2 = await _storage.GetBlobAsync(blob);
				Assert.True(blob2.Size > 0);
			}
			catch (NotSupportedException) {
				return;
			}

			//test
			blob2 = await _storage.GetBlobAsync(blob);
			Assert.NotNull(blob2.Metadata);
			Assert.Equal("ivan", blob2.Metadata["user"]);
			Assert.Equal("no", blob2.Metadata["fun"]);
			Assert.Equal(2, blob2.Metadata.Count);
		}

		[Fact]
		public async Task UserMetadata_OverwriteWithLess_RemovesOld() {
			//setup
			var blob = new Blob(RandomBlobPath());
			blob.Metadata["user"] = "ivan";
			blob.Metadata["fun"] = "no";
			await _storage.WriteTextAsync(blob, "test");
			try {
				await _storage.SetBlobAsync(blob);
			}
			catch (NotSupportedException) {
				return;
			}
			blob.Metadata.Clear();
			blob.Metadata["user"] = "ivan2";
			await _storage.WriteTextAsync(blob, "test2");
			await _storage.SetBlobAsync(blob);

			//test
			IBlob blob2 = await _storage.GetBlobAsync(blob);
			Assert.NotNull(blob2.Metadata);
			Assert.Single(blob2.Metadata);
			Assert.Equal("ivan2", blob2.Metadata["user"]);
		}

		[Fact]
		public async Task UserMetadata_openwrite_readsback() {
			var blob = new Blob(RandomBlobPath());
			blob.Metadata["user"] = "ivan";
			blob.Metadata["fun"] = "no";

			await _storage.WriteAsync(blob, new MemoryStream(RandomGenerator.GetRandomBytes(10, 15)));

			try {
				await _storage.SetBlobAsync(blob);
			}
			catch (NotSupportedException) {
				return;
			}

			//test
			IBlob blob2 = await _storage.GetBlobAsync(blob);
			Assert.NotNull(blob2.Metadata);
			Assert.Equal("ivan", blob2.Metadata["user"]);
			Assert.Equal("no", blob2.Metadata["fun"]);
			Assert.Equal(2, blob2.Metadata.Count);
		}

		[Fact]
		public async Task UserMetadata_List_AlsoReturnsMetadata() {
			var blob = new Blob(RandomBlobPath());
			blob.Metadata["user"] = "ivan";
			blob.Metadata["fun"] = "no";
			await _storage.WriteTextAsync(blob, "test2");

			try {
				await _storage.SetBlobAsync(blob);
			}
			catch (NotSupportedException) {
				return;
			}

			IReadOnlyCollection<IBlob> all = await _storage.ListAsync(folderPath: blob.FolderPath, includeAttributes: true);

			//test
			IBlob blob2 = all.First(b => b.FullPath == blob.FullPath);
			Assert.NotNull(blob2.Metadata);
			Assert.Equal("ivan", blob2.Metadata["user"]);
			Assert.Equal("no", blob2.Metadata["fun"]);
			Assert.Equal(2, blob2.Metadata.Count);
		}

		[Fact]
		public async Task GetMd5HashAsync() {
			var blob = new Blob(RandomBlobPath());
			string content = RandomGenerator.RandomString;
			string hash = content.MD5();

			await _storage.WriteTextAsync(blob, content);

			string hash2 = await _storage.GetMD5HashAsync(blob);
			Assert.Equal(hash, hash2);
		}

		[Fact]
		public async Task Hierarchy_CreateFolder_Exists() {
			string folderPath = RandomBlobPath();

			try {
				await _storage.CreateFolderAsync(folderPath);

				IReadOnlyCollection<IBlob> files = await _storage.ListAsync(folderPath);
				Assert.True(files.Any());  //check dummy file exists
			}
			catch (NotSupportedException) {

			}
		}

		private string RandomBlobPath(string prefix = null, string subfolder = null, string extension = "") {
			return StoragePath.Combine(
			   _blobPrefix,
			   subfolder,
			   (prefix ?? "") + Guid.NewGuid().ToString() + extension);
		}

		class TestDocument {
			public string M { get; set; }
		}
	}
}
