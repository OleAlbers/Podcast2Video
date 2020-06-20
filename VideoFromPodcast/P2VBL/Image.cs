﻿using P2VEntities;

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;


namespace P2VBL
{
    public class Image
    {
        public Episode CurrentEpisode { get; set; }
        public Podcast Podcast { get; set; }
        private System.Drawing.Image _podcastImage;
        private P2VEntities.Config.Podcast _config = Config.Configuration.Podcast;

        private Bitmap _background = null;



        public Image(Podcast podcast, string episodeId = null)
        {
            Podcast = podcast;
            CurrentEpisode = Podcast.Episodes.First();
            if (episodeId != null) CurrentEpisode = Podcast.Episodes.First(q => q.Unique == episodeId);
            _podcastImage = DownloadImage();
        }

        

        private System.Drawing.Image DownloadImage()
        {
            var image = new WebClient().DownloadData(Podcast.Image);
            return new Bitmap(new MemoryStream(image));
        }

        private void DrawBackground(ref Graphics graphics)
        {
            graphics.FillRectangle(_config.Background.BrushValue,_config.Background.ToRectangle());  
        }

        private void DrawImage(ref Graphics graphics)
        {
            var targetRegion = _config.Icon.ToRectangle();
            var sourceRegion = new Rectangle(0, 0, _podcastImage.Width, _podcastImage.Height);
            graphics.DrawImage(_podcastImage, targetRegion, sourceRegion, GraphicsUnit.Pixel);
        }

        private void DrawBar(ref Graphics graphics)
        {
            graphics.FillRectangle(_config.Episode.Timeline.BrushValue, _config.Episode.Timeline.ToRectangle());
        }

        private void DrawTitle(ref Graphics graphics)
        {
            DrawCenteredText(ref graphics, _config.Title,Podcast.Title );
            DrawCenteredText(ref graphics, _config.Episode.Title, CurrentEpisode.Title);
        }

        private void DrawCenteredText(ref Graphics graphics, P2VEntities.Config.TextElement textelement, string text)
        {
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            StringFormat stringFormat = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            graphics.DrawString(text, new Font(textelement.Font.Name, textelement.Font.Size), textelement.BrushValue, new PointF(textelement.X,textelement.Y), stringFormat);
        }

        public Bitmap CreateBackgroundImage()
        {
            var emptyBackground = new Bitmap(_config.Background.Width, _config.Background.Height, PixelFormat.Format24bppRgb);
            var graphics = Graphics.FromImage(emptyBackground);
            DrawBackground(ref graphics);
            DrawBar(ref graphics);
            DrawTitle(ref graphics);
            DrawImage(ref graphics);
            graphics.Dispose();
            return emptyBackground;
        }

        private double GetRelativePosition(TimeSpan currentPosition)
        {
            return currentPosition.TotalMilliseconds / CurrentEpisode.Duration.TotalMilliseconds;
        }

        private void DrawBarPosition(ref Graphics graphics, TimeSpan position)
        {
            graphics.FillRectangle(_config.Episode.TimelineActive.BrushValue, new Rectangle( _config.Episode.Timeline.X, _config.Episode.Timeline.Y, (int)(_config.Episode.Timeline.Width * GetRelativePosition(position)), _config.Episode.Timeline.Height));
        }

        private Chapter GetNearChapter(Chapter currentChapter, int relativePosition)
        {
            if (currentChapter == null) return null;
            int currentChapterPosition = CurrentEpisode.Chapters.IndexOf(currentChapter);

            int absolutePosition = currentChapterPosition + relativePosition;
            if (absolutePosition < 0 || absolutePosition >= CurrentEpisode.Chapters.Count) return null;
            return CurrentEpisode.Chapters.ElementAt(absolutePosition);
        }

        private Chapter GetChapterAt(TimeSpan position)
        {
            if (CurrentEpisode.Chapters == null) return null;
            return CurrentEpisode.Chapters.OrderByDescending(q => q.Offset).FirstOrDefault(q => q.Offset <= position);
        }

        private void DrawChapterInfo(ref Graphics graphics, TimeSpan position)
        {
            var currentChapter = GetChapterAt(position);
            if (currentChapter == null) return;
            DrawCenteredText(ref graphics, _config.Episode.CurrentChapter, currentChapter.Title);
            for (int i = -2; i <= 2; i++)
            {
                if (i == 0) continue;
                var chapter = GetNearChapter(currentChapter, i);
                if (chapter == null) continue;
                var otherChapter = _config.Episode.OtherChapter;
                otherChapter.X = otherChapter.X * i + _config.Episode.CurrentChapter.X;
                otherChapter.Y = otherChapter.Y * i + _config.Episode.CurrentChapter.Y;
                DrawCenteredText(ref graphics, otherChapter, chapter.Title);
            }
        }

        public Bitmap CreateImageForTime(TimeSpan position)
        {
            _background = _background ?? CreateBackgroundImage();
            var frame = new Bitmap(_background);
            var graphics = Graphics.FromImage(frame);
            DrawBarPosition(ref graphics, position);
            DrawChapterInfo(ref graphics, position);
            return frame;
        }
    }
}