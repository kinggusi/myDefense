using System;
using MyDefense.Auth;
using UnityEngine;

namespace MyDefense.Clans
{
    public interface IClanTransport
    {
        long UserId {get;}
        bool IsCurrent {get;}
        void Send(string path,string method,string json,Action<AuthHttpResult> completed);
    }
    public sealed class AccountClanTransport : IClanTransport
    {
        private readonly MonoBehaviour owner;
        private readonly AuthSession session;
        private readonly int generation;
        private readonly string endpoint;
        public long UserId {get;}
        public AccountClanTransport(MonoBehaviour owner,AuthSession session,string endpoint)
        {this.owner=owner;this.session=session;this.endpoint=endpoint;generation=session?.Generation??0;UserId=session?.User?.userId??0;}
        public bool IsCurrent => owner!=null&&session!=null&&ReferenceEquals(session,AuthSession.Instance)&&session.IsAuthenticated
            &&session.Generation==generation&&session.User?.userId==UserId&&UserId>0&&session.ApiBaseUrl==endpoint
            &&NetworkManager.Instance!=null&&NetworkManager.Instance.BaseUrl.TrimEnd('/')==endpoint;
        public void Send(string path,string method,string json,Action<AuthHttpResult> completed)
        {
            if(!IsCurrent){completed(new AuthHttpResult{Status=401,Error="로그인이 필요합니다."});return;}
            owner.StartCoroutine(session.SendAccount(path,method,json,null,completed));
        }
    }
}
