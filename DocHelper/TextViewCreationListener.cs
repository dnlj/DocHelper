using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;


namespace DocHelper {
	[Export(typeof(IWpfTextViewCreationListener))]
	[ContentType("code")]
	[TextViewRole(PredefinedTextViewRoles.Document)]
	internal sealed class TextViewCreationListener : IWpfTextViewCreationListener {
		public void TextViewCreated(IWpfTextView view) {
			new Formatter(view);
		}
	}
}
