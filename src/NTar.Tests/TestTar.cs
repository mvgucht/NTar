﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using NUnit.Framework;

namespace NTar.Tests
{
    [TestFixture]
    public class TestTar
    {
        [Test]
        public void TestEntries()
        {
            var testDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            using (var stream = File.OpenRead(Path.Combine(testDirectory, "test.tar")))
            {
                CheckStream(stream);
            }
        }

        /// <summary>
        /// Same test as TestEntries, but we perform this through a GzipStream
        /// </summary>
        [Test]
        public void TestEntriesGunzip()
        {
            var testDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location);

            var memoryStream = new MemoryStream();
            using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal, true))
            using (var stream = File.OpenRead(Path.Combine(testDirectory, "test.tar")))
            {
                stream.CopyTo(gzipStream);
                gzipStream.Flush();
            }
            memoryStream.Position = 0;
            using (var gunzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
            {
                CheckStream(gunzipStream);
            }
        }

        public void CheckStream(Stream stream)
        {
            var testDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            {
                var files = new Dictionary<string, string>();

                // Untar the stream
                foreach (var entryStream in stream.Untar())
                {
                    var reader = new StreamReader(entryStream);
                    files[entryStream.FileName] = reader.ReadToEnd();
                }

                Assert.AreEqual(2, files.Count);

                Assert.True(files.ContainsKey("./a.txt"));
                Assert.True(files.ContainsKey("./b/b.txt"));

                Assert.AreEqual("0123456789", files["./a.txt"]);
                Assert.AreEqual(string.Empty, files["./b/b.txt"]);

                if (stream.CanSeek)
                {
                    stream.Position = 0;
                    stream.UntarTo(testDirectory);
                    Assert.AreEqual("0123456789", File.ReadAllText(Path.Combine(testDirectory, "./a.txt")));
                }
            }
        }


        [Test]
        public void TestToDirectory()
        {
            var testDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var outputDirectory = Path.Combine(testDirectory, "output");
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }

            using (var stream = File.OpenRead(Path.Combine(testDirectory, "test.tar")))
            {
                stream.UntarTo(outputDirectory);

                var fileA = Path.Combine(outputDirectory, "./a.txt");
                var fileB = Path.Combine(outputDirectory, "./b/b.txt");

                Assert.True(File.Exists(fileA));
                Assert.True(File.Exists(fileB));

                Assert.AreEqual("0123456789", File.ReadAllText(fileA));
                Assert.AreEqual(string.Empty, File.ReadAllText(fileB));
            }
        }
    }
}