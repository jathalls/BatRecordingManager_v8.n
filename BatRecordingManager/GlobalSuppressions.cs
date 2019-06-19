using System.Diagnostics.CodeAnalysis;

[assembly:
    SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Scope = "member",
        Target =
            "BatRecordingManager.BatTagSortConverter.#Convert(System.Object,System.Type,System.Object,System.Globalization.CultureInfo)")]
[assembly:
    SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Scope = "member",
        Target = "BatRecordingManager.BatEditor.#RefreshBatNameListBox(System.Boolean)")]
[assembly:
    SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope = "member",
        Target = "BatRecordingManager.RecordingSession.#Recordings")]
[assembly: SuppressMessage("Microsoft.Design", "CA1014:MarkAssembliesWithClsCompliant")]
[assembly:
    SuppressMessage("Style", "IDE0017:Simplify object initialization", Justification = "<Pending>", Scope = "member",
        Target =
            "~M:BatRecordingManager.BatEditor.TagAddButton_Click(System.Object,System.Windows.RoutedEventArgs)")] // This file is used by Code Analysis to maintain SuppressMessage
[assembly:
    SuppressMessage("Style", "IDE0017:Simplify object initialization", Justification = "<Pending>", Scope = "member",
        Target = "~P:BatRecordingManager.BatCallControl.BatCall")]
[assembly:
    SuppressMessage("Style", "IDE0017:Simplify object initialization", Justification = "<Pending>", Scope = "member",
        Target =
            "~M:BatRecordingManager.BatListControl.AddBatButton_Click(System.Object,System.Windows.RoutedEventArgs)")]
[assembly:
    SuppressMessage("Style", "IDE0017:Simplify object initialization", Justification = "<Pending>", Scope = "member",
        Target =
            "~M:BatRecordingManager.BatListControl.EditBatButton_Click(System.Object,System.Windows.RoutedEventArgs)")]
[assembly:
    SuppressMessage("Style", "IDE0017:Simplify object initialization", Justification = "<Pending>", Scope = "member",
        Target = "~M:BatRecordingManager.DBAccess.AddTag(System.String,System.Int32)~System.Int32")]
[assembly:
    SuppressMessage("Style", "IDE0017:Simplify object initialization", Justification = "<Pending>", Scope = "member",
        Target =
            "~M:BatRecordingManager.DBAccess.GetBlankBat~BatRecordingManager.Bat")] // attributes that are applied to this project.
[assembly:
    SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters",
        MessageId = "System.Windows.Controls.TextBlock.set_Text(System.String)", Scope = "member",
        Target = "BatRecordingManager.BatDetailControl.#selectedBat")]
[assembly:
    SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters",
        MessageId = "System.Windows.Controls.TextBox.set_Text(System.String)", Scope = "member",
        Target = "BatRecordingManager.BatDetailControl.#selectedCallIndex")]
[assembly:
    SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters",
        MessageId = "System.Windows.MessageBox.Show(System.String,System.String,System.Windows.MessageBoxButton)",
        Scope = "member", Target = "BatRecordingManager.BatEditor.#DisplayInvalidErrorMessage(System.String)")]
[assembly:
    SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope = "member",
        Target = "BatRecordingManager.Bat.#BatCalls")]
[assembly:
    SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope = "member",
        Target = "BatRecordingManager.Bat.#BatPictures")]
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.
//
// To add a suppression to this file, right-click the message in the
// Code Analysis results, point to "Suppress Message", and click
// "In Suppression File".
// You do not need to add suppressions to this file manually.