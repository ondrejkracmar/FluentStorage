namespace FluentStorage.Blobs {

	/// <summary>
	/// Controls recursion mode
	/// </summary>
	public enum RecursionMode {
		/// <summary>
		/// Recurse locally - for each folder on the remote datastore, iterate and query in a separate task
		/// </summary>
		Local = 1,

		/// <summary>
		/// Recurse remotely - let the remote datastore return the entire folder tree
		/// </summary>
		Remote = 2
	}

}
