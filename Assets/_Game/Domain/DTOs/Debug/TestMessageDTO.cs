namespace Game.Domain.DTOs.Debug
{
    public readonly struct TestMessageDTO
    {
        public string Message { get; }
        public TestMessageDTO(string message) => Message = message;
    }
}
