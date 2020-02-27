using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace CrossCutting
{
    /// <summary>
    /// An envelope that wraps the service response.
    /// </summary>
    public class ServiceResponse<TEntity> : ServiceResponse
    {
        /// <summary>
        /// The result from a successful response
        /// </summary>
        public new TEntity Result { get; set; }
    }

    /// <summary>
    /// An envelope that wraps the service response.
    /// </summary>
    public class ServiceResponse
    {
        /// <summary>
        /// Represents the ServiceResponse.
        /// </summary>
        public ServiceResponse()
        {
            Errors = new Dictionary<string, IList<string>>();
        }

        /// <summary>
        /// Represents the ServiceResponse.
        /// </summary>
        /// <param name="errors">list of errors.</param>
        public ServiceResponse(IDictionary<string, IList<string>> errors)
        {
            Errors = errors;
        }

        /// <summary>
        /// Gets if the response was a success.
        /// </summary>
        public bool IsSuccess => Errors.Count == 0;

        /// <summary>
        /// Error messages if the response was not a success.
        /// </summary>
        public IDictionary<string, IList<string>> Errors { get; }

        public HttpRequestMessage Request { get; set; }
        public HttpResponseMessage Response { get; set; }

        /// <summary>
        /// The result from a successful response.
        /// </summary>
        public object Result { get; set; }

        /// <summary>
        /// Add a error message to the collection.
        /// </summary>
        /// <param name="key">error key.</param>
        /// <param name="errorMessage">error message.</param>
        public void AddError(string key, string errorMessage)
        {
            if (Errors.ContainsKey(key))
            {
                Errors[key].Add(errorMessage);
            }
            else
            {
                Errors.Add(key, new List<string>() { errorMessage });
            }
        }

        /// <summary>
        /// Adds an error messages to the collection.
        /// </summary>
        /// <param name="errors">list of errors.</param>
        public void AddErrors(IDictionary<string, IList<string>> errors)
        {
            errors.ToList().ForEach(x => x.Value.ToList().ForEach(v => AddError(x.Key, v)));
        }
    }
}
