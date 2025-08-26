﻿namespace EasyReasy.KnowledgeBase.Tests
{
    [ResourceCollection(typeof(EmbeddedResourceProvider))]
    public static class TestDataFiles
    {
        public static readonly Resource TestDocument01 = new Resource("TestData/TestDocument01.md");
        public static readonly Resource TestDocument02 = new Resource("TestData/TestDocument02.md");
        public static readonly Resource TestDocument03 = new Resource("TestData/TestDocument03.md");
        public static readonly Resource TestDocument04 = new Resource("TestData/TestDocument04.md");
        public static readonly Resource TestDocument05 = new Resource("TestData/TestDocument05.md");
    }
}
