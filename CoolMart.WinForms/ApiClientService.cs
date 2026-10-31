using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace CoolMart.WinForms
{
    public static class ApiClientService
    {
        // Địa chỉ port khớp với đường dẫn chạy của Web API Backend (xem trên thanh địa chỉ Swagger)
        public static readonly HttpClient Client = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7182/api/")
        };

        public static void AttachToken(string token)
        {
            Client.DefaultRequestHeaders.Authorization = null;
            if (!string.IsNullOrEmpty(token))
            {
                Client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        }
    }
}