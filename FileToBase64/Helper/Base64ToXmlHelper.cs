using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace FileToBase64.Helper
{
    /// <summary>
    /// Helper class to serialise/deserialise a file to an XML file.
    /// </summary>
    internal static class Base64ToXmlHelper
    {
        /// <summary>XML element name for the root element.</summary>
        private static string _rootTag = "FileToBase64";

        /// <summary>XML element name for the file as a base64 string.</summary>
        private static string _base64FileTag = "Base64File";

        /// <summary>XML element name for the target filename (the deserialised file).</summary>
        private static string _filenameTag = "filename";

        /// <summary>XML element name for the file checksum.</summary>
        private static string _checksumTag = "MD5";

        /// <summary>
        /// Convert a file to base64 and put it into an XML string.
        /// </summary>
        /// <param name="sourceFile">Source file info.</param>
        /// <returns>The serialised file, as a string.</returns>
        public async static Task<string> FileToXml(FileInfo sourceFile)
        {
            int bufferSize = 1000;
            byte[] buffer = new byte[bufferSize];
            int readBytes = 0;

            using (var stream = new MemoryStream())
            {
                using (XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings() { Async = true }))
                {
                    FileStream inputFile = new FileStream(sourceFile.FullName, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);

                    await writer.WriteStartDocumentAsync();

                    await writer.WriteStartElementAsync(null, _rootTag, null);

                    // The file is serialised and put into the XML element.
                    await writer.WriteStartElementAsync(null, _base64FileTag, null);
                    BinaryReader br = new BinaryReader(inputFile);

                    do
                    {
                        readBytes = br.Read(buffer, 0, bufferSize);
                        await writer.WriteBase64Async(buffer, 0, readBytes);
                    }
                    while (bufferSize <= readBytes);
                    br.Close();

                    await writer.WriteEndElementAsync();

                    // Put the filename into a specific tag.
                    await writer.WriteStartElementAsync(null, _filenameTag, null);
                    await writer.WriteStringAsync(sourceFile.Name);
                    await writer.WriteEndElementAsync();

                    // Calculate the file checksum and put it in a specific tag.
                    await writer.WriteStartElementAsync(null, _checksumTag, null);
                    await writer.WriteStringAsync(Utils.CalculateMd5Hash(sourceFile.FullName));
                    await writer.WriteEndElementAsync();

                    // Close root element.
                    await writer.WriteEndDocumentAsync();
                }

                // Rewind the stream and read it as a string.
                using (var reader = new StreamReader(stream))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    return await reader.ReadToEndAsync();
                }
            }
        }

        /// <summary>
        /// Deserialise a base64 string to a file.
        /// </summary>
        /// <param name="serialisedContent">Serialised content (information + serialised file).</param>
        /// <param name="targetFolderPath">Folder path to put the deserialised file into.</param>
        /// <returns>The full target file name.</returns>
        public async static Task<string> XmlToFile(string serialisedContent, string targetFolderPath)
        {
            var xmlContent = new XmlDocument();
            xmlContent.LoadXml(serialisedContent);

            // Extract information and the serialised file.
            string fileName = xmlContent.GetElementsByTagName(_filenameTag)[0].InnerXml;

            string fullFileName = Path.Combine(targetFolderPath, fileName);

            // Check if the file already exists; if that's the case, no deserialisation is made
            if (File.Exists(fullFileName))
            {
                throw new IOException($"File {fullFileName} already exists.");
            }

            string sourceFileMd5 = xmlContent.GetElementsByTagName(_checksumTag)[0].InnerXml;

            string serialisedFile = xmlContent.GetElementsByTagName(_base64FileTag)[0].InnerXml;

            await Task.Run(() => File.WriteAllBytes(fullFileName, Convert.FromBase64String(serialisedFile)));

            // Calculate the target file hash and compare it to the source to check if the deserialisation were successful.
            string targetFileMd5 = Utils.CalculateMd5Hash(fullFileName);

            if (!string.Equals(sourceFileMd5, targetFileMd5))
            {
                throw new FormatException($"Filecheck fail: the target file does not have the same hash ({targetFileMd5}) as the source ({sourceFileMd5}).");
            }
            else
            {
                return fullFileName;
            }
        }
    }
}
