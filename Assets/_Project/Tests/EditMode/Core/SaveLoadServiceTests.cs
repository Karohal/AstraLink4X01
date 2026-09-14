using System;
using System.IO;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode.Core
{
    public class SaveLoadServiceTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "AstraLinkTests_" + Guid.NewGuid());
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        [Test]
        public void Save_Then_Load_RoundTripsMinimalState()
        {
            var service = new SaveLoadService(_tempDir);
            var state = new GameStateSnapshot
            {
                Colony = new ColonySnapshot { Name = "Premier Pas", DevelopmentLevel = 1 },
                Treasury = new TreasurySnapshot { Amount = 500f }
            };

            service.Save(state, "slot1");
            var loaded = service.Load("slot1");

            Assert.IsNotNull(loaded);
            Assert.AreEqual(1, loaded.SchemaVersion);
            Assert.AreEqual("Premier Pas", loaded.Colony.Name);
            Assert.AreEqual(500f, loaded.Treasury.Amount);
        }

        [Test]
        public void Load_UnsupportedSchemaVersion_Throws()
        {
            var service = new SaveLoadService(_tempDir);
            var path = Path.Combine(_tempDir, "slot2.json");
            File.WriteAllText(path, "{\"SchemaVersion\": 999}");

            Assert.Throws<UnsupportedSaveVersionException>(() => service.Load("slot2"));
        }

        [Test]
        public void Load_MissingSlot_ReturnsNull()
        {
            var service = new SaveLoadService(_tempDir);
            Assert.IsNull(service.Load("does-not-exist"));
        }
    }
}
