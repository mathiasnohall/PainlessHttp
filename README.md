# PainlessHttp

_No external libraries! No over engineered method signatures! No uber verbose setup! Just a HTTP client that is so easy to use that it won�t ever give you any headache!_

* async/sync ``GET``, ``POST``, ``PUT`` and ``DELETE``
* Content negotiation, so that you don't have to mind about ContentTypes, AcceptHeaders and all that stuff
* Plugable ``If-Modified-Since`` caches for speeding up your application even more.
* Authentication
* Plugable serializers, use the built in onces, plugin your favorite onces (like ``PainlessHttp.Serializers.JsonNet``) or build one yourself
* No external references to NuGets (_just Microsoft Core Libs!_)

Getting typed metadata async has never been easier

```csharp
	var client = new HttpClient("http://localhost:1337/");
	var response = await client.GetAsync<Todo>("api/todos/2");
	Todo todo = response.Body;
	Console.WriteLine("Mission of the day: {0}", todo.Description);
```
Store new data with ``POST``

```csharp
	var client = new HttpClient("http://localhost:1337/");
	var tomorrow = new Todo { Description = "Sleep in" };
	Todo created = client.PostAsync<Todo>("api/todos", tomorrow).Result;
	Console.WriteLine("Latest todo: {0}", created.Description);
```

... or update it with ``PUT``

```csharp
	// retrieve data
	var client = new HttpClient(config);
	var response = client.GetAsync<Todo>("api/todos/2").Result;
	var completedTodo = response.Body;

	// update it
	completedTodo.IsCompleted = true;
	client.Put<string>("api/todos", completedTodo);
```

``DELETE`` works the same way

```csharp
	var client = new HttpClient("http://localhost:1337/");
	var response = client.DeleteAsync<string>("api/todos/2").Result;
	if (response.StatusCode == HttpStatusCode.OK)
	{
		Console.WriteLine("Todo successfully deleted");
	}
```

## Configuration
### Serializers
Painless Http comes with a set of serializers for the standard formats (``application/xml``, ``application/json``). These serializers are registered in the client by default. This means that if you don't really care about how serialization is done, you can jump to the next section.

If you want to override serializers, just say so in the configuration object
```csharp
  var config = new HttpClientConfiguration
	{
		BaseUrl = "http://localhost:1337/",
		Advanced =
		{
			Serializers = new List<IContentSerializer>
			{
				PainlessJsonNet()
			}
		}
	};
  var client = new HttpClient(config);
```

There are more ways to create customized serializers.

```csharp
  var typedJson = new DefaultJsonSerializer();
  var defaultJson = new Serializer<DefaultJson>(ContentType.ApplicationJson);
  var customJson = SerializeSettings
    .For(ContentType.ApplicationJson)
      .Serialize(NewtonSoft.Serialize)        // use NewtonSoft's serializer
      .Deserialize(DefaultJson.Deserialize); // ...but the normal deserializer
```

Of course, you can create your own class  that implements ``IContentSerializer`` and register that one.

#### Increase speed with pre-complied serializers
Painless Http wants to perform your requests as fast as possible. That's why all the type-specific serializers are cached in all the default implementations. If you request the same _type_ of object twice, you won't need to pay the penalty of creating a new type-specific serializer. However, in some instances you would want to reduce the overhead of creating the serializer in the first request, too.  If you want to speed up things right from the start, just register underlying serializers
```csharp
  var ignores = new XmlAttributeOverrides();
  ignores.Add(typeof(Todo), "Description", new XmlAttributes { XmlIgnore = true });
  var preCompiledSerializers = new Dictionary<Type, XmlSerializer>
  {
    {typeof(Todo), new XmlSerializer(typeof(Todo), ignores)}
  };
  var xmlSerializer = new DefaultXmlSerializer(preCompiledSerializers);

  var xmlTomorrow = xmlSerializer.Serialize(tomorrow);
```

_Note that the Jsonsoft `ContentConverter is not part of the core lib. Download the nuget_ ``PainlessHttp.Serializers.JsonNet``.

### Content Negotiation
If the server responds with status code ``UnsupportedMediaType`` or the Accept headers that does not contain the supplied content type, the default behaviour is to resend the request with supported content type (based on Accept headers). This behaviour can be overridden by supplying a content type in the request
```csharp
  var config = new Configuration
  {
    BaseUrl = "http://localhost:1337/",
    Advanced =
    {
      ContentNegotiation = true
    }
  };
  var client = new HttpClient(config);
  var newTodo = new Todo { Description = "Write tests" };
  var created = _client.PostAsync<string>("/api/todo", newTodo);
```

### Customizing request
The default behavior of PainlessHttp should satisfy most of the developer out there. However, if you for some reason want to control manipulate properties of the request that is sent, there is a way to do so. With the ``WebrequestModifier`` you get access to the raw request and do all sorts of things, like adding you custom request header.
```csharp
  var config = new HttpClientConfiguration
  {
    BaseUrl = "http://localhost:1337/",
    Advanced =
    {
      WebrequestModifier = req => req.Headers.Add("X-Custom-Header", "Custom-Value")
    }
  };
```

### Authentication
Authentication is handled with ``System.Het.NetworkCredential``, and is registered in the configuration
```csharp
  var config = new HttpClientConfiguration
  {
    BaseUrl = "http://localhost:1337/",
    Advanced =
    {
			Credentials = Credentials = new NetworkCredential("pardahlman", "password")
		}
	};
```

### If-Modified-Since Cache
If the entities that you are requesting are large, and the server supports [If-Modified-Since Headers](http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html), you can turn caching on the through PainlessHttp to make you application even faster. There are three cache types
* ``NoCache`` (does nothing)
* ``InMemoryCache`` (saves entities in working memory)
* ``FileCache`` (saves entities on disk)

This is done in the configuration view:
```csharp
  var cacheDirectory = Environment.CurrentDirectory;
  var config = new Configuration
  {
  	BaseUrl = "http://localhost:1337/",
  	Advanced =
  	{
  		ModifiedSinceCache = new FileCache(cacheDirectory)
  	}
  };
```

## Credits

Author: P�r Dahlman