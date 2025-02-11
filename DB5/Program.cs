using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        using (AppDbContext db = new AppDbContext())
        {
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var product1 = new Product { Name = "Laptop", Price = 1200f };
            var product2 = new Product { Name = "Smartphone", Price = 800f };
            db.Products.AddRange(product1, product2);
            db.SaveChanges();

            Console.WriteLine("Products:");
            foreach (var product in db.Products.ToList())
            {
                Console.WriteLine($"Id: {product.Id}, Name: {product.Name}, Price: {product.Price}");
            }

            var order = new Order { Date = DateTime.Now };
            db.Orders.Add(order);
            db.SaveChanges();

            order.Products.Add(product1);
            order.Products.Add(product2);
            db.SaveChanges();

            Console.WriteLine("Orders:");
            foreach (var o in db.Orders.Include(o => o.Products))
            {
                Console.WriteLine($"Order Id: {o.Id}, Date: {o.Date}");
                foreach (var p in o.Products)
                {
                    Console.WriteLine($" - Product: {p.Name}, Price: {p.Price}");
                }
            }
        }
    }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public float Price { get; set; }
    public List<Order> Orders { get; set; } = new();
}

public class Order
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public List<Product> Products { get; set; } = new();
}

public class AppDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=testdb;Trusted_Connection=True;");
    }
}
