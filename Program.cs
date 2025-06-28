using System.Text.Json;


public class JFS
{
    public async Task<List<T>> ReadFromFileAsync<T>(string fpath)
    {
        try
        {
            await using FileStream fstream = File.OpenRead(fpath);
            return await JsonSerializer.DeserializeAsync<List<T>>(fstream) ?? new List<T>();
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"File not found: {fpath}");
            return new List<T>();
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON deserialization error: {ex.Message}");
            return new List<T>();
        }
    }

    public async Task WriteFromFileAsync<T>(string fpath, IEnumerable<T> data)
    {
        try
        {
            await using FileStream fstream = File.OpenWrite(fpath);
            var option = new JsonSerializerOptions { WriteIndented = true };
            await JsonSerializer.SerializeAsync(fstream, data, option);
        }
        catch (IOException ex)
        {
            Console.WriteLine($"I/O error while writing to file: {ex.Message}");
        }

    }
}

public class JDP
{
    private readonly JFS jfs;
    public JDP(JFS jfs)
    {
        this.jfs = jfs;
    }
    public async Task<List<T>> FilterDataAsync<T>(string fpath, Func<T, bool> predicate)
    {
        var data = await jfs.ReadFromFileAsync<T>(fpath);
        return data.Where(predicate).ToList();
    }
    public async Task<List<T>> GetSortedDataAsync<T, Tkey>(string fpath, Func<T, Tkey> keySelector)
    {
        var data = await jfs.ReadFromFileAsync<T>(fpath);
        return data.OrderBy(keySelector).ToList();
    }
    public async Task<Dictionary<Tkey, List<T>>> GroupDataAsync<T, Tkey>(string fpath, Func<T, Tkey> keySelector)
    {
        var data = await jfs.ReadFromFileAsync<T>(fpath);
        return data.GroupBy(keySelector).ToDictionary(g => g.Key, g => g.ToList());
    }
    public async Task<List<TResult>> ProjectDataAsync<T, TResult>(string fpath, Func<T, TResult> selector)
    {
        var data = await jfs.ReadFromFileAsync<T>(fpath);
        return data.Select(selector).ToList();
    }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; }
}

internal class Program
{
    static async Task Main(string[] args)
    {
        var file_service = new JFS();
        var process = new JDP(file_service);
        var products = new List<Product>
        {
            new Product { Id = 1, Name = "Laptop", Price = 999.99m, Category = "Electronics" },
            new Product { Id = 2, Name = "Smartphone", Price = 499.99m, Category = "Electronics" },
            new Product { Id = 3, Name = "Desk", Price = 199.99m, Category = "Furniture" },
            new Product { Id = 4, Name = "Chair", Price = 89.99m, Category = "Furniture" }
        };
        await file_service.WriteFromFileAsync("products.json", products);
        var max_price = await process.FilterDataAsync<Product>("products.json", p => p.Price > 10000);
        foreach (var p in max_price)
        {
            Console.WriteLine(p.Name);
        }
        var product_category = await process.GroupDataAsync<Product, string>("products.json", p => p.Category);
        foreach (var category in product_category)
        {
            Console.WriteLine($"{category.Key}: {category.Value.Count} products");
        }
        var sorted_products = await process.GetSortedDataAsync<Product, decimal>("products.json", p => p.Price);
        foreach (var p in sorted_products)
        {
            Console.WriteLine($"{p.Name}");
        }
        var select_product = await process.ProjectDataAsync<Product, decimal>("products.json", p => p.Price);
        foreach (var price in select_product)
        {
            Console.WriteLine(price);
        }
    }
}