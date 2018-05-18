using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocHelper {
	class Formatter {
		private IWpfTextView view;
		private bool formatting = false;

		public Formatter(IWpfTextView view) {
			this.view = view;
			view.TextBuffer.Changed += TextBuffer_Changed;
			view.TextBuffer.PostChanged += TextBuffer_PostChanged;
		}

		private void TextBuffer_Changed(object sender, TextContentChangedEventArgs e) {
			if (!formatting) {
				formatting = true;
				Format(e);
			}
		}

		private void TextBuffer_PostChanged(object sender, EventArgs e) {
			formatting = false;
		}

		private void Format(TextContentChangedEventArgs e) {
			if (e.Changes != null && (e.Before.LineCount < e.After.LineCount)) {
				foreach (var change in e.Changes) {
					HandleChange(change);
				}
			}
		}

		private void HandleChange(ITextChange change) {
			// Skip invalid changes
			if (!IsNewLine(change.NewText[0])) { return; }

			var snap = view.TextSnapshot;
			var oldLine = snap.GetLineFromPosition(change.OldPosition);
			var oldLineText = oldLine.GetText();
			var oldLineIndex = 0;

			// Get the position of the start of the text of the old line
			for (; oldLineIndex < oldLineText.Length; ++oldLineIndex) {
				if (!Char.IsWhiteSpace(oldLineText, oldLineIndex)) { break; }
			}

			var oldLineTextStart = oldLineText.Substring(oldLineIndex, oldLineText.Length - oldLineIndex);

			// Check if we need to append anything
			string append = null;

			if (oldLineTextStart.StartsWith("*") && !oldLineTextStart.StartsWith("*/") && InDocComment(change)) {
				append = "* ";
			} else if (oldLineTextStart.StartsWith("/**")) {
				append = " * ";
			}

			// Apply an edit if needed
			if (append != null) {
				var edit = view.TextBuffer.CreateEdit();
				var oldLinePos = oldLine.Start.Position;
				var restOfLine = snap.GetText(change.OldPosition, oldLinePos + oldLine.Length - change.OldPosition);

				// TODO: Need to handle the case where the cursor is in front of the first *
				// Dont add a * if there is already one there
				if (restOfLine.StartsWith("*")) {
					append = append.Substring(0, append.Length - 2);
				}

				// Apply the edit
				edit.Insert(change.NewEnd, oldLineText.Substring(0, oldLineIndex) + append);
				edit.Apply();
			}
		}
		
		private bool InDocComment(ITextChange change) {
			var snap = view.TextSnapshot;
			var curLine = snap.GetLineNumberFromPosition(change.OldPosition);

			while (curLine != 0) {
				var prevLineText = snap.GetLineFromLineNumber(curLine - 1).GetText().TrimStart();

				if (prevLineText.StartsWith("*")) {
					--curLine;
					continue;
				} else if (prevLineText.StartsWith("/**")) {
					return true;
				} else {
					return false;
				}
			}

			return false;
		}
		
		private bool IsNewLine(char c) {
			return c == '\u000A' // LF
				|| c == '\u000D' // CR
				|| c == '\u000B' // VT
				|| c == '\u000C' // FF
				|| c == '\u0085' // NEL
				|| c == '\u2028' // LS
				|| c == '\u2029';// PS
		}
	}
}
