using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Sanford.Multimedia;
using Sanford.Multimedia.Midi;

namespace ReactiveMidi.Core
{
    public class MidiInDevice
    {
        public static int DeviceId { get; set; }
        public static MidiInCaps Caps => InputDevice.GetDeviceCapabilities(DeviceId);
    }

    public static class ReactiveInputDevice
    {
        public static IObservable<IMidiMessage> AsObservable(this InputDevice rs)
        {
            return Observable.Create<IMidiMessage>(s =>
            {
                rs.StartRecording();
                return new CompositeDisposable(
                    rs.AsObservable2().Subscribe(s),
                    Disposable.Create(rs.StopRecording)
                    );
            });
        }

        public static IObservable<IMidiMessage> AsObservable2(this InputDevice rs)
        {
            var error =
                Observable.FromEventPattern<ErrorEventArgs>(a => rs.Error += a, r => rs.Error -= r)
                    .Select(_ => Unit.Default);
            var channelMessages =
                Observable.FromEventPattern<ChannelMessageEventArgs>(a => rs.ChannelMessageReceived += a,
                    r => rs.ChannelMessageReceived -= r).Select(ea => (IMidiMessage)ea.EventArgs.Message);
            var meta =
                Observable.FromEventPattern<SysCommonMessageEventArgs>(a => rs.SysCommonMessageReceived += a,
                    r => rs.SysCommonMessageReceived -= r).Select(ea => (IMidiMessage)ea.EventArgs.Message);
            var sysEx =
                Observable.FromEventPattern<SysExMessageEventArgs>(a => rs.SysExMessageReceived += a,
                    r => rs.SysExMessageReceived -= r).Select(ea => (IMidiMessage)ea.EventArgs.Message);
            var sysRt =
                Observable.FromEventPattern<SysRealtimeMessageEventArgs>(a => rs.SysRealtimeMessageReceived += a,
                    r => rs.SysRealtimeMessageReceived -= r).Select(ea => (IMidiMessage)ea.EventArgs.Message);
            var dataStreams = channelMessages.Merge(meta).Merge(sysEx).Merge(sysRt);
            return dataStreams.TakeUntil(error).Publish().RefCount();
        }
    }

    public static class ReactiveSequencer
    {
        public static IObservable<IMidiMessage> Create(Sequence seq)
        {
            return Observable
                .Using(() => new Sequencer(), rs => rs.AsObservable())
                .Publish()
                .RefCount();
        }

        public static IObservable<IMidiMessage> AsObservable(this Sequencer rs)
        {
            var pc =
                Observable.FromEventPattern(a => rs.PlayingCompleted += a, r => rs.PlayingCompleted -= r)
                    .Select(_ => Unit.Default);
            var disposed =
                Observable.FromEventPattern(a => rs.Disposed += a, r => rs.Disposed -= r)
                    .Select(_ => Unit.Default);
            var channelMessages =
                Observable.FromEventPattern<ChannelMessageEventArgs>(a => rs.ChannelMessagePlayed += a,
                    r => rs.ChannelMessagePlayed -= r).Select(ea => (IMidiMessage)ea.EventArgs.Message);
            var stopped =
                Observable.FromEventPattern<StoppedEventArgs>(a => rs.Stopped += a, r => rs.Stopped -= r)
                    .Select(_ => Unit.Default);
            var meta =
                Observable.FromEventPattern<MetaMessageEventArgs>(a => rs.MetaMessagePlayed += a,
                    r => rs.MetaMessagePlayed -= r).Select(ea => (IMidiMessage)ea.EventArgs.Message);
            var sysEx =
                Observable.FromEventPattern<SysExMessageEventArgs>(a => rs.SysExMessagePlayed += a,
                    r => rs.SysExMessagePlayed -= r).Select(ea => (IMidiMessage)ea.EventArgs.Message);
            var chased =
                Observable.FromEventPattern<ChasedEventArgs>(a => rs.Chased += a, r => rs.Chased -= r)
                    .Select(_ => Unit.Default);
            var errorStreams = stopped.Merge(disposed);
            var endStreams = pc;
            var dataStreams = channelMessages.Merge(meta).Merge(sysEx);
            return dataStreams.TakeUntil(endStreams).TakeUntil(errorStreams).Publish().RefCount();
        }

        public static IObservable<IMidiMessage> AsObservable(this InputDevice rs)
        {
            var channelMessages =
                Observable.FromEventPattern<ChannelMessageEventArgs>(a => rs.ChannelMessageReceived += a,
                    r => rs.ChannelMessageReceived -= r).Select(ea => (IMidiMessage)ea.EventArgs.Message);
            var meta =
                Observable.FromEventPattern<SysCommonMessageEventArgs>(a => rs.SysCommonMessageReceived += a,
                    r => rs.SysCommonMessageReceived -= r).Select(ea => (IMidiMessage)ea.EventArgs.Message);
            var sysEx =
                Observable.FromEventPattern<SysExMessageEventArgs>(a => rs.SysExMessageReceived += a,
                    r => rs.SysExMessageReceived -= r).Select(ea => (IMidiMessage)ea.EventArgs.Message);
            var sysRealtime =
                Observable.FromEventPattern<SysRealtimeMessageEventArgs>(a => rs.SysRealtimeMessageReceived += a, r => rs.SysRealtimeMessageReceived -= r)
                    .Select(ea => (IMidiMessage)ea.EventArgs.Message);
            var dataStreams = channelMessages.Merge(meta).Merge(sysEx).Merge(sysRealtime);
            return dataStreams.Publish().RefCount();
        }
    }
}
