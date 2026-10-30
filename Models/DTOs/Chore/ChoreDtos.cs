namespace ChoresApi.Models.DTOs.Chore;

public record ChoreDto(int Id, string Name, int? WeeklyTarget, int SortOrder);

public record SaveChoreDto(string Name, int? WeeklyTarget, int SortOrder);

public record ChoreEntryDto(int Id, int ChoreId, int UserId, DateOnly Date, bool Completed);

public record CreateChoreEntryDto(int ChoreId, DateOnly Date);

public record UpdateChoreEntryDto(bool Completed);
