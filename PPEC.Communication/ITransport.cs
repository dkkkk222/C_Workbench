using PPEC.Communication.Interface;
using System;

namespace PPEC.Communication
{
    public interface ITransport : IDisposable
    {
        int Retries { get; set; }

        uint RetryOnOldResponseThreshold { get; set; }

        bool SlaveBusyUsesRetryCount { get; set; }

        int WaitToRetryMilliseconds { get; set; }

        int ReadTimeout { get; set; }

        int WriteTimeout { get; set; }

        T UnicastMessage<T>(IMessage message) where T : IMessage, new();

        void Write(IMessage message);

        IStreamResource StreamResource { get; }
        void ValidateResponse(IMessage request, IMessage response);

        event Action<object> ReceiveDataChanged;

        event Action<object, IMessage> SendDataChanged;
    }
}
