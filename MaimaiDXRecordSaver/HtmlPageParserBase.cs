using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace MaimaiDXRecordSaver.PageParser
{
    public abstract class HtmlPageParserBase<T>
    {
        protected HtmlDocument doc;
        protected T resultObj;

        public HtmlPageParserBase()
        {
            doc = new HtmlDocument();
            resultObj = default(T);
        }

        public void LoadPage(string page)
        {
            doc.LoadHtml(page);
        }

        public T GetResult()
        {
            return resultObj;
        }

        public abstract void Parse();
    }
}
