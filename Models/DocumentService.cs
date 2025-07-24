using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibreOfficeAI.Models
{
    internal class DocumentService
    {
        public List<Document> DocumentsInUse { get; set; }

        public List<Document> AllDocuments { get; set; }
    }

    public class Document
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public DocType DocType { get; set; }

        Document(string name, string path, DocType docType)
        {
            Name = name;
            Path = path;
            DocType = docType;
        }
    }

    public enum DocType
    {
        Writer,
        Impress,
    }
}
