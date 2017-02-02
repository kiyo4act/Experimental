using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace WpfApplication1
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<Device> ConnectedDeviceCollection { get; }

        public MainWindow()
        {
            InitializeComponent();
            ConnectedDeviceCollection = new ObservableCollection<Device>();
            this.DataContext = ConnectedDeviceCollection;
            Server.Instance.ClientConnected += (sender, device) =>
            {
                listBox.Dispatcher.Invoke(() =>
                {
                    lock (ConnectedDeviceCollection)
                    {
                        ConnectedDeviceCollection.Add(device);
                    }
                });
            };
            Server.Instance.ClientDisconnected += (sender, device) =>
            {
                listBox.Dispatcher.Invoke(() =>
                {
                    lock (ConnectedDeviceCollection)
                    {
                        ConnectedDeviceCollection.RemoveAll(x => x.DeviceId == device.DeviceId);
                    }
                });
            };
            Server.Instance.ClientDisconnectedAll += (sender, args) =>
            {
                listBox.Dispatcher.Invoke(() =>
                {
                    lock (ConnectedDeviceCollection)
                    {
                        ConnectedDeviceCollection.Clear();
                    }
                });
            };
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            SessionManager.Instance.AddSession();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {

            Device device = listBox.SelectedItem as Device;
            if (device != null) Server.Instance.Disconnect(device);
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            Server.Instance.DisconnectAll();
        }
    }

    public class Server
    {
        #region Singleton Pattern
        private static Server _instance;

        public static Server Instance
        {
            get
            {
                if (Server._instance == null)
                {
                    Server._instance = new Server();
                }
                return Server._instance;
            }
        }
        private Server()
        {
            SessionManager.Instance.Sessions.CollectionChanged += SessionsOnCollectionChanged;
        }

        #endregion

        public EventHandler<Device> ClientConnected;
        public EventHandler<Device> ClientDisconnected;
        public EventHandler ClientDisconnectedAll;
        private void SessionsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            switch (notifyCollectionChangedEventArgs.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Session newSession in notifyCollectionChangedEventArgs.NewItems)
                    {
                        ClientConnected?.Invoke(this, newSession.Device);
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (Session oldSession in notifyCollectionChangedEventArgs.OldItems)
                    {
                        ClientDisconnected?.Invoke(this, oldSession.Device);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    ClientDisconnectedAll?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }

        public void Disconnect(Device device)
        {
            SessionManager.Instance.Sessions.RemoveAll(x => x.Device.DeviceId == device.DeviceId);
        }

        internal void DisconnectAll()
        {
            SessionManager.Instance.DisconectAll();
        }
    }

    public class SessionManager
    {
        #region Singleton Pattern
        private static SessionManager _instance;

        internal static SessionManager Instance
        {
            get
            {
                if (SessionManager._instance == null)
                {
                    SessionManager._instance = new SessionManager();
                }
                return SessionManager._instance;
            }
        }
        private SessionManager()
        {
            Sessions = new ObservableCollection<Session>();
            Sessions.CollectionChanged += SessionsOnCollectionChanged;
        }

        #endregion

        internal ObservableCollection<Session> Sessions { get; set; }

        private void SessionsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            switch (notifyCollectionChangedEventArgs.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Session newSession in notifyCollectionChangedEventArgs.NewItems)
                    {
                        newSession.StartCommunicationLoop();
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (Session oldSession in notifyCollectionChangedEventArgs.OldItems)
                    {
                        oldSession.Dispose();
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    break;
            }
        }

        public void AddSession()
        {
            Session session = new Session();
            session.SessionDestroyed += Session_SessionDestroyed;
            Sessions.Add(session);
        }

        private void Session_SessionDestroyed(object sender, Device e)
        {
            lock (Sessions)
            {
                Sessions.RemoveAll(x => x.Device.DeviceId == e.DeviceId);
            }
        }

        internal void DisconectAll()
        {
            lock (Sessions)
            {
                Sessions.ForAll(x => x.Dispose());
                Sessions.Clear();
            }
        }
    }

    public class Session : IDisposable
    {
        internal Device Device { get; set; }

        public event EventHandler<Device> SessionDestroyed;
        public Session()
        {
            Device = new Device(DateTime.Now.ToLongTimeString(), Device.ConnectMethodEnum.Bluetooth);
        }

        internal Session StartCommunicationLoop()
        {
            Task.Run((() =>
            {
                Debug.WriteLine("Sleep Start");
                Thread.Sleep(5000);
                Debug.WriteLine("Sleep End");
                SessionDestroyed?.Invoke(this, Device);
                Dispose();
            }));
            return this;
        }

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージ状態を破棄します (マネージ オブジェクト)。
                }

                // TODO: アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。

                disposedValue = true;
            }
        }

        // TODO: 上の Dispose(bool disposing) にアンマネージ リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~Session() {
        //   // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
        //   Dispose(false);
        // }

        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(true);
            // TODO: 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
            // GC.SuppressFinalize(this);
        }
        #endregion

    }

    public class Device
    {
        public enum ConnectMethodEnum { Bluetooth, Tcp }
        public string DeviceName { get; }
        public ConnectMethodEnum ConnectMethod { get; }
        public Guid DeviceId { get; }

        public Device(string deviceName, ConnectMethodEnum connectMethod)
        {
            DeviceName = deviceName;
            ConnectMethod = connectMethod;
            DeviceId = Guid.NewGuid();
        }
    }

    public static class ExtensionMethods
    {
        public static int RemoveAll<T>(
            this ObservableCollection<T> coll, Func<T, bool> condition)
        {
            var itemsToRemove = coll.Where(condition).ToList();

            foreach (var itemToRemove in itemsToRemove)
            {
                coll.Remove(itemToRemove);
            }

            return itemsToRemove.Count;
        }

        public static void ForAll<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            foreach (T item in sequence) action(item);
        }
    }
}
