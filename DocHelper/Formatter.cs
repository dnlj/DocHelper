using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
//using System.Diagnostics;

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

		// TODO: This whole function is a mess. I think we should be able to
		//       clean this up a lot by utilizing change.OldText/NewText and other
		//       ITextChange members.
		private void HandleChange(ITextChange change) {
			// Skip invalid changes
			if (change.NewText.Length == 0 || !IsNewLine(change.NewText[0])) { return; }

			// Useful variables
			var line = view.TextSnapshot.GetLineFromPosition(change.OldPosition);
			var lineText = line.GetText();
			var lineTextTrimmed = lineText.TrimStart();
			int indentIndex = lineText.Length - lineTextTrimmed.Length;
			var cursorPos = change.OldPosition - line.Start.Position;
			//Debug.WriteLine(change.ToString());
			//System.Diagnostics.Debugger.Break();

			// Determine where we are in the comment
			var inComment = false;
			var isCommentStart = false;
			var afterIndent = cursorPos > indentIndex;

			if (IsDocCommentStart(lineTextTrimmed)) {
				isCommentStart = true;
				inComment = afterIndent;
			} else {
				inComment = InDocComment(change);

				if (inComment && IsDocCommentEnd(lineTextTrimmed)) {
					inComment = !afterIndent;
				}
			}

			// Skip if we are not in a comment
			if (!inComment) { return; }

			// Perform the edit
			var edit = view.TextBuffer.CreateEdit();

			if (afterIndent) {

				string append = isCommentStart ? " * " : "* ";

				//Debug.WriteLine("|"+change.NewText+"|");

				// TODO: When can this case happen? Seems like its never hit.
				//
				//var afterCursor = lineText.Substring(cursorPos);
				//var afterCursorTrim = afterCursor.TrimStart();
				//
				//if (afterCursorTrim.StartsWith("*")) {
				//	Debug.WriteLine("AFTER !!!!!!!!");
				//	// Dont add an extra asterisk if there is already one there
				//	append = append.Substring(0, append.Length - 2);
				//	var afterLength = afterCursor.Length - afterCursorTrim.Length;
				//	edit.Delete(change.NewEnd, afterLength);
				//
				//	// Add a space after if needed
				//	var nextCharIndex = cursorPos + afterLength + 1;
				//	char nextChar = '\0';
				//
				//	if (nextCharIndex < lineText.Length) {
				//		nextChar = lineText[nextCharIndex];
				//	}
				//
				//	if (nextChar != '/' && !char.IsWhiteSpace(nextChar)) {
				//		edit.Insert(change.NewEnd + afterLength + 1, " ");
				//	}
				//}

				if (!change.NewText.Contains("*")) {
					//Debug.WriteLine("Insert: |"+ append +"|");
					edit.Insert(change.NewEnd, lineText.Substring(0, indentIndex) + append);
				}
			} else {
				// TODO: what was the point of this branch? Doesn't seem useful?
				//Debug.WriteLine("Insert 2");
				//Debug.WriteLine(change);
				//edit.Insert(change.OldPosition, lineText.Substring(cursorPos, indentIndex - cursorPos) + "* ");
				//edit.Insert(change.NewEnd, lineText.Substring(0, cursorPos));
			}

			edit.Apply();
		}

		private bool InDocComment(ITextChange change) {
			var snap = view.TextSnapshot;
			var curLine = snap.GetLineNumberFromPosition(change.OldPosition);

			for (;  curLine != 0; --curLine) {
				var lineText = snap.GetLineFromLineNumber(curLine).GetText().TrimStart();

				if (lineText.StartsWith("*")) {
				} else if (IsDocCommentStart(lineText)) {
					return true;
				} else {
					return false;
				}
			}

			return false;
		}

		private bool IsDocCommentStart(string line) {
			return line.StartsWith("/**") // JavaDoc style
				|| line.StartsWith("/*!");// Qt style
		}

		private bool IsDocCommentEnd(string line) {
			return line.StartsWith("*/");
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
