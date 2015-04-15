/*
  ReviewBot 0.1
  Copyright (c) Microsoft Corporation
  All rights reserved. 
  
  MIT License
  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the ""Software""), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
  The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
  THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.ReviewBot.Github
{
    class SearchResponse
    {
        public string total_count;
        public string incomplete_results;
        public Result[] items;
    }
    class Result
    {
        public string id;
        public string name;
        public string full_name;
        public Owner owner;
        public bool isPrivate;
        public string html_url;
      	public string description;
      	public string fork;
      	public string url;
      	public string forks_url;
      	public string keys_url;
      	public string collaborators_url;
      	public string teams_url;
      	public string hooks_url;
      	public string issue_events_url;
      	public string events_url;
      	public string assignees_url;
      	public string branches_url;
      	public string tags_url;
      	public string blobs_url;
      	public string git_tags_url;
      	public string git_refs_url;
      	public string trees_url;
      	public string statuses_url;
      	public string languages_url;
      	public string stargazers_url;
      	public string contributors_url;
      	public string subscribers_url;
      	public string subscription_url;
      	public string commits_url;
      	public string git_commits_url;
      	public string comments_url;
      	public string issue_comment_url;
      	public string contents_url;
      	public string compare_url;
      	public string merges_url;
      	public string archive_url;
      	public string downloads_url;
      	public string issues_url;
      	public string pulls_url;
      	public string milestones_url;
      	public string notifications_url;
      	public string labels_url;
      	public string releases_url;
      	public string created_at;
      	public string updated_at;
      	public string pushed_at;
      	public string git_url;
      	public string ssh_url;
      	public string clone_url;
      	public string svn_url;
      	public string homepage;
      	public string size;
      	public string stargazers_count;
      	public string watchers_count;
      	public string language;
      	public string has_issues;
      	public string has_downloads;
      	public string has_wiki;
      	public string forks_count;
      	public string mirror_url;
      	public string open_issues_count;
      	public string forks;
      	public string open_issues;
      	public string watchers;
      	public string default_branch;
      	public string score;

    }
    class Owner
    {
        public string login;
        public string id;
        public string avatar_url;
        public string gravatar_id;
        public string url;
        public string html_url;
        public string followers_url;
        public string following_url;
        public string gists_url;
        public string starred_url;
        public string subscriptions_url;
        public string organizations_url;
        public string repos_url;
        public string events_url;
        public string received_events_url;
        public string type;
        public string site_admin;
    }
}
