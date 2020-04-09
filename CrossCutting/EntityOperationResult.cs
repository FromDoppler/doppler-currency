using System.Collections.Generic;

namespace CrossCutting
{
    public class EntityOperationResult<TEntity> where TEntity : class
    {
        public TEntity Entity { get; set; }
        public bool Success => Entity != null && Errors.Count == 0;
        public IDictionary<string, List<string>> Errors { get; }

        public EntityOperationResult(TEntity entity = null)
        {
            Entity = entity;
            Errors = new Dictionary<string, List<string>>();
        }

        public void AddError(string key, string message)
        {
            List<string> list;

            if (Errors.ContainsKey(key))
            {
                list = Errors[key];
            }
            else
            {
                list = new List<string>();
                Errors.Add(key, list);
            }

            list.Add(message);
        }
    }
}