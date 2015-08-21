using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Text;


namespace MPKBridge
{
	/// <summary>
	/// Verifies if all files listed in a MD5 file verification database are OK.
	/// </summary>
	public class MD5Verifier
	{
		private string file;
		private Encoding enc;
		private string checksum;

		private struct fileEntry
		{
			public string checksum;
			public string file;
		}

		public delegate void onVerifyProgressDelegate(string file, MD5VerifyStatus status, int verified, int success, int corrupt, int missing, int total);
		public delegate void onVerifyDoneDelegate(Exception ex);
		public event onVerifyProgressDelegate onVerifyProgress;
		public event onVerifyDoneDelegate onVerifyDone;

		public string GetCheckSum()
		{
			return checksum;
		}

		public enum MD5VerifyStatus 
		{
			None,
			Verifying,
			OK,
			Bad,
			Error,
			FileNotFound,
		}
	
		public MD5Verifier(string Filename, Encoding UseEncoding)
		{
			// set initial values
			file = Filename;
			enc = UseEncoding;
		}

		public MD5Verifier(Encoding UseEncoding)
		{
			enc = UseEncoding;
		}

		public void doVerify(string myText)
		{
			MD5CryptoServiceProvider csp = new MD5CryptoServiceProvider();
			try
			{
				
				byte[] hash = csp.ComputeHash(enc.GetBytes(myText));
				
				// convert to string
				string computed = BitConverter.ToString(hash).Replace("-", "").ToLower();

				this.checksum = computed;

			}
			catch
			{
			}


		}

		public void doVerify()
		{
			// try to open the file
			TextReader tr;
			try
			{
				tr = new StreamReader(file, enc);
			}				
			catch (Exception ex)
			{
				onVerifyDone(ex);
				return;
			}

			// read the file
			string line;
			ArrayList files = new ArrayList();
			while((line = tr.ReadLine()) != null) 
			{
				if (line.Length >= 35) 
				{
					fileEntry entry;
					entry.checksum = line.Substring(0, 32);
					entry.file = line.Substring(34);
					files.Add(entry);
				}
			}
			
			// close it
			tr.Close();

			// try to access directory
			try 
			{
				Environment.CurrentDirectory = new FileInfo(file).Directory.FullName;
			}
			catch (Exception ex)
			{
				onVerifyDone(ex);
				return;
			}

			// display progress
			int ver = 0;
			int success = 0;
			int corrupt = 0;
			int missing = 0;
			onVerifyProgress("", MD5VerifyStatus.None, ver, success, corrupt, missing, files.Count);

			// check for empty (maybe invalid) files
			if (files.Count < 1) 
			{
				onVerifyDone(null);
				return;
			}

			MD5CryptoServiceProvider csp = new MD5CryptoServiceProvider();
			
			for (int idx = 0; (idx < files.Count); ++idx) 
			{
				// get file
				fileEntry entry = (fileEntry) files[idx];
				
				// display file name
				onVerifyProgress(entry.file, MD5VerifyStatus.Verifying, ver, success, corrupt, missing, files.Count);

				if (File.Exists(entry.file)) 
				{
					try
					{
						// compute hash
						FileStream stmcheck = File.OpenRead(entry.file);
						byte[] hash = csp.ComputeHash(stmcheck);
						stmcheck.Close();

						// convert to string
						string computed = BitConverter.ToString(hash).Replace("-", "").ToLower();

						// compare
						if (computed == entry.checksum) 
						{
							++ver;
							++success;
							onVerifyProgress(entry.file, MD5VerifyStatus.OK, ver, success, corrupt, missing, files.Count);
						}
						else
						{
							++corrupt;
							++success;
							onVerifyProgress(entry.file, MD5VerifyStatus.Bad, ver, success, corrupt, missing, files.Count);
						}
					}
					catch
					{
						// error
						++ver;
						++corrupt;
						onVerifyProgress(entry.file, MD5VerifyStatus.Error, ver, success, corrupt, missing, files.Count);
					}
				}
				else
				{
					// file does not exist
					++ver;
					++missing;
					onVerifyProgress(entry.file, MD5VerifyStatus.FileNotFound, ver, success, corrupt, missing, files.Count);
				}
			}

			onVerifyDone(null);
		} // public void doVerify()

	} // public class MD5Verifier
}

