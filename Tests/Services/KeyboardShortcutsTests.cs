using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.System;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class KeyboardShortcutsTests
{
    [TestMethod]
    public void Resolve_AltLeft_ReturnsGoBack()
    {
        Assert.AreEqual(KeyboardAction.GoBack, KeyboardShortcuts.Resolve(VirtualKey.Left, ctrl: false, alt: true));
    }

    [TestMethod]
    public void Resolve_AltRight_ReturnsGoForward()
    {
        Assert.AreEqual(KeyboardAction.GoForward, KeyboardShortcuts.Resolve(VirtualKey.Right, ctrl: false, alt: true));
    }

    [TestMethod]
    public void Resolve_CtrlComma_ReturnsOpenSettings()
    {
        Assert.AreEqual(KeyboardAction.OpenSettings, KeyboardShortcuts.Resolve(KeyboardShortcuts.CommaKey, ctrl: true, alt: false));
    }

    [TestMethod]
    public void Resolve_EscapeAlone_ReturnsDismissUpdateFlow()
    {
        Assert.AreEqual(KeyboardAction.DismissUpdateFlow, KeyboardShortcuts.Resolve(VirtualKey.Escape, ctrl: false, alt: false));
    }

    [TestMethod]
    public void Resolve_UnreservedCombinations_ReturnNone()
    {
        // Plain arrows navigate nothing (they belong to focused controls).
        Assert.AreEqual(KeyboardAction.None, KeyboardShortcuts.Resolve(VirtualKey.Left, ctrl: false, alt: false));
        Assert.AreEqual(KeyboardAction.None, KeyboardShortcuts.Resolve(VirtualKey.Right, ctrl: false, alt: false));
        // Ctrl+arrows are unreserved.
        Assert.AreEqual(KeyboardAction.None, KeyboardShortcuts.Resolve(VirtualKey.Left, ctrl: true, alt: false));
        // Ctrl+Alt chords are never shortcuts (AltGr layouts).
        Assert.AreEqual(KeyboardAction.None, KeyboardShortcuts.Resolve(KeyboardShortcuts.CommaKey, ctrl: true, alt: true));
        Assert.AreEqual(KeyboardAction.None, KeyboardShortcuts.Resolve(VirtualKey.Left, ctrl: true, alt: true));
        // Modified Escape stays a control key, not a dismissal.
        Assert.AreEqual(KeyboardAction.None, KeyboardShortcuts.Resolve(VirtualKey.Escape, ctrl: true, alt: false));
        Assert.AreEqual(KeyboardAction.None, KeyboardShortcuts.Resolve(VirtualKey.Escape, ctrl: false, alt: true));
        // Ordinary keys and letters are unreserved.
        Assert.AreEqual(KeyboardAction.None, KeyboardShortcuts.Resolve(VirtualKey.A, ctrl: false, alt: false));
        Assert.AreEqual(KeyboardAction.None, KeyboardShortcuts.Resolve(VirtualKey.A, ctrl: false, alt: true));
        Assert.AreEqual(KeyboardAction.None, KeyboardShortcuts.Resolve(VirtualKey.F5, ctrl: false, alt: false));
    }
}
