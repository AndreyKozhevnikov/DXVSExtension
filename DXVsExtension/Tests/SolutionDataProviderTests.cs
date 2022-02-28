
using DXVsExtension.Properties;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DXVSExtension.Tests {
    [TestFixture]
    public class SolutionDataProviderTests {
        [Test]
       public void dxT866644nonxaf() {
            //arrang
            var confFile = Resources.dxT866644_nonxaf__config;
            var xDocument = XDocument.Parse(confFile);
            var provider = new SolutionDataProvider();
            //act
            var res =provider.GetDBName(xDocument);
            //assert
            Assert.AreEqual("28-6-dxT866644", res);
        }
        [Test]
        public void xafConfig() {
            //arrang
            var confFile = Resources.AppXAF;
            var xDocument = XDocument.Parse(confFile);
            var provider = new SolutionDataProvider();

            //act
            var res = provider.GetDBName(xDocument);
            //assert
            Assert.AreEqual("28-6-dxT866644", res);
        }
        [Test]
        public void xafConfig2() {
            //arrang
            var confFile = Resources.AppTrueXAF;
            var xDocument = XDocument.Parse(confFile);
            var provider = new SolutionDataProvider();

            //act
            var res = provider.GetDBName(xDocument);
            //assert
            Assert.AreEqual("28-22-dxT866654v1", res);
        }
    }
}
