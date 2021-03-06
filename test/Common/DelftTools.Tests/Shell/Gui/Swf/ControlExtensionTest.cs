﻿using System.Linq;
using System.Windows.Forms;
using DelftTools.Shell.Gui.Swf.Extensions;
using NUnit.Framework;

namespace DelftTools.Tests.Shell.Gui.Swf
{
    [TestFixture]
    public class ControlExtensionsTest
    {
        private Form form;
        private Control userControl;

        #region Setup

        [TestFixtureSetUp]
        public void FixtureSetup()
        {
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
        }

        [SetUp]
        public void TestTearDown()
        {
            form = new Form();
            userControl = new UserControl();
            userControl.Controls.Add(new TextBox());
            form.Controls.Add(userControl);
        }

        [TearDown]
        public void TestSetup()
        {
            form.Dispose();
        }

        #endregion

        [Test]
        public void GetAllControlsRecursiveReturnsCorrectList()
        {
            Assert.AreEqual(1, userControl.GetAllControlsRecursive<TextBox>().Count());
            Assert.AreEqual(1, form.GetAllControlsRecursive<TextBox>().Count());
            Assert.AreEqual(2, form.GetAllControlsRecursive().Count());
            Assert.AreEqual(1, form.GetAllControlsRecursive<UserControl>().Count());
            Assert.AreEqual(2, form.Controls.GetAllControlsRecursive().Count());                   
        }

        [Test]
        public void GetFirstControlOfTypeReturnsControlAsExptected()
        {
            Assert.IsNotNull(form.GetFirstControlOfType<UserControl>());
            Assert.IsNull(form.GetFirstControlOfType<CheckBox>());
        }
    }
}