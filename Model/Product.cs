using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BasicBot.Model
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public float Price { get; set; }
        public string Image { get; set; }
        public string Brand { get; set; }

        public Product(string name, float price, string image, string brand)
        {
            Name = name;
            Price = price;
            Image = image;
            Brand = brand;
        }
    }
}
