using Platform.Shared.Helpers;
using Platform.Shared.Models;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Platform.Shared.Test.Helpers
{
    // Covers TranslationChangeMapper.ToMessage, which turns the Translations list the
    // translation-db-updater script posts to Platform.API v1/cachepreload into the OnTranslationChange
    // message relayed to both OddsEngine consumers. OddsEngine scopes a MaTe update to the markets
    // whose MaNa equals SrcTx (GM-852), so a lost or mis-mapped SourceText changes which markets
    // are translated with no error anywhere. Source, target and key values below are all distinct so
    // a mapping from the wrong field fails.
    public class TranslationChangeMapperTests
    {
        [Fact]
        public void ToMessage_MapsEveryField_SourceTextToSrcTx()
        {
            var message = TranslationChangeMapper.ToMessage(new List<TranslationChangeRequestDTO>
            {
                new TranslationChangeRequestDTO { StringId = "MaTe:16", TargetLanguage = "ZH", SourceText = "Total Goals", TargetText = "总进球" }
            });

            var item = Assert.Single(message.Tr);
            Assert.Equal("MaTe:16", item.StId);
            Assert.Equal("ZH", item.LaCo);
            Assert.Equal("总进球", item.Tx);
            Assert.Equal("Total Goals", item.SrcTx);
        }

        // Older senders omit sourceText. SrcTx must stay null (not empty or the target text) so
        // OddsEngine falls back to an unscoped, template-wide update.
        [Fact]
        public void ToMessage_NullSourceText_LeavesSrcTxNull()
        {
            var message = TranslationChangeMapper.ToMessage(new List<TranslationChangeRequestDTO>
            {
                new TranslationChangeRequestDTO { StringId = "Te:2", TargetLanguage = "ES", TargetText = "Rogers" }
            });

            Assert.Null(Assert.Single(message.Tr).SrcTx);
        }

        // An empty Tx is a removal: consumers revert to the English name.
        [Fact]
        public void ToMessage_NullTargetText_BecomesEmptyTx()
        {
            var message = TranslationChangeMapper.ToMessage(new List<TranslationChangeRequestDTO>
            {
                new TranslationChangeRequestDTO { StringId = "LeId:7", TargetLanguage = "FR", SourceText = "Premier League" }
            });

            Assert.Equal(string.Empty, Assert.Single(message.Tr).Tx);
        }

        [Fact]
        public void ToMessage_DropsNullRowsAndRowsWithoutKeyOrLanguage()
        {
            var message = TranslationChangeMapper.ToMessage(new List<TranslationChangeRequestDTO>
            {
                null,
                new TranslationChangeRequestDTO { StringId = " ", TargetLanguage = "ZH", SourceText = "Soccer", TargetText = "足球" },
                new TranslationChangeRequestDTO { StringId = "SpId:1", TargetLanguage = "", SourceText = "Soccer", TargetText = "足球" },
                new TranslationChangeRequestDTO { StringId = "SpId:1", TargetLanguage = "ZH", SourceText = "Soccer", TargetText = "足球" }
            });

            Assert.Equal("SpId:1", Assert.Single(message.Tr).StId);
        }

        [Fact]
        public void ToMessage_NullList_ReturnsEmptyMessage()
        {
            Assert.Empty(TranslationChangeMapper.ToMessage(null).Tr);
        }

        // update.py posts camelCase keys. ASP.NET Core's default System.Text.Json input formatter
        // (Platform.API Startup uses plain AddControllers) reads with JsonSerializerDefaults.Web, so
        // this is the binding the endpoint performs. A [JsonPropertyName] that no longer matches
        // "sourceText" would silently drop SrcTx for every DeepL update.
        [Fact]
        public void CachePreloadRequestDTO_BindsCamelCaseTranslationPayload()
        {
            const string body = "{\"cacheType\":0,\"translations\":[{\"stringId\":\"MaTe:16\",\"targetLanguage\":\"ZH\",\"targetText\":\"总进球\",\"sourceText\":\"Total Goals\"}]}";

            var request = JsonSerializer.Deserialize<CachePreloadRequestDTO>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));

            var item = Assert.Single(TranslationChangeMapper.ToMessage(request.Translations).Tr);
            Assert.Equal("MaTe:16", item.StId);
            Assert.Equal("ZH", item.LaCo);
            Assert.Equal("总进球", item.Tx);
            Assert.Equal("Total Goals", item.SrcTx);
        }
    }
}
