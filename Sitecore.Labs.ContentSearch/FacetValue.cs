
namespace Sitecore.Labs.ContentSearch
{
    public class FacetValue<T>
    {
        public FacetDefinition<T> FacetDefinition { get; private set; }
        public int Count { get; set; }
        public string Value { get; set; }
        public bool Selected { get; set; }

        public FacetValue(string value, int count, bool selected, FacetDefinition<T> facetDefinition)
        {
            Value = value;
            Count = count;
            Selected = selected;
            FacetDefinition = facetDefinition;
        }
        public string DisplayValue
        {
            get
            {
                if (FacetDefinition.ValueToDisplayName != null)
                {
                    return FacetDefinition.ValueToDisplayName(Value);
                }
                return Value;
            }
        }

    }
}
