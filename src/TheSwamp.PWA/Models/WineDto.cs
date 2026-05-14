namespace TheSwamp.PWA.Models;

public record WineDto(
	long Id,
	string Display,
	string Producer,
	string Name,
	string Country,
	string Region,
	string Colour,
	string Type,
	string? Vintage
	);
