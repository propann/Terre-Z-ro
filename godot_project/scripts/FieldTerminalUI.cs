using System;
using System.Linq;
using Godot;
using TerreZero.Bunker;
using TerreZero.Gameplay;

namespace TerreZero.UI
{
    public partial class FieldTerminalUI : CanvasLayer
    {
        public event Action Closed;

        private Control _root;
        private Label _profile;
        private VBoxContainer _inventory;
        private VBoxContainer _quests;
        private VBoxContainer _crafting;
        private Label _bunker;
        private Label _status;

        public override void _Ready()
        {
            Layer = 65;
            BuildUi();
            Visible = false;
        }

        public void Open()
        {
            RefreshAll();
            Visible = true;
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }

        public void Close()
        {
            Visible = false;
            GlobalSaveStore.SaveAll();
            BunkerSaveStore.Save();
            Closed?.Invoke();
        }

        public void RefreshAll()
        {
            if (_root == null)
                return;

            var p = GameState.Player;
            _profile.Text =
                $"NIVEAU {p.Level}   XP {p.Experience}/{PlayerProfileState.ExperienceForLevel(p.Level)}\n" +
                $"PV {p.Health}/{p.MaxHealth}   END {p.Stamina}/{p.MaxStamina}   RAD {p.Radiation}%\n" +
                $"CAPTURES {p.ChimeresCaptured}   VICTOIRES {p.BattlesWon}\n" +
                $"DISTANCE {p.DistanceWalkedKm:F2} km   MAÎTRISE {p.MasteryPoints}";

            Clear(_inventory);
            foreach (var stack in GameState.Inventory.Stacks
                .OrderBy(s => ItemCatalog.Get(s.ItemId)?.Category))
            {
                ItemDefinition item = ItemCatalog.Get(stack.ItemId);
                if (item == null)
                    continue;

                _inventory.AddChild(new Label
                {
                    Text = $"{item.Name} x{stack.Quantity}  [{item.Rarity}]"
                });
            }
            _inventory.AddChild(new Label
            {
                Text = $"POIDS {GameState.Inventory.CurrentWeightKg:F1}/{GameState.Inventory.MaxWeightKg:F1} kg"
            });

            Clear(_quests);
            foreach (var quest in GameState.Quests.Quests)
            {
                string progress = string.Join(
                    " • ",
                    quest.Objectives.Select(o => $"{o.Progress}/{o.Required}")
                );

                _quests.AddChild(new Label
                {
                    Text = $"{quest.Title.ToUpperInvariant()} [{quest.State}]\n{quest.Description}\n{progress}",
                    AutowrapMode = TextServer.AutowrapMode.WordSmart
                });
            }

            Clear(_crafting);
            foreach (CraftRecipe recipe in CraftingCatalog.All)
            {
                var button = new Button
                {
                    Text = $"{recipe.Name} {(recipe.BunkerOnly ? "[BUNKER]" : "[TERRAIN]")}",
                    CustomMinimumSize = new Vector2(330, 38)
                };

                button.Pressed += () =>
                {
                    CraftingService.TryCraft(
                        recipe.Id,
                        BunkerService.State.Level > 0,
                        out string message
                    );
                    _status.Text = message;
                    RefreshAll();
                };

                _crafting.AddChild(button);
            }

            var b = BunkerService.State;
            _bunker.Text =
                $"NIVEAU {b.Level}   ÉNERGIE {b.Energy}   DÉFENSE {b.Defense}\n" +
                $"RAFFINERIE {(b.RefineryBuilt ? "OK" : "--")}   " +
                $"DRESSAGE {(b.TrainingBayBuilt ? "OK" : "--")}   " +
                $"INFIRMERIE {(b.MedicalBayBuilt ? "OK" : "--")}\n" +
                $"BONUS STOCKAGE +{b.StorageBonusKg} kg";
        }

        private void BuildUi()
        {
            _root = new ColorRect
            {
                Color = new Color(0.01f, 0.015f, 0.02f, 0.96f)
            };
            _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            AddChild(_root);

            var title = new Label
            {
                Text = "TERRE ZÉRO // TERMINAL DE TERRAIN",
                Position = new Vector2(30, 22)
            };
            title.AddThemeFontSizeOverride("font_size", 22);
            title.AddThemeColorOverride("font_color", new Color("e8a23a"));
            _root.AddChild(title);

            var tabs = new TabContainer
            {
                Position = new Vector2(30, 64),
                Size = new Vector2(1220, 575)
            };
            _root.AddChild(tabs);

            _profile = new Label();
            _profile.AddThemeFontSizeOverride("font_size", 18);
            tabs.AddChild(Wrap("PROFIL", _profile));

            _inventory = new VBoxContainer();
            tabs.AddChild(Wrap("INVENTAIRE", _inventory));

            _quests = new VBoxContainer();
            tabs.AddChild(Wrap("QUÊTES", _quests));

            _crafting = new VBoxContainer();
            tabs.AddChild(Wrap("CRAFTING", _crafting));

            var bunkerBox = new VBoxContainer();
            _bunker = new Label();
            bunkerBox.AddChild(_bunker);
            bunkerBox.AddChild(ActionButton("CONSTRUIRE RAFFINERIE", () =>
            {
                BunkerService.TryBuildRefinery(out string m);
                _status.Text = m;
                RefreshAll();
            }));
            bunkerBox.AddChild(ActionButton("ZONE DE DRESSAGE", () =>
            {
                BunkerService.TryBuildTrainingBay(out string m);
                _status.Text = m;
                RefreshAll();
            }));
            bunkerBox.AddChild(ActionButton("INFIRMERIE", () =>
            {
                BunkerService.TryBuildMedicalBay(out string m);
                _status.Text = m;
                RefreshAll();
            }));
            bunkerBox.AddChild(ActionButton("AGRANDIR STOCKAGE", () =>
            {
                BunkerService.TryExpandStorage(out string m);
                _status.Text = m;
                RefreshAll();
            }));
            bunkerBox.AddChild(ActionButton("RAFFINER 8 FERRAILLES", () =>
            {
                BunkerService.RefineScrap(8, out string m);
                _status.Text = m;
                RefreshAll();
            }));
            bunkerBox.AddChild(ActionButton("DÉCONTAMINATION", () =>
            {
                BunkerService.TryDecontaminate(out string m);
                _status.Text = m;
                RefreshAll();
            }));
            tabs.AddChild(Wrap("BUNKER", bunkerBox));

            _status = new Label
            {
                Text = "PRÊT",
                Position = new Vector2(30, 654),
                Size = new Vector2(930, 36)
            };
            _root.AddChild(_status);

            var close = new Button
            {
                Text = "RETOUR [I]",
                Position = new Vector2(1070, 650),
                Size = new Vector2(180, 44)
            };
            close.Pressed += Close;
            _root.AddChild(close);
        }

        private static Control Wrap(string name, Control child)
        {
            var scroll = new ScrollContainer { Name = name };
            scroll.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            scroll.AddChild(child);
            child.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            return scroll;
        }

        private static Button ActionButton(string text, Action action)
        {
            var button = new Button
            {
                Text = text,
                CustomMinimumSize = new Vector2(360, 38)
            };
            button.Pressed += action;
            return button;
        }

        private static void Clear(Node node)
        {
            foreach (Node child in node.GetChildren())
                child.QueueFree();
        }
    }
}
