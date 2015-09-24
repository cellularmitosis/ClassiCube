﻿using System;
using System.Drawing;
using OpenTK.Input;

namespace ClassicalSharp {
	
	public sealed class MenuInputWidget : Widget {
		
		public MenuInputWidget( Game game, Font font, Font boldFont, Font hintFont ) : base( game ) {
			HorizontalDocking = Docking.LeftOrTop;
			VerticalDocking = Docking.BottomOrRight;
			this.font = font;
			this.boldFont = boldFont;
			this.hintFont = hintFont;
			chatInputText = new StringBuffer( 64 );
		}
		
		public static MenuInputWidget Create( Game game, int x, int y, int width, int height, string text, Docking horizontal,
		                                     Docking vertical, Font font, Font tildeFont, Font hintFont, MenuInputValidator validator ) {
			MenuInputWidget widget = new MenuInputWidget( game, font, tildeFont, hintFont );
			
			widget.HorizontalDocking = horizontal;
			widget.VerticalDocking = vertical;
			widget.XOffset = x;
			widget.YOffset = y;
			widget.DesiredMaxWidth = width;
			widget.DesiredMaxHeight = height;
			widget.chatInputText.Append( 0, text );
			widget.Validator = validator;
			widget.Init();
			return widget;
		}
		
		Texture chatInputTexture, chatCaretTexture;
		Color backColour = Color.FromArgb( 200, 30, 30, 30 );
		readonly Font font, boldFont, hintFont;
		StringBuffer chatInputText;
		public int XOffset = 0, YOffset = 0;
		public int DesiredMaxWidth, DesiredMaxHeight;
		public MenuInputValidator Validator;
		
		double accumulator;
		public override void Render( double delta ) {		
			chatInputTexture.Render( graphicsApi );
			if( (accumulator % 1) >= 0.5 )
				chatCaretTexture.Render( graphicsApi );
			accumulator += delta;
		}

		public override void Init() {
			DrawTextArgs caretArgs = new DrawTextArgs( graphicsApi, "_", Color.White, false );
			chatCaretTexture = Utils2D.MakeTextTexture( boldFont, 0, 0, ref caretArgs );
			SetText( chatInputText.GetString() );
		}
		
		public void SetText( string value ) {
			chatInputText.Append( 0, value );
			Size textSize = Utils2D.MeasureSize( value, font, false );
			Size size = new Size( Math.Max( textSize.Width, DesiredMaxWidth ), 
			                     Math.Max( textSize.Height, DesiredMaxHeight ) );
			
			using( Bitmap bmp = Utils2D.CreatePow2Bitmap( size ) ) {
				using( Graphics g = Graphics.FromImage( bmp ) ) {
					Utils2D.DrawRect( g, backColour, 0, 0, size.Width, size.Height );
					DrawTextArgs args = new DrawTextArgs( graphicsApi, value, Color.White, false );
					args.SkipPartsCheck = true;
					Utils2D.DrawText( g, font, ref args, 0, 0 );
					
					string range = Validator.Range;
					Size hintSize = Utils2D.MeasureSize( range, hintFont, true );
					args = new DrawTextArgs( graphicsApi, range, Color.White, false );
					args.SkipPartsCheck = true;
					Utils2D.DrawText( g, hintFont, ref args, size.Width - hintSize.Width, 0 );
				}
				chatInputTexture = Utils2D.Make2DTexture( graphicsApi, bmp, size, 0, 0 );
			}
			
			X = CalcOffset( game.Width, size.Width, XOffset, HorizontalDocking );
			Y = CalcOffset( game.Height, size.Height, YOffset, VerticalDocking );
			chatCaretTexture.X1 = chatInputTexture.X1 = X;
			chatCaretTexture.X1 += textSize.Width;
			chatCaretTexture.Y1 = chatInputTexture.Y1 = Y;
			chatCaretTexture.Y1 = (Y + size.Height) - chatCaretTexture.Height;
			Width = size.Width;
			Height = size.Height;
		}
		
		public string GetText() {
			return chatInputText.GetString();
		}

		public override void Dispose() {
			graphicsApi.DeleteTexture( ref chatCaretTexture );
			graphicsApi.DeleteTexture( ref chatInputTexture );
		}

		public override void MoveTo( int newX, int newY ) {
			int deltaX = newX - X;
			int deltaY = newY - Y;
			X = newX; Y = newY;
			chatCaretTexture.X1 += deltaX;
			chatCaretTexture.Y1 += deltaY;
			chatInputTexture.X1 += deltaX;
			chatInputTexture.Y1 += deltaY;
		}
		
		static bool IsInvalidChar( char c ) {
			// Make sure we're in the printable text range from 0x20 to 0x7E
			return c < ' ' || c == '&' || c > '~';
		}
		
		public override bool HandlesKeyPress( char key ) {
			if( chatInputText.Length < 64 && !IsInvalidChar( key ) ) {
				if( !Validator.IsValidChar( key ) ) return true;		
				chatInputText.Append( chatInputText.Length, key );
				
				if( !Validator.IsValidString( chatInputText.GetString() ) ) {
					chatInputText.DeleteAt( chatInputText.Length - 1 );
					return true;
				}
				graphicsApi.DeleteTexture( ref chatInputTexture );
				SetText( chatInputText.ToString() );
			}
			return true;
		}
		
		public override bool HandlesKeyDown( Key key ) {
			if( key == Key.BackSpace && !chatInputText.Empty ) {
				chatInputText.DeleteAt( chatInputText.Length - 1 );
				graphicsApi.DeleteTexture( ref chatInputTexture );
				SetText( chatInputText.ToString() );
			}
			return true;
		}
		
		public override bool HandlesKeyUp( Key key ) {
			return true;
		}
	}
}