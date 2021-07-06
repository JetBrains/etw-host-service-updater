using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using JetBrains.Annotations;
using JetBrains.DownloadPgpVerifier;
using JetBrains.Etw.HostService.Notifier.Progress;
using JetBrains.Etw.HostService.Notifier.Util;
using JetBrains.Etw.HostService.Notifier.ViewModel;
using ILogger = JetBrains.Etw.HostService.Notifier.Util.ILogger;

namespace JetBrains.Etw.HostService.Notifier
{
  internal partial class DownloadingWindow : Window
  {
    private const string SpecialContext = nameof(PgpSignaturesVerifier) + Logger.Delimiter + nameof(PgpSignaturesVerifier.Verify);
    private readonly CancellationTokenSource myCancellationTokenSource = new();
    private readonly ILogger myLogger;
    private string myMsiFile;

    public DownloadingWindow([NotNull] ILogger logger, [NotNull] UpdateRequest updateRequest, bool downloadDelay = false)
    {
      if (updateRequest == null) throw new ArgumentNullException(nameof(updateRequest));
      myLogger = logger ?? throw new ArgumentNullException(nameof(logger));

      InitializeComponent();
      var viewModel = new DownloadingViewModel();
      DataContext = viewModel;

      var deactivateSystemCloseButton = true;
      Closing += (_, args) =>
        {
          args.Cancel = deactivateSystemCloseButton;
          myCancellationTokenSource.Cancel();
        };

      var loggerContext = Logger.Context;

      async void DoAsync()
      {
        try
        {
          myMsiFile = await Task.Run(() => DownloadAndVerifyMsi(logger, updateRequest, viewModel, myCancellationTokenSource.Token, downloadDelay), myCancellationTokenSource.Token);
          logger.Info($"{loggerContext} res=downloaded");
        }
        catch (OperationCanceledException)
        {
          logger.Info($"{loggerContext} res=cancelled");
        }
        catch (Exception ex)
        {
          logger.Info($"{loggerContext} res=error");
          logger.Exception(ex);
          MessageBox.Show(ex.GetBaseException().Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
          deactivateSystemCloseButton = false;
          DialogResult = myMsiFile != null;
        }
      }

      DoAsync();
    }

    [NotNull]
    public string MsiFile => DialogResult == true ? myMsiFile : throw new InvalidOperationException();

    [NotNull]
    private static string DownloadAndVerifyMsi(ILogger logger,
      [NotNull] UpdateRequest updateRequest,
      [NotNull] IProgress mainProgress,
      CancellationToken ct,
      bool downloadDelay = false)
    {
      logger.Info($"{Logger.Context} size={updateRequest.Size}\n\tlink={updateRequest.Link}\n\tchecksumLink={updateRequest.ChecksumLink}\n\tsignedChecksumLink={updateRequest.SignedChecksumLink}");
      mainProgress.Start(100);
      var digestSha256 = updateRequest.ChecksumLink.OpenSeekableStreamFromWeb(dataStream =>
        {
          var verificationResult = PgpSignaturesVerifier.MasterPublicKey.OpenStreamFromString(masterPublicKeyStream =>
            PgpSignaturesVerifier.PublicKeysUri.OpenSeekableStreamFromWeb(publicKeysStream =>
              updateRequest.SignedChecksumLink.OpenSeekableStreamFromWeb(signaturesStream =>
                {
                  mainProgress.Advance(5);
                  return PgpSignaturesVerifier.Verify(masterPublicKeyStream, publicKeysStream, signaturesStream, dataStream, new SubLogger(logger, SpecialContext));
                })));
          mainProgress.Advance(4);

          if (!verificationResult)
            throw new InvalidOperationException("The PGP signature for SHA256 checksum file is incorrect");

          dataStream.Position = 0;
          using var reader = new BinaryReader(dataStream, Encoding.UTF8, false);
          return reader.ReadChars(2 * 256 / 8).FromHexString();
        });
      logger.Info($"{Logger.Context} digestSha256={digestSha256.ToLoverHexString()}");
      mainProgress.Advance(1);
      ct.ThrowIfCancellationRequested();

      var fileProgress = new SubProgress(mainProgress, 90);
      fileProgress.Start(updateRequest.Size);
      var downloadFile = Path.GetTempFileName();
      logger.Info($"{Logger.Context} downloadFile={downloadFile}");
      try
      {
        using var fileStream = File.Create(downloadFile);
        var fileSha256 = updateRequest.Link.OpenStreamFromWeb(stream =>
          {
            var buffer = new byte[4 * 1024];
            using var hasher = SHA256.Create();
            while (true)
            {
              var received = stream.Read(buffer, 0, buffer.Length);
              ct.ThrowIfCancellationRequested();
              if (received == 0)
              {
                hasher.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                ct.ThrowIfCancellationRequested();
                return hasher.Hash;
              }

              hasher.TransformBlock(buffer, 0, received, null, 0);
              ct.ThrowIfCancellationRequested();

              fileStream.Write(buffer, 0, received);
              ct.ThrowIfCancellationRequested();

              fileProgress.Advance(received);

              if (downloadDelay)
                Thread.Sleep(1);
            }
          }).NotNull();
        fileProgress.Stop();

        logger.Info($"{Logger.Context} fileSha256={fileSha256.ToLoverHexString()}");
        if (!digestSha256.SequenceEqual(fileSha256))
          throw new InvalidOperationException("The file has invalid SHA256 checksum");

        mainProgress.Stop();
        return downloadFile;
      }
      catch
      {
        if (File.Exists(downloadFile))
          File.Delete(downloadFile);
        throw;
      }
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
      myLogger.Info(Logger.Context);
      myCancellationTokenSource.Cancel();
    }

    private sealed class SubLogger : DownloadPgpVerifier.ILogger
    {
      private readonly string myContext;
      private readonly ILogger myLogger;

      public SubLogger([NotNull] ILogger logger, [NotNull] string context)
      {
        myLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        myContext = context ?? throw new ArgumentNullException(nameof(context));
      }

      public void Info(string str)
      {
        myLogger.Info($"{myContext} {str}");
      }

      public void Warning(string str)
      {
        myLogger.Warning($"{myContext} {str}");
      }

      public void Error(string str)
      {
        myLogger.Error($"{myContext} {str}");
      }
    }
  }
}