/* 
    Copyright (c) 2012 - 2013 Microsoft Corporation.  All rights reserved.
    Use of this sample source code is subject to the terms of the Microsoft license 
    agreement under which you licensed this sample source code and is provided AS-IS.
    If you did not accept the terms of the license agreement, you are not authorized 
    to use this sample source code.  For the terms of the license, please see the 
    license agreement between you and Microsoft.
  
    To see all Code Samples for Windows Phone, visit http://code.msdn.microsoft.com/wpapps
  
*/

using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Telegram.MTProto;
using Telegram.Resources;
using Telegram.Utils;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;

namespace sdkImages.Scenarios {
    public partial class PinchAndZoom : PhoneApplicationPage {
        private bool menuClicked = false;

        const double MaxScale = 10;
        private bool doubleTap = false;

        double _scale = 1.0;
        double _minScale;
        double _coercedScale;
        double _originalScale;

        Size _viewportSize;
        bool _pinching;
        Point _screenMidpoint;
        Point _relativeMidpoint;

        BitmapImage _bitmap;
        private MessageMedia media;

        private bool downloaded = false;
        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
        }

        public PinchAndZoom() {
            InitializeComponent();

            media = MediaTransitionHelper.Instance.Media;

            if (media.Constructor == Constructor.messageMediaPhoto) {
                ImageViewportElement.Visibility = Visibility.Visible;
                VideoPlayerElement.Visibility = Visibility.Collapsed;
            }

            if (media.Constructor == Constructor.messageMediaVideo) {
                ImageViewportElement.Visibility = Visibility.Collapsed;
                VideoPlayerElement.Visibility = Visibility.Visible;

                MessageMediaVideoConstructor cons = (MessageMediaVideoConstructor) media;
                PlaybackControls.Visibility = Visibility.Visible;
            }

//            BuildLocalizedApplicationBar();
        }



        /// <summary> 
        /// Either the user has manipulated the image or the size of the viewport has changed. We only 
        /// care about the size. 
        /// </summary> 
        void viewport_ViewportChanged(object sender, System.Windows.Controls.Primitives.ViewportChangedEventArgs e) {
            Size newSize = new Size(ImageViewportElement.Viewport.Width, ImageViewportElement.Viewport.Height);
            if (newSize != _viewportSize) {
                _viewportSize = newSize;
                CoerceScale(true);
                ResizeImage(false);
            }
        }

        /// <summary> 
        /// Handler for the ManipulationStarted event. Set initial state in case 
        /// it becomes a pinch later. 
        /// </summary> 
        void OnManipulationStarted(object sender, ManipulationStartedEventArgs e) {
            _pinching = false;
            _originalScale = _scale;
        }

        /// <summary> 
        /// Handler for the ManipulationDelta event. It may or may not be a pinch. If it is not a  
        /// pinch, the ViewportControl will take care of it. 
        /// </summary> 
        /// <param name="sender"></param> 
        /// <param name="e"></param> 
        void OnManipulationDelta(object sender, ManipulationDeltaEventArgs e) {
            if (e.PinchManipulation != null) {
                e.Handled = true;

                if (!_pinching) {
                    _pinching = true;
                    Point center = e.PinchManipulation.Original.Center;
                    _relativeMidpoint = new Point(center.X / ImageElement.ActualWidth, center.Y / ImageElement.ActualHeight);

                    var xform = ImageElement.TransformToVisual(ImageViewportElement);
                    _screenMidpoint = xform.Transform(center);
                }

                _scale = _originalScale * e.PinchManipulation.CumulativeScale;

                CoerceScale(false);
                ResizeImage(false);
            } else if (_pinching) {
                _pinching = false;
                _originalScale = _scale = _coercedScale;
            }
        }

        /// <summary> 
        /// The manipulation has completed (no touch points anymore) so reset state. 
        /// </summary> 
        void OnManipulationCompleted(object sender, ManipulationCompletedEventArgs e) {
            _pinching = false;
            _scale = _coercedScale;
        }


        /// <summary> 
        /// When a new image is opened, set its initial scale. 
        /// </summary> 
        void OnImageOpened(object sender, RoutedEventArgs e) {
            _bitmap = (BitmapImage)ImageElement.Source;

            // Set scale to the minimum, and then save it. 
            _scale = 0;
            CoerceScale(true);
            _scale = _coercedScale;

            ResizeImage(true);
        }

