namespace Maple.Result.Extensions.HttpClient
{
    public class Class1
    {
        public async Task Test()
        {
            var httpClient = new System.Net.Http.HttpClient();

            var response = await httpClient.PostAsync("http://test.org", new FormUrlEncodedContent());
        }
    }
}
