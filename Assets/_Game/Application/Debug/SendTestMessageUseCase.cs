using MessagePipe;
using Game.Domain.DTOs.Debug;

namespace Game.Application.Debug
{
    public sealed class SendTestMessageUseCase
    {
        private readonly IPublisher<TestMessageDTO> _publisher;

        public SendTestMessageUseCase(IPublisher<TestMessageDTO> publisher)
        {
            _publisher = publisher;
        }

        public void Execute(string message)
        {
            _publisher.Publish(new TestMessageDTO(message));
        }
    }
}
