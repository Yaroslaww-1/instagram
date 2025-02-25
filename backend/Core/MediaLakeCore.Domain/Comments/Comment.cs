﻿using MediaLakeCore.BuildingBlocks.Domain;
using MediaLakeCore.Domain.Comments.Events;
using MediaLakeCore.Domain.Posts;
using MediaLakeCore.Domain.Users;
using System;

namespace MediaLakeCore.Domain.Comments
{
    public class Comment : Entity, IAggregateRoot
    {
        public CommentId Id { get; private set; }
        public PostId PostId { get; private set; }
        public User CreatedBy { get; private set; }
        public string Content { get; private set; }
        public int LikesCount { get; private set; }
        public int DislikesCount { get; private set; }

        private Comment()
        {
            // Only for EF
        }

        private Comment(User createdBy, PostId postId, string content)
        {
            Id = new CommentId(Guid.NewGuid());
            CreatedBy = createdBy;
            PostId = postId;
            Content = content;
            LikesCount = 0;
            DislikesCount = 0;

            AddDomainEvent(new CommentCreatedDomainEvent(this));
        }

        public static Comment CreateNew(User user, PostId postId, string content)
        {
            return new Comment(user, postId, content);
        }

        public void AddNewLike() => LikesCount++;
        public void RemoveExistingLike() => LikesCount--;

        public void AddNewDislike() => DislikesCount++;
        public void RemoveExistingDislike() => DislikesCount--;
    }
}
