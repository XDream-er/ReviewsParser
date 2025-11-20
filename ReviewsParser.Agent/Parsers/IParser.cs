using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReviewsParser.Agent.Parsers
{
    public interface IParser
    {
        IAsyncEnumerable<ParsedReview> ParseAsync(ParsingTask task, ApiClient apiClient, CancellationToken cancellationToken);
    }
}
