using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Newtonsoft.Json;
using StackExchange.Redis;
using web_app_performance.Model;

namespace web_app_performance.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProdutosController : ControllerBase
    {
        private static ConnectionMultiplexer redis;

        [HttpGet]
        public async Task<IActionResult> GetProduto()
        {
            string key = "getprodutos";
            redis = ConnectionMultiplexer.Connect("localhost:6379");
            IDatabase db = redis.GetDatabase();
            await db.KeyExpireAsync(key, TimeSpan.FromMinutes(10));
            string user = await db.StringGetAsync(key);

            if (!string.IsNullOrEmpty(user))
            {
                return Ok(user);
            }


            string connectionString = "Server=localhost;Database=sys;User=root;Password=123;";
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            string query = "SELECT id, nome, preco, quantidade_estoque, data_criacao FROM produtos;";
            var produtos = await connection.QueryAsync<Produto>(query);
            string produtosJson = JsonConvert.SerializeObject(produtos);
            await db.StringSetAsync(key, produtosJson);

            return Ok(produtos);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Produto produto)
        {
            string connectionString = "Server=localhost;Database=sys;User=root;Password=123;";
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();


            string sql = "INSERT INTO produtos (nome, preco, quantidade_estoque, data_criacao) VALUES (@nome, @preco, @quantidade_estoque, @data_criacao);";
            await connection.ExecuteAsync(sql, produto);

            //apagar cache
            string key = "getprodutos";
            redis = ConnectionMultiplexer.Connect("localhost:6379");
            IDatabase db = redis.GetDatabase();
            await db.KeyDeleteAsync(key);


            return Ok();
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] Produto produto)
        {
            string connectionString = "Server=localhost;Database=sys;User=root;Password=123;";
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();


            string sql = @"UPDATE produtos
                            set nome = @nome,
	                            preco = @preco,
                                quantidade_estoque = @quantidade_estoque,
                                data_criacao = @data_criacao
                            Where ID = @id";
            await connection.ExecuteAsync(sql, produto);

            //apagar cache
            string key = "getprodutos";
            redis = ConnectionMultiplexer.Connect("localhost:6379");
            IDatabase db = redis.GetDatabase();
            await db.KeyDeleteAsync(key);


            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            string connectionString = "Server=localhost;Database=sys;User=root;Password=123;";
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();


            string sql = @"DELETE FROM produtos
                            Where ID = @id";
            await connection.ExecuteAsync(sql, new { id });

            //apagar cache
            string key = "getprodutos";
            redis = ConnectionMultiplexer.Connect("localhost:6379");
            IDatabase db = redis.GetDatabase();
            await db.KeyDeleteAsync(key);


            return Ok();
        }
    }
}
