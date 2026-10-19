namespace my_project.Core.Models;

// ExpiresAt is UTC
public record AccessToken(string Token, DateTime ExpiresAt);
