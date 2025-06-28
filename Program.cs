using System.Xml;
using System.Xml.Linq;

public class XFS
{
    public async Task<XDocument> ReadFromFileAsync(string fpath)
    {
        try
        {
            using var stream = File.OpenRead(fpath);
            return await Task.Run(() => XDocument.Load(stream));
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"File not found: {fpath}");
            return new XDocument();
        }
        catch (XmlException ex)
        {
            Console.WriteLine($"XML error: {ex.Message}");
            return new XDocument();
        }
        catch (IOException ex)
        {
            Console.WriteLine($"I/O error: {ex.Message}");
            return new XDocument();
        }
    }

    public async Task WriteToFileAsync(string fpath, XDocument doc)
    {
        try
        {
            using var stream = File.Create(fpath);
            await Task.Run(() => doc.Save(stream));
        }
        catch (IOException ex)
        {
            Console.WriteLine($"I/O error while writing to file: {ex.Message}");
        }
    }
}

public class XDP
{
    private readonly XFS xfs;
    public XDP(XFS xfs)
    {
        this.xfs = xfs;
    }

    public async Task<List<XElement>> FilterElementsAsync(string fpath, Func<XElement, bool> predicate)
    {
        var doc = await xfs.ReadFromFileAsync(fpath);
        return doc.Root?.Elements("Book").Where(predicate).ToList() ?? new List<XElement>();
    }

    public async Task<List<XElement>> GetSortedElementsAsync(string fpath, Func<XElement, object> keySelector)
    {
        var doc = await xfs.ReadFromFileAsync(fpath);
        return doc.Root?.Elements("Book").OrderBy(keySelector).ToList() ?? new List<XElement>();
    }

    public async Task<Dictionary<string, List<XElement>>> GroupElementsAsync(string fpath, Func<XElement, string> keySelector)
    {
        var doc = await xfs.ReadFromFileAsync(fpath);
        return doc.Root?.Elements("Book").GroupBy(keySelector).ToDictionary(g => g.Key, g => g.ToList()) ?? new Dictionary<string, List<XElement>>();
    }

    public async Task<List<TResult>> ProjectElementsAsync<TResult>(string fpath, Func<XElement, TResult> selector)
    {
        var doc = await xfs.ReadFromFileAsync(fpath);
        return doc.Root?.Elements("Book").Select(selector).ToList() ?? new List<TResult>();
    }

    public async Task AddElementAsync(string fpath, XElement newElement)
    {
        var doc = await xfs.ReadFromFileAsync(fpath);
        doc.Root?.Add(newElement);
        await xfs.WriteToFileAsync(fpath, doc);
    }

    public async Task RemoveElementAsync(string fpath, Func<XElement, bool> predicate)
    {
        var doc = await xfs.ReadFromFileAsync(fpath);
        var toRemove = doc.Root?.Elements("Book").Where(predicate).ToList();
        if (toRemove != null)
        {
            foreach (var el in toRemove)
            {
                el.Remove();
            }
            await xfs.WriteToFileAsync(fpath, doc);
        }
    }
}

internal class Program
{
    static async Task Main(string[] args)
    {
        var file_service = new XFS();
        var process = new XDP(file_service);
        var expensiveBooks = await process.FilterElementsAsync("books.xml", e => (decimal)e.Element("Price") > 45);
        Console.WriteLine("Expensive books:");
        foreach (var b in expensiveBooks)
        {
            Console.WriteLine(b.Element("Title")?.Value);
        }
        var booksByCategory = await process.GroupElementsAsync("books.xml", e => (string)e.Element("Category"));
        Console.WriteLine("\nBooks by category:");
        foreach (var group in booksByCategory)
        {
            Console.WriteLine($"{group.Key}: {group.Value.Count} books");
        }
        var sortedBooks = await process.GetSortedElementsAsync("books.xml", e => (decimal)e.Element("Price"));
        Console.WriteLine("\nSorted by price:");
        foreach (var b in sortedBooks)
        {
            Console.WriteLine($"{b.Element("Title")?.Value} - {b.Element("Price")?.Value}");
        }
        var titles = await process.ProjectElementsAsync("books.xml", e => (string)e.Element("Title"));
        Console.WriteLine("\nBook titles:");
        foreach (var t in titles)
        {
            Console.WriteLine(t);
        }
        var newBook = new XElement("Book",
            new XElement("Title", "Refactoring"),
            new XElement("Author", "Martin Fowler"),
            new XElement("Price", 55.00m),
            new XElement("Category", "Software Engineering")
        );
        await process.AddElementAsync("books.xml", newBook);
        await process.RemoveElementAsync("books.xml", e => (string)e.Element("Title") == "Clean Code");
    }
}