using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Speech.V1Beta1;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Auth;
using NAudio.Wave;
using System.Speech.Synthesis;
using NAudio.CoreAudioApi;

namespace GoogleTranscribeStreamingDemo
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private static int SAMPLING_RATE = 16000;
        private static int SAMPLING_BITS = 16;
        private static int SAMPLING_CHANNELS = 1;

        private IWaveIn _waveIn;
        private Speech.SpeechClient _client;
        private AsyncDuplexStreamingCall<StreamingRecognizeRequest, StreamingRecognizeResponse> _call;
        private Task _responseRenderTask;
        private StreamingResultsDataCollection _data = new StreamingResultsDataCollection();
        private DispatcherTimer _timer;
        private TimeSpan _countTimeSpan;
        public event EventHandler EndOfAudioEvent;
        private bool _canWrite = false;

        public MainWindow()
        {
            InitializeComponent();
            LoadWasapiDevicesCombo();
            InitializeWaveIn();
            InitializeSpeechClient();
            ButtonStopRecord.IsEnabled = false;
            _timer = new DispatcherTimer();
            _timer.Tick += Timer_Tick;
            _timer.Interval = new TimeSpan(0,0,1);
            _countTimeSpan = TimeSpan.Zero;
            EndOfAudioEvent += OnEndOfAudioEvent;
        }

        private void LoadWasapiDevicesCombo()
        {
            var deviceEnum = new MMDeviceEnumerator();
            var devices = deviceEnum.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();

            ComboBoxRecordingDevice.DataContext = devices;
            ComboBoxRecordingDevice.DisplayMemberPath = "FriendlyName";
            ComboBoxRecordingDevice.SelectedIndex = 0;
        }

        private async void OnEndOfAudioEvent(object sender, EventArgs eventArgs)
        {
            Debug.WriteLine(nameof(OnEndOfAudioEvent) + ": Start");
            try
            {
                await _call.RequestStream.CompleteAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(nameof(OnEndOfAudioEvent) + ": "+ex.Message);
            }
            await _responseRenderTask;
            ResetRecognition();
            StartRecognition();
            Debug.WriteLine(nameof(OnEndOfAudioEvent) + ": End");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RouteGrid.DataContext = _data;
        }
        private void InitializeSpeechClient()
        {
            GoogleCredential credential = Task.Run(GoogleCredential.GetApplicationDefaultAsync).Result;

            if (credential.IsCreateScopedRequired)
            {
                credential = credential.CreateScoped("https://www.googleapis.com/auth/cloud-platform");
            }

            Channel channel = new Channel("speech.googleapis.com", 443, credential.ToChannelCredentials());

            _client = new Speech.SpeechClient(channel);
        }
        private void InitializeWaveIn()
        {
            if (_waveIn != null)
            {
                _waveIn.StopRecording();
                _waveIn.DataAvailable -= OnDataAvailable;
                _waveIn.RecordingStopped -= OnRecordingStopped;
                _waveIn.Dispose();
            }
            _waveIn = new WaveInEvent { WaveFormat = new WaveFormat(SAMPLING_RATE, SAMPLING_BITS, SAMPLING_CHANNELS) };
            //_waveIn = new WasapiCapture((MMDevice)ComboBoxRecordingDevice.SelectedItem);
            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStopped;
        }

        private StreamingRecognizeRequest ConfigRequestFactory(string languageCode, RecognitionConfig.Types.AudioEncoding encoding, int sampleRate)
        {
            return new StreamingRecognizeRequest()
            {
                StreamingConfig = new StreamingRecognitionConfig()
                {
                    Config = new RecognitionConfig()
                    {
                        LanguageCode = languageCode,
                        Encoding = encoding,
                        SampleRate = sampleRate,
                    },
                    InterimResults = true,
                    SingleUtterance = true
                }
            };
        }

        private async void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            //Debug.WriteLine(nameof(OnDataAvailable) + ": Start");

            var audioRequest = new StreamingRecognizeRequest()
            {
                AudioContent = RecognitionAudio.FromBytes(e.Buffer, 0, e.BytesRecorded).Content
            };
            try
            {
                if (_call != null && _canWrite)
                {
                    await _call.RequestStream.WriteAsync(audioRequest);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(nameof(OnDataAvailable) + ": Failed send data" + ex.Message);
            }
            //Debug.WriteLine(nameof(OnDataAvailable) + ": End");
        }

        private async void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            Debug.WriteLine(nameof(OnRecordingStopped) + ": Start");
            try
            {
                await _call.RequestStream.CompleteAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(nameof(OnEndOfAudioEvent) + ": " + ex.Message);
            }
            await _responseRenderTask;
            _call.Dispose();
            _timer.Stop();
            ButtonStartRecord.IsEnabled = true;
            Debug.WriteLine(nameof(OnRecordingStopped) + ": Start");
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _countTimeSpan += TimeSpan.FromSeconds(1);
            LabelTime.Content = _countTimeSpan;
        }
        private void ButtonStartRecord_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(nameof(OnRecordingStopped) + ": Start");
            ButtonStartRecord.IsEnabled = false;

            ResetRecognition();

            _waveIn.StartRecording();
            _countTimeSpan = TimeSpan.Zero;
            _timer.Start();

            StartRecognition();

            LabelState.Content = AppStatus.Recording.ObtainStatus();
            ButtonStopRecord.IsEnabled = true;
            Debug.WriteLine(nameof(OnRecordingStopped) + ": End");
        }

        private async void ResetRecognition()
        {
            Debug.WriteLine(nameof(ResetRecognition) + ": Start");
            _canWrite = false;
            _call?.Dispose();
            _call = _client.StreamingRecognize();
            await ComboBoxLanguage.Dispatcher.BeginInvoke(new Action((async () =>
            {
                try
                {
                    await
                        _call.RequestStream.WriteAsync(ConfigRequestFactory(ComboBoxLanguage.Text,
                            RecognitionConfig.Types.AudioEncoding.Linear16, SAMPLING_RATE));
                    _canWrite = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(nameof(ResetRecognition) + ": "+ex.Message);
                }
            })));
            Debug.WriteLine(nameof(ResetRecognition) + ": End");
        }
        private void StartRecognition()
        {
            Debug.WriteLine(nameof(StartRecognition) + ": Start");
            _responseRenderTask = Task.Run(async () =>
            {
                Debug.WriteLine(nameof(_responseRenderTask) + ": Start");
                try
                {
                    while (await _call.ResponseStream.MoveNext())
                    {
                        await TextBlockResult.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            TextBlockResult.Text += _call.ResponseStream.Current.Results + Environment.NewLine;
                            TextBlockResult.Text += _call.ResponseStream.Current.EndpointerType + Environment.NewLine;
                            _data.Clear();
                        }));

                        foreach (var result in _call.ResponseStream.Current.Results)
                        {
                            bool isFinal = result.IsFinal;
                            foreach (var alternative in result.Alternatives)
                            {
                                string transcript = alternative.Transcript;
                                float confidence = alternative.Confidence;
                                await TextBlockResult.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    _data.Add(new StreamingResultsData()
                                    {
                                        Transcript = transcript,
                                        Confidence = confidence,
                                        IsFinal = isFinal
                                    });
                                    LabelTranscript.Content = transcript;
                                    LabelConfidence.Content = Math.Abs(confidence) <= 0
                                        ? "N/A"
                                        : confidence.ToString(CultureInfo.InvariantCulture);
                                    LabelIsFinal.Content = isFinal.ToString();
                                    TextBlockResult.Text += transcript + Environment.NewLine;
                                }));
                            }
                        }
                        if (_call.ResponseStream.Current.EndpointerType ==
                            StreamingRecognizeResponse.Types.EndpointerType.EndOfAudio)
                        {
                            if (EndOfAudioEvent != null) EndOfAudioEvent(this, new EventArgs());
                        }
                    }
                    Debug.WriteLine(nameof(_responseRenderTask) + ": End");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(nameof(StartRecognition) + ": " + ex.Message);
                }
            });
            Debug.WriteLine(nameof(StartRecognition) + ": End");
        }

        private void ButtonStopRecord_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(nameof(ButtonStopRecord_Click) + ": Start");
            ButtonStopRecord.IsEnabled = false;
            _waveIn.StopRecording();
            LabelState.Content = AppStatus.Finished.ObtainStatus();
            Debug.WriteLine(nameof(ButtonStopRecord_Click) + ": End");
        }

        private void ButtonSpeech_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(nameof(ButtonSpeech_Click) + ": Start");
            var synth = new SpeechSynthesizer();
            synth.SpeakAsync(TextBoxSpeech.Text);
            TextBoxSpeech.Text = String.Empty;
            Debug.WriteLine(nameof(ButtonSpeech_Click) + ": End");
        }
    }
    public enum AppStatus
    {
        Null,
        Initialized,
        Recording,
        Finished
    }
    public static class AppStatusExt
    {
        public static string ObtainStatus(this AppStatus value)
        {
            string[] values = { "Null", "Initialized", "Recording", "Finished" };
            return values[(int)value];
        }
    }
}
