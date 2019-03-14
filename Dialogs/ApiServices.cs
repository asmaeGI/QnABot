
using BasicBot.Dialogs.Shoes;
using BasicBot.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace BasicBot.Dialogs
{
    public  static class ApiServices
    {
        public static async Task<List<Product>> GetProductByCategorie(ShoesState shoesState)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://localhost:44307/");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var stringContent = new StringContent(JsonConvert.SerializeObject(shoesState), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("api/Products/ProductByCategorie", stringContent);
                var result = response.Content.ReadAsStringAsync().Result;
                return JsonConvert.DeserializeObject<List<Product>>(result);
            }
        }
    }
}
