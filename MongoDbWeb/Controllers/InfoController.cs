using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDbWeb.Models;

namespace MongoDbWeb.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class InfoController : ControllerBase
    {
        /// <summary>
        /// Назви всіх баз даних, що зберігаються на сервері.
        /// </summary>
        /// <returns></returns>
        [HttpGet(Name = "ShowDatabaseNames")]
        public async Task<IEnumerable<string>> GetDatabases()
        {
            var client = new MongoClient(ConnectionString.MongoConnectionString);

            var databases = await client.ListDatabaseNamesAsync();

            return await databases.ToListAsync();
        }

        /// <summary>
        /// Назви всіх колекцій заданої бази даних.
        /// </summary>
        /// <returns></returns>
        [HttpGet(Name = "ShowDatabaseCollectionNames")]
        public async Task<IActionResult> GetDatabaseCollections()
        {
            var client = new MongoClient(ConnectionString.MongoConnectionString);
            var database = client.GetDatabase(ConnectionString.DatabaseName);

            if (database == null)
            {
                return NotFound();
            }

            List<string> collections = new List<string>();

            var listOfCollections = (await database.ListCollectionsAsync()).ToList();
            foreach (BsonDocument collection in listOfCollections)
            {
                string name = collection["name"].AsString;
                collections.Add(name);
            }

            return new OkObjectResult(collections);
        }

        /// <summary>
        /// Назви усіх документів із заданої колекції.
        /// </summary>
        /// <returns></returns>
        [HttpGet(Name = "ShowDocumentsFromProvidedCollection")]
        public async Task<IActionResult> GetCollectionDocuments()
        {
            var client = new MongoClient(ConnectionString.MongoConnectionString);
            var database = client.GetDatabase(ConnectionString.DatabaseName);

            if (database != null)
            {
                var collection = database.GetCollection<Book>(ConnectionString.CollectionName);
                var filter = new BsonDocument();

                return Ok(collection.Find(filter).ToList());
            }

            return BadRequest();
        }

        [HttpGet(Name = "ShowComputersCategory")]
        public async Task<IActionResult> GetComputersBooks()
        {
            var client = new MongoClient(ConnectionString.MongoConnectionString);
            var database = client.GetDatabase(ConnectionString.DatabaseName);

            if (database != null)
            {
                var collection = database.GetCollection<Book>(ConnectionString.CollectionName);

                var filter = new BsonDocument();
                var elem = new BsonElement("Category", "Computers");
                filter.Add(elem);

                return Ok(collection.Find(filter).ToList());
            }

            return BadRequest();
        }

        [HttpGet(Name = "ShowExpensiveBooks")]
        public async Task<IActionResult> GetBooksExpensiveThan()
        {
            var client = new MongoClient(ConnectionString.MongoConnectionString);
            var database = client.GetDatabase(ConnectionString.DatabaseName);

            var filter = new BsonDocument("Price", new BsonDocument("$gt", 50.00));

            var expensiveBooks = database.GetCollection<Book>(ConnectionString.CollectionName).Find(filter).ToList();

            return Ok(expensiveBooks);
        }

        [HttpGet(Name = "ShowBooksPerAuthorAndCategory")]
        public async Task<IActionResult> GetComputerBooksSpecificAuthor()
        {
            var client = new MongoClient(ConnectionString.MongoConnectionString);
            var database = client.GetDatabase(ConnectionString.DatabaseName);

            var filter = new BsonDocument("$and",
                new BsonArray
                {
                    new BsonDocument("Author", "Ralph Johnson"),
                    new BsonDocument("Category", "Computers")
                });

            var books = database.GetCollection<Book>(ConnectionString.CollectionName).Find(filter).ToList();

            return Ok(books);
        }

        [HttpGet(Name = "ShowBooksWithNameLongerThanAndContainingSpecCharactersInAscendingOrder")]
        public async Task<IActionResult> GetBooksWithFilteredNameLongerOrdered()
        {
            var client = new MongoClient(ConnectionString.MongoConnectionString);
            var database = client.GetDatabase(ConnectionString.DatabaseName);

            var filter = Builders<Book>.Filter.And(
                Builders<Book>.Filter.Regex(b => b.BookName, new BsonRegularExpression("^Mongo")),
                Builders<Book>.Filter.Where(b => b.BookName.Length > 50)
            );

            var books = database.GetCollection<Book>(ConnectionString.CollectionName).Find(filter).ToList();

            return Ok(books);
        }

        [HttpGet(Name = "ComplexFilter")]
        public async Task<IActionResult> GetBooksWithCompexFilter()
        {
            var client = new MongoClient(ConnectionString.MongoConnectionString);
            var database = client.GetDatabase(ConnectionString.DatabaseName);

            var filter = Builders<Book>.Filter.And(
            Builders<Book>.Filter.Gt(b => b.Price, 30),
            Builders<Book>.Filter.Eq(b => b.Category, "Computers"),
            Builders<Book>.Filter.In(b => b.Author, new[] { "Robert C. Martin", "Ralph Johnson" }),
            Builders<Book>.Filter.Or(
                Builders<Book>.Filter.Regex(b => b.BookName, new BsonRegularExpression("^Clean")),
                Builders<Book>.Filter.Regex(b => b.BookName, new BsonRegularExpression("^Design"))),
                Builders<Book>.Filter.Gt(b => b.PublicationDate, new DateTime(2000, 1, 1))  // Example date
            );

            var sort = Builders<Book>.Sort.Ascending(b => b.Author);

            // Retrieve the list of books matching the filter
            var filteredBooks = database.GetCollection<Book>(ConnectionString.CollectionName).Find(filter).Sort(sort).ToList();

            return Ok(filteredBooks);
        }

        [HttpPost]
        public async Task<IActionResult> InsertOne([FromBody] Book book)
        {
            if (book == null)
            {
                return BadRequest("Book is null.");
            }

            var client = new MongoClient(ConnectionString.MongoConnectionString);
            var database = client.GetDatabase(ConnectionString.DatabaseName);

            BsonDocument document = new BsonDocument
               {
                   { "Name", book.BookName },
                   { "Price", book.Price },
                   { "Category", book.Category },
                   { "Author", book.Author },
                   { "PublicationDate", book.PublicationDate },
               };

            database.GetCollection<BsonDocument>(ConnectionString.CollectionName).InsertOne(document);

            return CreatedAtAction(nameof(InsertOne), new { id = book.Id }, book);
        }

        [HttpPost("insertMany")]
        public async Task<IActionResult> InsertMany([FromBody] IEnumerable<Book> books)
        {
            var client = new MongoClient(ConnectionString.MongoConnectionString);
            var database = client.GetDatabase(ConnectionString.DatabaseName);
            var collection = database.GetCollection<BsonDocument>(ConnectionString.CollectionName);

            List<BsonDocument> documents = [];
            foreach (var book in books)
            {
                documents.Add(new BsonDocument
                {
                    { "Name", book.BookName },
                    { "Price", book.Price },
                    { "Category", book.Category },
                    { "Author", book.Author },
                    { "PublicationDate", book.PublicationDate },
                });
            }

            await collection.InsertManyAsync(documents);

            return CreatedAtAction(nameof(InsertMany), books);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateOne(string name, [FromBody] Book book)
        {
            var client = new MongoClient(ConnectionString.MongoConnectionString);
            var database = client.GetDatabase(ConnectionString.DatabaseName);
            var collection = database.GetCollection<BsonDocument>(ConnectionString.CollectionName);

            var filter = new BsonDocument("Name", name);

            BsonDocument new_doc = new BsonDocument
            {
                { "Price", book.Price },
                { "Category", book.Category },
                { "Author", book.Author },
                { "PublicationDate", book.PublicationDate },
                { "BookName", book.BookName },
            };

            collection.ReplaceOne(filter, new_doc);

            return Ok();
        }

        [HttpPut("updateMany")]
        public async Task<IActionResult> UpdateMany([FromBody] UpdateManyParameter parameter)
        {
            var client = new MongoClient(ConnectionString.MongoConnectionString);
            var database = client.GetDatabase(ConnectionString.DatabaseName);
            var collection = database.GetCollection<BsonDocument>(ConnectionString.CollectionName);

            collection.UpdateMany(new BsonDocument("Category", parameter.Category),
                new BsonDocument("$inc", new BsonDocument("Price", parameter.Price)));

            return Ok();
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteOne(string bookId)
        {
            var client = new MongoClient(ConnectionString.MongoConnectionString);
            var database = client.GetDatabase(ConnectionString.DatabaseName);
            var collection = database.GetCollection<BsonDocument>(ConnectionString.CollectionName);

            var filter = new BsonDocument("_id", ObjectId.Parse(bookId));

            collection.DeleteOne(filter);

            return Ok();
        }

        [HttpDelete("deleteMany")]
        public async Task<IActionResult> DeleteMany(string author)
        {
            var client = new MongoClient(ConnectionString.MongoConnectionString);
            var database = client.GetDatabase(ConnectionString.DatabaseName);
            var collection = database.GetCollection<BsonDocument>(ConnectionString.CollectionName);

            var filter = new BsonDocument("Author", author);

            collection.DeleteMany(filter);

            return Ok();
        }
    }

    public class UpdateManyParameter
    {
        public decimal Price { get; set; }
        public string Category { get; set; }
    }
}
