using Lertaro.App.Views.QuickSearchWindow.Helpers;

namespace Lertaro.App.Tests.Views.QuickSearchWindow.Helpers;

[TestClass]
public sealed class QuickSearchWindowPositionerTests
{
    [TestMethod]
    public void CalculatePhysicalPosition_SameDpi_CentersCorrectly()
    {
        var (physX, physY) = QuickSearchWindowPositioner.CalculatePhysicalPosition(
            waLeft: 0, waTop: 0, waWidth: 1920, waHeight: 1080,
            windowDipWidth: 600, targetDpiScaleX: 1.0,
            relativeLeft: null, relativeTop: null);

        Assert.AreEqual(660.0, physX, 0.001);
        Assert.AreEqual(237.6, physY, 0.001);
    }

    [TestMethod]
    public void CalculatePhysicalPosition_MixedDpiCrossMonitor_CentersCorrectly()
    {
        // Target display: 125% DPI (1.25x), 1080p secondary screen at physical X=3840
        var (physX, physY) = QuickSearchWindowPositioner.CalculatePhysicalPosition(
            waLeft: 3840, waTop: 0, waWidth: 1920, waHeight: 1080,
            windowDipWidth: 648, targetDpiScaleX: 1.25,
            relativeLeft: null, relativeTop: null);

        // Target physical width = 648 * 1.25 = 810px
        // Target physical X = 3840 + (1920 - 810) / 2 = 4395px
        // Target physical Y = 1080 * 0.22 = 237.6px
        Assert.AreEqual(4395.0, physX, 0.001);
        Assert.AreEqual(237.6, physY, 0.001);
    }

    [TestMethod]
    public void CalculatePosition_MixedDpi_ReturnsSynchronizedDipCoordinates()
    {
        // Target display: 125% DPI (1.25x), 1080p secondary screen at X=3840
        var (left, top) = QuickSearchWindowPositioner.CalculatePosition(
            waLeft: 3840, waTop: 0, waWidth: 1920, waHeight: 1080,
            windowDipWidth: 648, targetDpiScaleX: 1.25, targetDpiScaleY: 1.25,
            relativeLeft: null, relativeTop: null);

        // Target physical coordinates: (4395, 237.6)
        // WPF DIP values: Left = 4395 / 1.25 = 3516, Top = 237.6 / 1.25 = 190.08
        Assert.AreEqual(3516.0, left, 0.001);
        Assert.AreEqual(190.08, top, 0.001);

        // When WPF multiplies by target DPI scale (1.25), it hits exact target physical pixels
        Assert.AreEqual(4395.0, left * 1.25, 0.001);
        Assert.AreEqual(237.6, top * 1.25, 0.001);
    }

    [TestMethod]
    public void CalculatePhysicalPosition_RelativePercentage_PreservedAcrossMixedDpi()
    {
        var (physX, physY) = QuickSearchWindowPositioner.CalculatePhysicalPosition(
            waLeft: 3840, waTop: 0, waWidth: 1920, waHeight: 1080,
            windowDipWidth: 648, targetDpiScaleX: 1.25,
            relativeLeft: 0.2, relativeTop: 0.1);

        // Target physical X = 3840 + 0.2 * 1920 = 4224px
        // Target physical Y = 0 + 0.1 * 1080 = 108px
        Assert.AreEqual(4224.0, physX, 0.001);
        Assert.AreEqual(108.0, physY, 0.001);
    }

    [TestMethod]
    public void CalculateSummonPosition_FollowMouse_CentersOnCursorAndClampsInsideWorkArea()
    {
        // Cursor mid-screen: window (600 DIP wide, 100 DIP tall at 100%) centers on it.
        var (physX, physY) = QuickSearchWindowPositioner.CalculateSummonPosition(
            waLeft: 0, waTop: 0, waWidth: 1920, waHeight: 1080,
            windowDipWidth: 600, windowDipHeight: 100,
            targetDpiScaleX: 1.0, targetDpiScaleY: 1.0,
            mousePhysX: 960, mousePhysY: 540,
            mode: "FollowMouse", relativeLeft: null, relativeTop: null,
            customLeft: 0.5, customTop: 0.22);

        Assert.AreEqual(660.0, physX, 0.001); // 960 - 600/2
        Assert.AreEqual(490.0, physY, 0.001); // 540 - 100/2

        // Cursor in the work area's top-left corner: the window clamps fully inside.
        var (cornerX, cornerY) = QuickSearchWindowPositioner.CalculateSummonPosition(
            waLeft: 0, waTop: 0, waWidth: 1920, waHeight: 1080,
            windowDipWidth: 600, windowDipHeight: 100,
            targetDpiScaleX: 1.0, targetDpiScaleY: 1.0,
            mousePhysX: 10, mousePhysY: 10,
            mode: "FollowMouse", relativeLeft: null, relativeTop: null,
            customLeft: 0.5, customTop: 0.22);

        Assert.AreEqual(0.0, cornerX, 0.001);
        Assert.AreEqual(0.0, cornerY, 0.001);
    }

    [TestMethod]
    public void CalculateSummonPosition_CenterAndCustom_OverrideTheDragMemory()
    {
        // The remembered spot (0.2/0.1) must NOT win under Center or Custom.
        var (centerX, centerY) = QuickSearchWindowPositioner.CalculateSummonPosition(
            waLeft: 0, waTop: 0, waWidth: 1920, waHeight: 1080,
            windowDipWidth: 600, windowDipHeight: 100,
            targetDpiScaleX: 1.0, targetDpiScaleY: 1.0,
            mousePhysX: 0, mousePhysY: 0,
            mode: "Center", relativeLeft: 0.2, relativeTop: 0.1,
            customLeft: 0.0, customTop: 0.0);

        Assert.AreEqual(660.0, centerX, 0.001); // (1920 - 600) / 2
        Assert.AreEqual(237.6, centerY, 0.001); // 1080 * 0.22

        var (customX, customY) = QuickSearchWindowPositioner.CalculateSummonPosition(
            waLeft: 0, waTop: 0, waWidth: 1920, waHeight: 1080,
            windowDipWidth: 600, windowDipHeight: 100,
            targetDpiScaleX: 1.0, targetDpiScaleY: 1.0,
            mousePhysX: 0, mousePhysY: 0,
            mode: "Custom", relativeLeft: 0.2, relativeTop: 0.1,
            customLeft: 0.25, customTop: 0.5);

        Assert.AreEqual(480.0, customX, 0.001); // 0.25 * 1920
        Assert.AreEqual(540.0, customY, 0.001); // 0.5 * 1080
    }

    [TestMethod]
    public void CalculateSummonPosition_RememberOrUnknown_FallsBackToDragMemory()
    {
        foreach (var mode in new[] { "Remember", null, "Nonsense" })
        {
            var (physX, physY) = QuickSearchWindowPositioner.CalculateSummonPosition(
                waLeft: 0, waTop: 0, waWidth: 1920, waHeight: 1080,
                windowDipWidth: 600, windowDipHeight: 100,
                targetDpiScaleX: 1.0, targetDpiScaleY: 1.0,
                mousePhysX: 960, mousePhysY: 540,
                mode: mode, relativeLeft: 0.2, relativeTop: 0.1,
                customLeft: 0.5, customTop: 0.22);

            Assert.AreEqual(384.0, physX, 0.001, mode ?? "null"); // 0.2 * 1920
            Assert.AreEqual(108.0, physY, 0.001, mode ?? "null"); // 0.1 * 1080
        }
    }
}