        /// <summary> 
        /// Adjust the size of the image according to the coerced scale factor. Optionally 
        /// center the image, otherwise, try to keep the original midpoint of the pinch 
        /// in the same spot on the screen regardless of the scale. 
        /// </summary> 
        /// <param name="center"></param> 
        void ResizeImage(bool center) {
            if (_coercedScale != 0 && _bitmap != null) {
                double newWidth = canvas.Width = Math.Round(_bitmap.PixelWidth * _coercedScale);
                double newHeight = canvas.Height = Math.Round(_bitmap.PixelHeight * _coercedScale);

                xform.ScaleX = xform.ScaleY = _coercedScale;

                ImageViewportElement.Bounds = new Rect(0, 0, newWidth, newHeight);

                if (center) {
                    ImageViewportElement.SetViewportOrigin(
                        new Point(
                            Math.Round((newWidth - ImageViewportElement.ActualWidth) / 2),
                            Math.Round((newHeight - ImageViewportElement.ActualHeight) / 2)
                            ));
                } else {
                    Point newImgMid = new Point(newWidth * _relativeMidpoint.X, newHeight * _relativeMidpoint.Y);
                    Point origin = new Point(newImgMid.X - _screenMidpoint.X, newImgMid.Y - _screenMidpoint.Y);
                    ImageViewportElement.SetViewportOrigin(origin);
                }
            }
        }

        /// <summary> 
        /// Coerce the scale into being within the proper range. Optionally compute the constraints  
        /// on the scale so that it will always fill the entire screen and will never get too big  
        /// to be contained in a hardware surface. 
        /// </summary> 
        /// <param name="recompute">Will recompute the min max scale if true.</param> 
        void CoerceScale(bool recompute) {
            if (recompute && _bitmap != null && ImageViewportElement != null) {
                // Calculate the minimum scale to fit the viewport 
                double minX = ImageViewportElement.ActualWidth / _bitmap.PixelWidth;
                double minY = ImageViewportElement.ActualHeight / _bitmap.PixelHeight;

                _minScale = Math.Min(minX, minY);
            }

            _coercedScale = Math.Min(MaxScale, Math.Max(_scale, _minScale));

        }

        private void OnDoubleTap(object sender, GestureEventArgs e) {
            e.Handled = true;
            _originalScale = _scale;

            Point center = e.GetPosition(ImageElement);
            _relativeMidpoint = new Point(center.X / ImageElement.ActualWidth, center.Y / ImageElement.ActualHeight);

            var xform = ImageElement.TransformToVisual(ImageViewportElement);
            _screenMidpoint = xform.Transform(center);

            if (doubleTap) {
                _scale = 0;
                doubleTap = false;
            } else {
                doubleTap = true;
                _scale = _originalScale * 3;

            }
            CoerceScale(false);
            ResizeImage(false);
        }

        private void OnSendClick(object sender, RoutedEventArgs e) {
            throw new NotImplementedException();
        }

        private void OnSaveClick(object sender, RoutedEventArgs e) {
            throw new NotImplementedException();
        }

        private void OnShareClick(object sender, RoutedEventArgs e) {
            throw new NotImplementedException();
        }

        private void OnBrowseClick(object sender, RoutedEventArgs e) {
            throw new NotImplementedException();
        }

        private void OnExpandMenuTap(object sender, GestureEventArgs e) {
            if (menuClicked) {
                menuClicked = false;
                AppbarDown.Begin();
            } else {
                menuClicked = true;
                AppbarUp.Begin();
            }
        }

        private void OnDownloadOrPlayClick(object sender, RoutedEventArgs e) {
            if (media == null)
                return;

            if (!downloaded) {
                PlaybackButton.IsEnabled = false;
                PlaybackButton.Content = "downloading...";

                MessageMediaVideoConstructor mediaVideo = (MessageMediaVideoConstructor) media;

                Task.Run(() => TelegramSession.Instance.Files.DownloadVideo(mediaVideo.video, Handler)).ContinueWith(
                    (result) => {
                        Deployment.Current.Dispatcher.BeginInvoke(() => {
                            VideoPlayerElement.Source = new Uri(result.Result, UriKind.Absolute);
                            PlaybackButton.Content = "play";
                            PlaybackButton.IsEnabled = true;
                            PlaybackProgress.Visibility = Visibility.Collapsed;
                        });

                    });
            }
            else {
                VideoPlayerElement.Play();
                VideoPlayerElement.MediaEnded += delegate {
                    PlaybackProgress.Visibility = Visibility.Visible;
                };
            }
        }

        private void Handler(float progress) {
            Deployment.Current.Dispatcher.BeginInvoke(() => {
                PlaybackProgress.Value = progress * 100f;
            });
        }
    }
}
