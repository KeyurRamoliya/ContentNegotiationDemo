using System.Text;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace ContentNegotiationDemo.Formatters
{
    public class HtmlOutputFormatter : TextOutputFormatter
    {
        public HtmlOutputFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/html"));
            SupportedEncodings.Add(Encoding.UTF8);
        }

        protected override bool CanWriteType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return typeof(IEnumerable<WeatherForecast>).IsAssignableFrom(type);
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            var response = context.HttpContext.Response;
            var data = context.Object as IEnumerable<WeatherForecast>;

            if (data == null)
                return;

            using (var buffer = new MemoryStream())
            using (var writer = new StreamWriter(buffer, selectedEncoding))
            {
                writer.WriteLine("<!DOCTYPE html>");
                writer.WriteLine("<html lang=\"en\">");
                writer.WriteLine("<head>");
                writer.WriteLine("<meta charset=\"UTF-8\">");
                writer.WriteLine("<title>Content Negotiation Demo</title>");
                writer.WriteLine("<style>");
                writer.WriteLine("table, th, td { border: 1px solid black; border-collapse: collapse; }");
                writer.WriteLine("th, td { padding: 5px; }");
                writer.WriteLine("</style>");
                writer.WriteLine("</head>");
                writer.WriteLine("<body>");

                var htmlTable = data.ToHtmlTable();

                writer.WriteLine(htmlTable);

                writer.WriteLine("</body>");
                writer.WriteLine("</html>");
                writer.Flush();

                buffer.Position = 0;
                await buffer.CopyToAsync(response.Body);
            }
        }
    }
}
