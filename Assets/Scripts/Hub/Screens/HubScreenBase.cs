using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// Base for the hub's screens: a large window with the money on hand in the title bar, a hover tooltip,
    /// labels that tick once a second (timers), and a <see cref="Refresh"/> that rebuilds the body after an
    /// action while keeping every list's scroll position.
    /// </summary>
    public abstract class HubScreenBase : UIScreen
    {
        /// <summary>The argument the screen was opened with (<see cref="HubScreens.Open"/>), e.g. a trader id.</summary>
        public string Arg;

        protected HubTooltip Tip;
        private Text _money;
        private bool _closed;
        private float _nextSecond;
        private readonly List<KeyValuePair<Text, Func<string>>> _live = new List<KeyValuePair<Text, Func<string>>>();

        protected override Vector2 WindowSize => new Vector2(1800f, 980f);

        protected static CharacterProfile Character => ProfileStore.ActiveCharacter;
        protected static PlayerInventory Carried => HubKit.Carried;

        /// <summary>False for screens that work without a character (settings from the main menu).</summary>
        protected virtual bool NeedsCharacter => true;
        protected virtual bool ShowMoney => true;

        protected sealed override void Build()
        {
            EnsureChrome();
            _closed = false;
            _live.Clear();
            if (Tip != null) Tip.HideAll();
            if (NeedsCharacter && Character == null)
            {
                RectTransform col = HubUI.Stack(HubUI.Area(Body, 0.25f, 0.35f, 0.75f, 0.65f), 8f, 16);
                HubUI.Card((RectTransform)col.parent);
                HubUI.Heading(col, "No runner", 24);
                HubUI.Para(col, "There is no active character. Create or pick one from the main menu first.", 17, UIKit.TextDim);
                RefreshMoney();
                return;
            }
            try { BuildContent(); }
            catch (Exception e)
            {
                Debug.LogException(e);
                SetStatus("Something went wrong drawing this screen: " + e.Message, true);
            }
            RefreshMoney();
        }

        /// <summary>Builds the screen into <see cref="UIScreen.Body"/>.</summary>
        protected abstract void BuildContent();

        private void EnsureChrome()
        {
            if (Tip != null) return;
            Tip = HubTooltip.Create(Canvas);
            _money = UIKit.Label(Window, "", 18, UIKit.Accent, TextAnchor.MiddleRight, FontStyle.Bold);
            UIKit.Place(_money.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-172f, -12f), new Vector2(620f, 36f));
        }

        protected void RefreshMoney()
        {
            if (_money == null) return;
            if (!ShowMoney || Character == null) { _money.text = ""; return; }
            _money.text = "Stash " + Money.Format(Money.Count(ProfileStore.StashInventory))
                + UIKit.Colored("   \u00b7   ", UIKit.TextDim) + "Carried " + Money.Format(HubKit.MoneyCarried);
        }

        /// <summary>Rebuilds the body (after a purchase, a craft...) keeping the scroll position of every list.</summary>
        protected void Refresh(bool keepScroll = true)
        {
            if (_closed || Body == null) return;
            List<float> pos = null;
            if (keepScroll)
            {
                pos = new List<float>();
                ScrollRect[] before = Body.GetComponentsInChildren<ScrollRect>(false);
                for (int i = 0; i < before.Length; i++) pos.Add(before[i].content != null ? before[i].content.anchoredPosition.y : 0f);
            }
            for (int i = 0; i < Body.childCount; i++) Body.GetChild(i).gameObject.SetActive(false);
            Rebuild();
            if (pos == null || pos.Count == 0) return;
            ScrollRect[] after = Body.GetComponentsInChildren<ScrollRect>(false);
            if (after.Length != pos.Count) return;
            for (int i = 0; i < after.Length; i++)
            {
                if (after[i].content == null) continue;
                Vector2 p = after[i].content.anchoredPosition;
                p.y = pos[i];
                after[i].content.anchoredPosition = p;
            }
        }

        /// <summary>Registers a label that is re-evaluated every second (countdowns, fuel...).</summary>
        protected Text Live(Text label, Func<string> text)
        {
            if (label == null || text == null) return label;
            label.text = text();
            _live.Add(new KeyValuePair<Text, Func<string>>(label, text));
            return label;
        }

        /// <summary>Reports an action's outcome: the status line, a UI sound.</summary>
        protected void Report(bool ok, string message)
        {
            SetStatus(message ?? "", !ok);
            Sfx.PlayUI(ok ? SfxIds.UiBuy : SfxIds.UiError);
        }

        protected static void Click() => Sfx.PlayUI(SfxIds.UiClick);

        /// <summary>Opens another hub screen (closing this one).</summary>
        protected static void Open(string id, string arg = null)
        {
            Click();
            HubScreens.Open(id, arg);
        }

        /// <summary>Called once a second while open (after the live labels refresh).</summary>
        protected virtual void OnSecond() { }

        protected override void Update()
        {
            base.Update();
            if (_closed || Time.unscaledTime < _nextSecond) return;
            _nextSecond = Time.unscaledTime + 1f;
            for (int i = 0; i < _live.Count; i++)
            {
                Text t = _live[i].Key;
                if (t == null) continue;
                try { t.text = _live[i].Value(); }
                catch (Exception e) { Debug.LogException(e); _live[i] = new KeyValuePair<Text, Func<string>>(null, null); }
            }
            RefreshMoney();
            try { OnSecond(); }
            catch (Exception e) { Debug.LogException(e); }
        }

        protected override void OnClosed()
        {
            _closed = true;
            _live.Clear();
        }

        /// <summary>A standard service button strip (insurance, medic, technician, guide...), skipping this screen's own id.</summary>
        protected void ServiceButton(Transform row, string label, string screenId, string arg = null, float width = 150f)
        {
            if (!HubScreens.Has(screenId)) return;
            HubUI.Btn(row, label, () => Open(screenId, arg), width, 32f, UIKit.ButtonColor, true, 15);
        }
    }
}
