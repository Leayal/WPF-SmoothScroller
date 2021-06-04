using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media.Animation;

namespace Leayal.WPF
{
    public class SmoothScroller : UIElement, IDisposable
    {
        private static readonly CubicEase EaseOut;
        private static readonly DependencyProperty AnimatedScrollValueProperty;
        static SmoothScroller()
        {
            EaseOut = new CubicEase() { EasingMode = EasingMode.EaseOut };
            // EaseOut.Freeze();
            AnimatedScrollValueProperty = DependencyProperty.Register("AnimatedScrollValue", typeof(double), typeof(SmoothScroller), new UIPropertyMetadata(0d, (obj, value) =>
            {
                if (obj is SmoothScroller scroller && scroller.isanimating)
                {
                    scroller.panel.ScrollToVerticalOffset((double)value.NewValue);
                }
            }));
        }

        private double currentScrollValue;
        private readonly ScrollViewer panel;
        private volatile bool isanimating;

        private SmoothScroller()
        {
            this.currentScrollValue = 0d;
            this.isanimating = false;
        }

        public SmoothScroller(ScrollViewer panel) : base()
        {
            this.panel = panel;
            if (!this.isanimating)
            {
                Interlocked.Exchange(ref this.currentScrollValue, panel.VerticalOffset);
                this.SetValue(AnimatedScrollValueProperty, panel.VerticalOffset);
            }
            this.panel.ScrollChanged += this.Panel_ScrollChanged;
            // this.GetGridFromPanel();
            // this.scrollBar.ValueChanged += ScrollBar_ValueChanged;
        }

        private void Panel_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!this.isanimating)
            {
                Interlocked.Exchange(ref this.currentScrollValue, e.VerticalOffset);
                this.SetValue(AnimatedScrollValueProperty, e.VerticalOffset);
            }
        }

        private static readonly Duration ScrollDuration = new Duration(TimeSpan.FromMilliseconds(500));

        public void StartScroll(double val)
        {
            DoubleAnimation verticalAnimation = new DoubleAnimation();
            verticalAnimation.EasingFunction = EaseOut;

            verticalAnimation.To = val;
            verticalAnimation.Duration = ScrollDuration;
            verticalAnimation.Completed += Clock_Completed;
            verticalAnimation.FillBehavior = FillBehavior.HoldEnd;

            this.isanimating = true;

            this.BeginAnimation(AnimatedScrollValueProperty, verticalAnimation, HandoffBehavior.Compose);

            //Storyboard storyboard = new Storyboard();

            //storyboard.Children.Add(verticalAnimation);
            //Storyboard.SetTarget(verticalAnimation, this.scrollBar);
            //Storyboard.SetTargetProperty(this.scrollBar, new PropertyPath(ScrollBar.ValueProperty)); // Attached dependency property
            //storyboard.Completed += Clock_Completed;
            //storyboard.Begin();

            // DoubleAnimation anim = new DoubleAnimation(val, ScrollDuration, FillBehavior.HoldEnd) { EasingFunction = EaseOut };
            // anim.Freeze();
            // sb.Children.Add(anim);
            // sb.Completed += Clock_Completed;
            // sb.BeginAnimation(AnimatedScrollValueProperty, anim, HandoffBehavior.Compose);
            // anim.ApplyAnimationClock(AnimatedScrollValueProperty, sb, HandoffBehavior.Compose);
        }

        public double ScrollOffset
        {
            get
            {
                return Interlocked.CompareExchange(ref this.currentScrollValue, 0d, 0d);
            }
            set
            {
                var oldval = Interlocked.CompareExchange(ref this.currentScrollValue, 0d, 0d);
                if (oldval != value)
                {
                    if (value < 0)
                    {
                        this.currentScrollValue = 0;
                    }
                    else if (value > this.panel.ScrollableHeight)
                    {
                        this.currentScrollValue = this.panel.ScrollableHeight;
                    }
                    else
                    {
                        this.currentScrollValue = value;
                    }
                    this.StartScroll(this.currentScrollValue);
                }
            }
        }

        private void Clock_Completed(object sender, EventArgs e)
        {
            if (sender is AnimationClock clock)
            {
                var animating = (double)this.GetValue(AnimatedScrollValueProperty);
                var oldValue = Interlocked.CompareExchange(ref animating, 0, this.currentScrollValue);
                if (oldValue == this.currentScrollValue)
                {
                    this.StopAnimation(oldValue);
                }
                clock.Completed -= Clock_Completed;
            }
        }

        public void StopAnimation() => this.StopAnimation(this.ScrollOffset);

        private void StopAnimation(double val)
        {
            // this.SetCurrentValue(AnimatedScrollValueProperty, val);
            this.BeginAnimation(AnimatedScrollValueProperty, null, HandoffBehavior.SnapshotAndReplace);
            this.SetValue(AnimatedScrollValueProperty, val);
            // this.SetCurrentValue(AnimatedScrollValueProperty, val);
            this.isanimating = false;
        }

        public void Dispose()
        {
            this.StopAnimation();
        }
    }
}
