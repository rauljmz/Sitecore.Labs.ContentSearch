using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq.Utilities;
using Sitecore.ContentSearch.Linq;
using Sitecore.Data;
using Sitecore.ContentSearch.SearchTypes;
using System.Linq.Expressions;

namespace Sitecore.Labs.ContentSearch
{
    public class SearchQuery<T> where T : SearchResultItem, new()
    {
        private ISearchIndex Index;

        #region properties

        public int TotalResults { get; private set; }
        public IEnumerable<T> Results { get; private set; }
        public ID RootItemID { get; set; }

        public List<string> FieldsToSearch { get; private set; }
        public List<Expression<Func<T, bool>>> Must { get; private set; }
        public List<Expression<Func<T, bool>>> Could { get; private set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public bool RestrictToCurrentLanguage { get; set; }

        public List<ID> RestrictTemplates { get; private set; }

        public List<FacetDefinition<T>> FacetDefinitions { get; private set; }

        public int MinFacetValues { get; set; }

        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalNumberPages { get; private set; }

        public float Fuzziness { get; set; }



        #endregion

        public SearchQuery(string databaseName)
        {
            var indexname = string.Format("sitecore_{0}_index", databaseName);
            Index = Sitecore.ContentSearch.ContentSearchManager.GetIndex(indexname);
            PageSize = 10;
            Page = 1;
            FieldsToSearch = new List<string>();
            RestrictTemplates = new List<ID>();
            FacetDefinitions = new List<FacetDefinition<T>>();
            RootItemID = ID.Null;
            MinFacetValues = 1;
            From = DateTime.MinValue;
            To = DateTime.MaxValue;
            Fuzziness = 0f;
            RestrictToCurrentLanguage = true;
            Must = new List<Expression<Func<T, bool>>>();
            Could = new List<Expression<Func<T, bool>>>();
        }

        public SearchQuery(Database database)
            : this(database.Name)
        {

        }
        public SearchQuery()
            : this(Sitecore.Context.Database)
        {
        }

        public void DoSearch()
        {
            DoSearch(null);
        }

        public void DoSearch(string searchTerms)
        {
            var predicate = BuildPredicate(searchTerms);
            using (var context = Index.CreateSearchContext())
            {
                var search = context.GetQueryable<T>()
                    .Where(predicate);
                foreach (var facet in FacetDefinitions)
                {
                    search = search.FacetOn(facet.Field, MinFacetValues);

                    foreach (var filter in facet.Filter)
                    {
                        search = search.Filter(facet.CreateFilterExpression(filter));
                    }
                }
                if (PageSize > 0)
                {
                    search = search.Page(Page - 1, PageSize);
                }

                var results = search.GetResults();

                TotalResults = results.TotalSearchResults;
                Results = results.Hits
                    .OrderByDescending(h => h.Score)
                    .Select(d =>
                {
                    var boostable = d.Document as IScorable;
                    if (boostable != null)
                    {
                        boostable.Score = d.Score;
                    }
                    return d.Document;
                })
                .ToList();

                if (FacetDefinitions.Any())
                {
                    foreach (var resultCategory in results.Facets.Categories)
                    {
                        var facetDefinition = FacetDefinitions.FirstOrDefault(f => f.FacetName == resultCategory.Name);
                        if (facetDefinition != null)
                        {
                            facetDefinition.Values = resultCategory.Values.Select(v => new FacetValue<T>(v.Name, v.AggregateCount, facetDefinition.Filter.Contains(v.Name), facetDefinition));
                        }
                    }
                }
                TotalNumberPages = TotalResults / PageSize + (TotalResults % PageSize != 0 ? 1 : 0);
            }
        }

        private System.Linq.Expressions.Expression<Func<T, bool>> BuildPredicate(string searchTerms)
        {
            var predicate = PredicateBuilder.True<T>();

            foreach (var condition in Could)
            {
                predicate = predicate.Or(condition);
            }

            if (searchTerms != null)
            {
                if (FieldsToSearch.Any())
                {
                    foreach (var field in FieldsToSearch)
                    {
                        if (Fuzziness > 0)
                        {
                            predicate = predicate.Or(ri => ri[field].Like(searchTerms, Fuzziness));
                        }
                        else
                        {
                            predicate = predicate.Or(ri => ri[field].Contains(searchTerms));
                        }
                    }
                }
                else
                {
                    if (Fuzziness > 0)
                    {
                        predicate = predicate.Or(ri => ri.Content.Like(searchTerms, Fuzziness));
                    }
                    else
                    {
                        predicate = predicate.Or(ri => ri.Content.Contains(searchTerms));
                    }

                }
            }

            foreach (var condition in Must)
            {
                predicate = predicate.And(condition);
            }

            if (RootItemID != ID.Null)
            {
                predicate = predicate.And(ri => ri.Paths.Contains(RootItemID));
            }

            if (To < DateTime.MaxValue || From > DateTime.MinValue)
            {
                predicate = predicate.And(ri => ri.Updated.Between(From, To, Inclusion.Both));
            }

            var templatesPredicate = PredicateBuilder.True<T>();
            foreach (var templateid in RestrictTemplates)
            {
                templatesPredicate = templatesPredicate.Or(ri => ri.TemplateId == templateid);
            }
            predicate = predicate.And(templatesPredicate);

            if (RestrictToCurrentLanguage)
            {
                predicate = predicate.And(i => i.Language == Sitecore.Context.Language.Name);
            }

            return predicate;
        }
        
    }
}
