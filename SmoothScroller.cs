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
                if (obj is SmoothScroller scroller && Interlocked.CompareExchange(ref scroller.isanimating, 1, 1) == 1)
                {
                    scroller.panel.ScrollToVerticalOffset((double)value.NewValue);
                }
            }));
        }

        private double currentScrollValue;
        private readonly ScrollViewer panel;
        private int isanimating;

        private SmoothScroller()
        {
            this.currentScrollValue = 0d;
            this.isanimating = 0;
        }

        public SmoothScroller(ScrollViewer panel) : base()
        {
            this.panel = panel;
            Interlocked.Exchange(ref this.currentScrollValue, panel.VerticalOffset);
            this.SetValue(AnimatedScrollValueProperty, panel.VerticalOffset);
            this.panel.ScrollChanged += this.Panel_ScrollChanged;
            // this.GetGridFromPanel();
            // this.scrollBar.ValueChanged += ScrollBar_ValueChanged;
        }

        private void Panel_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (Interlocked.CompareExchange(ref this.isanimating, 1, 1) == 0)
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

            Interlocked.CompareExchange(ref this.isanimating, 1, 0);

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
                var clamped = value;
                if (clamped < 0)
                {
                    clamped = 0d;
                }
                else if (clamped > this.panel.ScrollableHeight)
                {
                    var max = this.panel.ScrollableHeight;
                    if (clamped > max)
                    {
                        clamped = max;
                    }
                }
                var oldvalue = Interlocked.Exchange(ref this.currentScrollValue, clamped);
                if (oldvalue != clamped)
                {
                    this.StartScroll(clamped);
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
            if (Interlocked.CompareExchange(ref this.isanimating, 0, 1) == 1)
            {
                this.BeginAnimation(AnimatedScrollValueProperty, null, HandoffBehavior.SnapshotAndReplace);
                this.SetValue(AnimatedScrollValueProperty, val);
                this.SetCurrentValue(AnimatedScrollValueProperty, val);
            }
        }

        public void Dispose()
        {
            this.StopAnimation();
        }
    }
}
