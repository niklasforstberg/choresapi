namespace ChoresApi.Models.DTOs.Chore;

public record ChoreDto(int Id, string Name, int? WeeklyTarget, int SortOrder);

public record SaveChoreDto(string Name, int? WeeklyTarget, int SortOrder);

public record ChoreDayEntryDto(int Id, int ChoreId, int UserId, DateOnly Date);

public record CreateChoreDayEntryDto(int ChoreId, DateOnly Date);

public record ChoreCompletionDto(int Id, int ChoreId, int UserId, DateOnly Date, int? RecordedById);

// UserId is who did the chore; null for the caller.
public record CreateChoreCompletionDto(int ChoreId, DateOnly Date, int? UserId);
