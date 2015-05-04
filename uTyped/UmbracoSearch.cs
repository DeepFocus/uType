﻿using AutoMapper;
using Examine;
using Examine.LuceneEngine.SearchCriteria;
using Examine.SearchCriteria;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core.Models;
using Umbraco.Web;

namespace uTyped
{
    public class UmbracoSearch
    {
        // Node Properties Names that are not defined by the document type
        private readonly string[] _nodeProperties = { "Name", "Url", "UrlName" };
        // Used to split the searched string into multiple words
        private readonly char _splitTermCharacter;
        private readonly ExamineManager _examine;
        private readonly UmbracoHelper _umbracoHelper;

        public UmbracoSearch(UmbracoHelper umbracoHelper, char splitTermCharacter = ' ')
            : this(umbracoHelper, ExamineManager.Instance, splitTermCharacter) { }

        public UmbracoSearch(UmbracoHelper umbracoHelper, ExamineManager examine, char splitTermCharacter = ' ')
        {
            _examine = examine;
            _splitTermCharacter = splitTermCharacter;
            _umbracoHelper = umbracoHelper;
        }

        /// <summary>
        /// Search for the term and returns an IEnumerable of T
        /// </summary>
        /// <param name="term">Term to search</param>
        /// <param name="fuzzy">The fuzzieness level</param>
        /// <param name="wildCard">Using wildCard or not</param>
        /// <param name="xPath">Path to the node. If not provided, the output class name is used</param>
        /// <param name="nodeTypeAlias">Alias for the node type in Umbraco</param>
        /// <returns></returns>
        public IEnumerable<T> Search<T>(string term, float fuzzy = 0.5f, bool wildCard = true, string nodeTypeAlias = null, string xPath = null)
        {
            return Search<T>(term, fuzzy, wildCard, Mapper.Map<IEnumerable<T>>, nodeTypeAlias, xPath);
        }

        /// <summary>
        /// Search for the term and returns an IEnumerable of T
        /// </summary>
        /// <param name="term">Term to search</param>
        /// <param name="fuzzy">The fuzzieness level</param>
        /// <param name="wildCard">Using wildCard or not</param>
        /// <param name="mapper">Function mapping the IPublishedContent to the expected out Type T</param>
        /// <param name="xPath">Path to the node. If not provided, the output class name is used</param>
        /// <param name="nodeTypeAlias">Alias for the node type in Umbraco</param>
        /// <returns></returns>
        public IEnumerable<T> Search<T>(string term, float fuzzy, bool wildCard, Func<IEnumerable<IPublishedContent>, IEnumerable<T>> mapper, string nodeTypeAlias = null, string xPath = null)
        {
            //First getting a sample to retrieve all document type properties
            var content = _umbracoHelper.TypedContentSingleAtXPath(xPath ?? string.Format("//{0}", nodeTypeAlias ?? typeof(T).Name));
            if (null != content)
            {
                var properties = content.ContentType.PropertyTypes.Select(p => p.PropertyTypeAlias).Concat(_nodeProperties);
                return Search<T>(term, properties, mapper, fuzzy, wildCard, nodeTypeAlias);
            }

            return mapper(null);
        }

        /// <summary>
        /// Search for the term and returns an IEnumerable of T
        /// </summary>
        /// <param name="term">Term to search</param>
        /// <param name="properties">List of properties to apply the fuzziness on and use for the search</param>
        /// <param name="fuzzy">The fuzzieness level</param>
        /// <param name="wildCard">Using wildCard or not</param>
        /// <returns></returns>
        public IEnumerable<T> Search<T>(string term, IEnumerable<string> properties, float fuzzy = 0.5f, bool wildCard = true)
        {
            return Search<T>(term, properties, Mapper.Map<IEnumerable<T>>, fuzzy, wildCard);
        }

        /// <summary>
        /// Search for the term and returns an IEnumerable of T
        /// </summary>
        /// <param name="term">Term to search</param>
        /// <param name="properties">List of properties to apply the fuzziness on and use for the search</param>
        /// <param name="mapper">Function mapping the IPublishedContent to the expected out Type T</param>
        /// <param name="fuzzy">The fuzzieness level</param>
        /// <param name="wildCard">Using wildCard or not</param>
        /// <param name="nodeTypeAlias">Alias for the node type in Umbraco</param>
        /// <returns></returns>
        public IEnumerable<T> Search<T>(string term, IEnumerable<string> properties, Func<IEnumerable<IPublishedContent>, IEnumerable<T>> mapper, float fuzzy = 0.5f, bool wildCard = true, string nodeTypeAlias = null)
        {
            nodeTypeAlias = nodeTypeAlias ?? typeof(T).Name;

            var words = term.Split(_splitTermCharacter);
            var query = words.Select(s => s.Fuzzy(fuzzy));
            if (wildCard)
            {
                query = query.Concat(words.Select(s => s.MultipleCharacterWildcard()));
            }

            var search = _examine.CreateSearchCriteria();
            var q = search.NodeTypeAlias(nodeTypeAlias).And().GroupedOr(properties, query.ToArray());

            return Search<T>(q.Compile(), mapper);
        }

        /// <summary>
        /// Search for the cirteria and returns an IEnumerable of T
        /// </summary>
        /// <param name="criteria">Criteria to search for</param>
        /// <returns></returns>
        public IEnumerable<T> Search<T>(ISearchCriteria criteria)
        {
            return Search<T>(criteria, Mapper.Map<IEnumerable<T>>);
        }

        /// <summary>
        /// Search for the cirteria and returns an IEnumerable of T
        /// </summary>
        /// <param name="criteria">Criteria to search for</param>
        /// <param name="mapper">Function mapping the IPublishedContent to the expected out Type T</param>
        /// <returns></returns>
        public IEnumerable<T> Search<T>(ISearchCriteria criteria, Func<IEnumerable<IPublishedContent>, IEnumerable<T>> mapper)
        {
            return mapper(_umbracoHelper.TypedSearch(criteria));
        }

        /// <summary>
        /// Search for the term and returns an IEnumerable of IPublishedContent
        /// </summary>
        /// <param name="term">Term to search</param>
        /// <returns></returns>
        public IEnumerable<IPublishedContent> Search(string term)
        {
            return _umbracoHelper.TypedSearch(term);
        }
    }
}
