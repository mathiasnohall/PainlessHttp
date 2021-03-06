﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Moq;
using NUnit.Framework;
using PainlessHttp.Http;
using PainlessHttp.Http.Contracts;
using PainlessHttp.Integration;
using PainlessHttp.Serializers.Contracts;
using PainlessHttp.Serializers.Defaults;
using PainlessHttp.Utils;

namespace PainlessHttp.Tests.Utils
{
	public class ResponseTransformerTests
	{
		private ResponseTransformer _transformer;
		private List<IContentSerializer> _serializers;
		private Mock<IHttpWebResponse> _rawResponse;

		[SetUp]
		public void Setup()
		{
			_serializers = new List<IContentSerializer>();
			_transformer = new ResponseTransformer(_serializers);

			_rawResponse = new Mock<IHttpWebResponse>();
			_rawResponse
				.Setup(r => r.GetResponseStream())
				.Returns(new MemoryStream());
		}

		[TestCase(HttpStatusCode.OK)]
		[TestCase(HttpStatusCode.PartialContent)]
		public async void Should_Map_Http_Status_Code(HttpStatusCode code)
		{
			/* Setup */
			_serializers.AddRange(ContentSerializers.Defaults);
			_rawResponse
				.Setup(r => r.StatusCode)
				.Returns(code);

			/* Test */
			var result = await _transformer.TransformAsync<string>(_rawResponse.Object);

			/* Assert */
			Assert.That(result.StatusCode, Is.EqualTo(code));
		}

		[TestCase(ContentType.ApplicationJson, ContentTypes.ApplicationJson)]
		[TestCase(ContentType.ApplicationXml, ContentTypes.ApplicationXml)]
		[TestCase(ContentType.TextHtml, ContentTypes.TextHtml)]
		public async void Should_Use_Serializer_Based_On_Content_Type(ContentType contentType, string contentHeaderValue)
		{
			/* Setup */
			var mockedSerializers = new List<Mock<IContentSerializer>>
			{
				new Mock<IContentSerializer>(),
				new Mock<IContentSerializer>(),
				new Mock<IContentSerializer>()
			};

			mockedSerializers[0]
				.Setup(s => s.ContentType)
				.Returns(new List<ContentType> {ContentType.ApplicationXml});
			mockedSerializers[1]
				.Setup(s => s.ContentType)
				.Returns(new List<ContentType> {ContentType.ApplicationJson});
			mockedSerializers[2]
				.Setup(s => s.ContentType)
				.Returns(new List<ContentType> {ContentType.TextHtml});
			_serializers.AddRange(mockedSerializers.Select(s => s.Object));

			var usedSerializer = mockedSerializers.First(s => s.Object.ContentType.Contains(contentType));
			usedSerializer
				.Setup(s => s.Deserialize<object>(It.IsAny<string>()))
				.Verifiable();

			var responseHeaders = new WebHeaderCollection {{HttpResponseHeader.ContentType, contentHeaderValue}};
			_rawResponse
				.Setup(r => r.Headers)
				.Returns(responseHeaders);

			/* Test */
			await _transformer.TransformAsync<object>(_rawResponse.Object);

			/* Assert */
			usedSerializer.VerifyAll();
		}

		[Test]
		public void Should_Try_To_Get_Content_Type_From_Body()
		{
			/* Setup */
			_rawResponse
				.Setup(r => r.ContentType)
				.Returns(ContentTypes.TextHtml);

			/* Test */
			var contentType = _transformer.ExtractContentTypeFromResponse(_rawResponse.Object);

			/* Assert */
			Assert.That(contentType, Is.EqualTo(ContentType.TextHtml));
		}

		[Test]
		public void Should_Get_Content_Type_From_Headers_If_Body_Does_Not_Contain_Any_Content_Type()
		{
			/* Setup */
			var responseHeaders = new WebHeaderCollection { { HttpResponseHeader.ContentType, ContentTypes.TextPlain } };
			_rawResponse
				.Setup(r => r.Headers)
				.Returns(responseHeaders);

			/* Test */
			var contentType = _transformer.ExtractContentTypeFromResponse(_rawResponse.Object);

			/* Assert */
			Assert.That(contentType, Is.EqualTo(ContentType.TextPlain));
		}
	}
}
