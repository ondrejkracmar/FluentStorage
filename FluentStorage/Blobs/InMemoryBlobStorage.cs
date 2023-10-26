﻿using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using FluentStorage.Streaming;
using FluentStorage.Utils.Extensions;
using FluentStorage.Utils.IO;

namespace FluentStorage.Blobs {
	class InMemoryBlobStorage : IBlobStorage {
		struct Tag {
			public IBlob blob;
			public byte[] data;
		}

		private readonly Dictionary<string, Tag> _pathToTag = new Dictionary<string, Tag>();

		public Task<IReadOnlyCollection<IBlob>> ListAsync(ListOptions options, CancellationToken cancellationToken) {
			if (options == null) options = new ListOptions();

			IEnumerable<KeyValuePair<string, Tag>> query = _pathToTag;

			//limit by folder path
			if (options.Recurse) {
				if (!StoragePath.IsRootPath(options.FolderPath)) {
					string prefix = options.FolderPath + StoragePath.PathSeparatorString;

					query = query.Where(p => p.Key.StartsWith(prefix));
				}
			}
			else {
				query = query.Where(p => StoragePath.ComparePath(p.Value.blob.FolderPath, options.FolderPath));
			}

			//prefix
			query = query.Where(p => options.IsMatch(p.Value.blob));

			//browser filter
			query = query.Where(p => options.BrowseFilter == null || options.BrowseFilter(p.Value.blob));

			//limit
			if (options.MaxResults != null) {
				query = query.Take(options.MaxResults.Value);
			}

			IReadOnlyCollection<IBlob> matches = query.Select(p => p.Value.blob).ToList();

			return Task.FromResult(matches);
		}

		public Task WriteAsync(string fullPath, Stream sourceStream, bool append, CancellationToken cancellationToken) {
			GenericValidation.CheckBlobFullPath(fullPath);
			fullPath = StoragePath.Normalize(fullPath);

			if (sourceStream is null)
				throw new ArgumentNullException(nameof(sourceStream));

			if (append) {
				if (!Exists(fullPath)) {
					Write(fullPath, sourceStream);
				}
				else {
					Tag tag = _pathToTag[fullPath];
					byte[] data = tag.data.Concat(sourceStream.ToByteArray()).ToArray();
					Write(fullPath, new MemoryStream(data));
				}
			}
			else {
				Write(fullPath, sourceStream);
			}

			return Task.FromResult(true);
		}

		public Task<Stream> WriteAsync(string fullPath, bool append, CancellationToken cancellationToken) {
			GenericValidation.CheckBlobFullPath(fullPath);
			fullPath = StoragePath.Normalize(fullPath);

			var result = new FixedStream(new MemoryStream(), null, async fx => {
				MemoryStream ms = (MemoryStream)fx.Parent;
				ms.Position = 0;
				await WriteAsync(fullPath, ms, append, cancellationToken).ConfigureAwait(false);
			});

			return Task.FromResult<Stream>(result);
		}

		public Task<Stream> OpenReadAsync(string fullPath, CancellationToken cancellationToken) {
			GenericValidation.CheckBlobFullPath(fullPath);
			fullPath = StoragePath.Normalize(fullPath);

			if (!_pathToTag.TryGetValue(fullPath, out Tag tag) || tag.data == null) return Task.FromResult<Stream>(null);

			return Task.FromResult<Stream>(new NonCloseableStream(new MemoryStream(tag.data)));
		}

		public Task DeleteAsync(IEnumerable<string> fullPaths, CancellationToken cancellationToken) {
			GenericValidation.CheckBlobFullPaths(fullPaths);

			foreach (string path in fullPaths) {
				//try to delete as entry
				Blob pb = path;
				if (_pathToTag.ContainsKey(pb)) {
					_pathToTag.Remove(pb);
				}


				string prefix = StoragePath.Normalize(path) + StoragePath.PathSeparatorString;

				List<IBlob> candidates = _pathToTag.Where(p => p.Value.blob.FullPath.StartsWith(prefix)).Select(p => p.Value.blob).ToList();

				foreach (Blob candidate in candidates) {
					_pathToTag.Remove(candidate);
				}
			}
			return Task.FromResult(true);
		}

		public Task<IReadOnlyCollection<bool>> ExistsAsync(IEnumerable<string> fullPaths, CancellationToken cancellationToken) {
			var result = new List<bool>();

			foreach (string fullPath in fullPaths) {
				result.Add(_pathToTag.ContainsKey(StoragePath.Normalize(fullPath)));
			}

			return Task.FromResult<IReadOnlyCollection<bool>>(result);
		}

		public Task<IReadOnlyCollection<IBlob>> GetBlobsAsync(IEnumerable<string> fullPaths, CancellationToken cancellationToken) {
			GenericValidation.CheckBlobFullPaths(fullPaths);

			var result = new List<IBlob>();

			foreach (string fullPath in fullPaths) {
				if (!_pathToTag.TryGetValue(StoragePath.Normalize(fullPath), out Tag tag)) {
					result.Add(null);
				}
				else {
					result.Add(tag.blob);
				}
			}

			return Task.FromResult<IReadOnlyCollection<IBlob>>(result);
		}

		public Task SetBlobsAsync(IEnumerable<IBlob> blobs, CancellationToken cancellationToken = default) {
			if (blobs == null)
				return Task.FromResult(true);

			foreach (Blob blob in blobs) {
				if (_pathToTag.TryGetValue(blob, out Tag tag)) {
					tag.blob.Metadata.Clear();
					tag.blob.Metadata.AddRange(blob.Metadata);
				}
			}

			return Task.FromResult(true);
		}

		private void Write(string fullPath, Stream sourceStream) {
			GenericValidation.CheckBlobFullPath(fullPath);
			fullPath = StoragePath.Normalize(fullPath);

			if (sourceStream is MemoryStream ms)
				ms.Position = 0;
			byte[] data = sourceStream.ToByteArray();

			if (!_pathToTag.TryGetValue(fullPath, out Tag tag)) {
				tag = new Tag {
					data = data,
					blob = new Blob(fullPath) {
						Size = data.Length,
						LastModificationTime = DateTime.UtcNow,
						MD5 = data.MD5().ToHexString()
					}
				};
			}
			else {
				tag.data = data;
				tag.blob.Size = data.Length;
				tag.blob.LastModificationTime = DateTime.UtcNow;
				tag.blob.MD5 = data.MD5().ToHexString();
			}
			_pathToTag[fullPath] = tag;

			AddVirtualFolderHierarchy(tag.blob);
		}

		private void AddVirtualFolderHierarchy(IBlob fileBlob) {
			string path = fileBlob.FolderPath;

			while (!StoragePath.IsRootPath(path)) {
				var vf = new Blob(path, BlobItemKind.Folder);
				_pathToTag[path] = new Tag { blob = vf };

				path = StoragePath.GetParent(path);
			}
		}

		private bool Exists(string fullPath) {
			GenericValidation.CheckBlobFullPath(fullPath);

			return _pathToTag.ContainsKey(fullPath);
		}

		public void Dispose() {
		}

		public Task<ITransaction> OpenTransactionAsync() {
			return Task.FromResult(EmptyTransaction.Instance);
		}
	}
}
