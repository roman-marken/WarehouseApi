namespace WarehouseApi.Common.Exceptions;

public class NotFoundException(string message) : Exception(message) { }
public class ConflictException(string message) : Exception(message) { }
