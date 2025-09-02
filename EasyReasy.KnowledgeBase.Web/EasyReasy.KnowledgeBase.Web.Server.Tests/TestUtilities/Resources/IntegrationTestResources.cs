using EasyReasy;

namespace EasyReasy.KnowledgeBase.Web.Server.Tests.TestUtilities.Resources
{
    /// <summary>
    /// Defines embedded test resources for FileStorageController integration testing.
    /// </summary>
    public static class IntegrationTestResources
    {
        /// <summary>
        /// Test files for file upload and storage integration scenarios.
        /// </summary>
        [ResourceCollection(typeof(EmbeddedResourceProvider))]
        public static class TestFiles
        {
            /// <summary>
            /// Small text file (~50 bytes) - good for testing basic chunked upload functionality.
            /// </summary>
            public static readonly Resource SmallTextFile = new Resource("TestFiles/small-test.txt");

            /// <summary>
            /// Medium text file (~2KB) - tests multi-chunk upload scenarios.
            /// </summary>
            public static readonly Resource MediumTextFile = new Resource("TestFiles/medium-test.txt");

            /// <summary>
            /// Sample JSON file for testing different content types.
            /// </summary>
            public static readonly Resource JsonFile = new Resource("TestFiles/sample.json");

            /// <summary>
            /// Sample markdown document for testing document uploads.
            /// </summary>
            public static readonly Resource MarkdownFile = new Resource("TestFiles/document.md");
        }
    }
}
